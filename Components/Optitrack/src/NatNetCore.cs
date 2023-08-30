using System.Collections;
using NatNetML;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace NatNetComponent
{   
    /// <summary>
    /// Internal Optitrack component class.
    /// </summary>
    internal sealed class NatNetCore : ISourceComponent, IDisposable
    {
        private readonly NatNetCoreConfiguration Configuration;

         /// <summary>
        /// The underlying NatNet device.
        /// </summary>
        /// 
        private static NatNetClientML mNatNet = new NatNetML.NatNetClientML();

        /*  List for saving each of datadescriptors */
        private List<DataDescriptor> mDataDescriptor = new List<DataDescriptor>();

        /*  Lists and Hashtables for saving data descriptions   */
        private Hashtable mHtSkelRBs = new Hashtable();
        private List<NatNetML.RigidBody> mRigidBodies = new List<NatNetML.RigidBody>();
        private List<Skeleton> mSkeletons = new List<Skeleton>();
        private List<ForcePlate> mForcePlates = new List<ForcePlate>();
        private List<Device> mDevices = new List<Device>();
        private List<Camera> mCameras = new List<Camera>();
        private readonly object ConnexionOpenLock = new object();
        private Pipeline Parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="NatNetCore"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="config">Configuration to use for the device.</param>
        public NatNetCore(Pipeline pipeline, NatNetCoreConfiguration? config = null)
        {
            Configuration = config ?? new NatNetCoreConfiguration();
            Parent = pipeline;

            //this.Bodies = pipeline.CreateEmitter<List<Skeleton>>(this, nameof(this.Bodies));
            OutRigidBodies = pipeline.CreateEmitter<List<RigidBody>>(this, nameof(this.OutRigidBodies));
            //this.ForcePlates = pipeline.CreateEmitter<List<ForcePlate>>(this, nameof(this.ForcePlates));
            //this.Gestures = pipeline.CreateEmitter<List<UserGesturesState>>(this, nameof(this.Gestures));
            //this.FrameRate = pipeline.CreateEmitter<double>(this, nameof(this.FrameRate));
        }

        /// <summary>
        /// Gets the current image from the color camera.
        /// </summary>
        //public Emitter<Shared<Image>> ColorImage { get; private set; }

        /// <summary>
        /// Gets the current depth image.
        /// </summary>
        //public Emitter<Shared<DepthImage>> DepthImage { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked bodies.
        /// </summary>
        //public Emitter<List<Skeleton>> Bodies { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently rigid bodies.
        /// </summary>
        public Emitter<List<RigidBody>> OutRigidBodies { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked users.
        /// </summary>
        //public Emitter<List<ForcePlate>> ForcePlates { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked users.
        /// </summary>
        //public Emitter<List<UserGesturesState>> Gestures { get; private set; }

        /// <summary>
        /// Gets the current frames-per-second actually achieved.
        /// </summary>
        //public Emitter<double> FrameRate { get; private set; }

        /// <summary>
        /// Returns the number of Kinect for Azure devices available on the system.
        /// </summary>
        /// <returns>Number of available devices.</returns>
        public static int GetInstalledCount()
        {
            return 0;// mDevices.Count;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            mNatNet.Dispose();
        }

        private void fetchFrameData(NatNetML.FrameOfMocapData data, NatNetML.NatNetClientML client)
        {

            /*  Exception handler for cases where assets are added or removed.
                Data description is re-obtained in the main function so that contents
                in the frame handler is kept minimal. */
            if ((data.bTrackingModelsChanged == true || data.nRigidBodies != mRigidBodies.Count || data.nSkeletons != mSkeletons.Count || data.nForcePlates != mForcePlates.Count))
            {
                fetchAndParseDataDescriptor();
            }

            /*  Processing and ouputting frame data every 200th frame.
                This conditional statement is included in order to simplify the program output */
            if (Configuration.OutputRigidBodies)
            {
                List<RigidBody> rigidBodies = new List<RigidBody>();
                /*  Parsing Rigid Body Frame Data   */
                for (int i = 0; i < mRigidBodies.Count; i++)
                {
                    int rbID = mRigidBodies[i].ID;              // Fetching rigid body IDs from the saved descriptions

                    for (int j = 0; j < data.nRigidBodies; j++)
                    {
                        if (rbID == data.RigidBodies[j].ID)      // When rigid body ID of the descriptions matches rigid body ID of the frame data.
                        {
                            NatNetML.RigidBody rb = mRigidBodies[i];                // Saved rigid body descriptions
                            NatNetML.RigidBodyData rbData = data.RigidBodies[j];    // Received rigid body descriptions

                            if (rbData.Tracked == true)
                            {
                                RigidBody rigidB = new RigidBody();
                                rigidB.name = rb.Name;
                                rigidB.position = new Vector3D(rbData.x, rbData.y, rbData.z);
                                rigidB.orientation = new Quaternion(rbData.qx, rbData.qy, rbData.qz, rbData.qw);
                                rigidBodies.Add(rigidB);
                            }
                        }
                    }
                }
                OutRigidBodies.Post(rigidBodies, Parent.GetCurrentTime());
            }   
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            // Prevent device open race condition.
            lock (ConnexionOpenLock)
            {
                NatNetClientML.ConnectParams connectParams = new NatNetClientML.ConnectParams();
                connectParams.ConnectionType = Configuration.ConnectionType;
                connectParams.ServerAddress = Configuration.ServerIP;
                connectParams.LocalAddress = Configuration.LocalIP;
                mNatNet.Connect(connectParams);
            }
            try
            {
                // activate selected device
                NatNetML.ServerDescription m_ServerDescriptor = new NatNetML.ServerDescription();
                int errorCode = mNatNet.GetServerDescription(m_ServerDescriptor);

                if (errorCode != 0)
                    throw new ArgumentException("Error: Failed to connect. Check the connection settings.");

                fetchAndParseDataDescriptor();
                mNatNet.OnFrameReady += new NatNetML.FrameReadyEventHandler(fetchFrameData);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("Invalid operation: " + exception.ToString());
            }
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            /*  [NatNet] Disabling data handling function   */
            mNatNet.OnFrameReady -= fetchFrameData;

            /*  Clearing Saved Descriptions */
            mRigidBodies.Clear();
            mSkeletons.Clear();
            mHtSkelRBs.Clear();
            mForcePlates.Clear();
            mNatNet.Disconnect();
            notifyCompleted();
        }

        private void fetchAndParseDataDescriptor()
        {
            if (!mNatNet.GetDataDescriptions(out mDataDescriptor))
                throw new ArgumentException("Error: Failed to get data descriptions. Check the connection settings.");
            //  [NatNet] Request a description of the Active Model List from the server. 
            //  This sample will list only names of the data sets, but you can access 
            int numDataSet = mDataDescriptor.Count;
            Console.WriteLine("Total {0} data sets in the capture:", numDataSet);

            for (int i = 0; i < numDataSet; ++i)
            {
                int dataSetType = mDataDescriptor[i].type;
                // Parse Data Descriptions for each data sets and save them in the delcared lists and hashtables for later uses.
                switch (dataSetType)
                {
                    case ((int)NatNetML.DataDescriptorType.eMarkerSetData):
                        NatNetML.MarkerSet mkset = (NatNetML.MarkerSet)mDataDescriptor[i];
                        break;

                    case ((int)NatNetML.DataDescriptorType.eRigidbodyData):
                        NatNetML.RigidBody rb = (NatNetML.RigidBody)mDataDescriptor[i];
                        // Saving Rigid Body Descriptions
                        mRigidBodies.Add(rb);
                        break;

                    case ((int)NatNetML.DataDescriptorType.eSkeletonData):
                        NatNetML.Skeleton skl = (NatNetML.Skeleton)mDataDescriptor[i];
                        //Saving Skeleton Descriptions
                        //mSkeletons.Add(skl);
                        // Saving Individual Bone Descriptions
                        for (int j = 0; j < skl.nRigidBodies; j++)
                        {
                            int uniqueID = skl.ID * 1000 + skl.RigidBodies[j].ID;
                            int key = uniqueID.GetHashCode();
                            mHtSkelRBs.Add(key, skl.RigidBodies[j]); //Saving the bone segments onto the hashtable
                        }
                        break;


                    case ((int)NatNetML.DataDescriptorType.eForcePlateData):
                        NatNetML.ForcePlate fp = (NatNetML.ForcePlate)mDataDescriptor[i];
                        // Saving Force Plate Channel Names
                        mForcePlates.Add(fp);
                        break;

                    case ((int)NatNetML.DataDescriptorType.eDeviceData):
                        NatNetML.Device dd = (NatNetML.Device)mDataDescriptor[i];
                        // Saving Device Data Channel Names
                        mDevices.Add(dd);
                        break;

                    case ((int)NatNetML.DataDescriptorType.eCameraData):
                        // Saving Camera Names
                        NatNetML.Camera camera = (NatNetML.Camera)mDataDescriptor[i];
                        // Saving Force Plate Channel Names
                        mCameras.Add(camera);
                        break;

                    default:
                        // When a Data Set does not match any of the descriptions provided by the SDK.
                        throw new ArgumentException("Error: Invalid Data Set - dataSetType = " + dataSetType);
                }
            }
        }
        static double RadiansToDegrees(double dRads)
        {
            return dRads * (180.0f / Math.PI);
        }

        static int LowWord(int number)
        {
            return number & 0xFFFF;
        }

        static int HighWord(int number)
        {
            return ((number >> 16) & 0xFFFF);
        }
    }
}
