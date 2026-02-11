// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.NatNetComponent
{
    using System.Collections;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using NatNetML;

    /// <summary>
    /// Internal Optitrack component class.
    /// </summary>
    internal sealed class NatNetCore : ISourceComponent, IDisposable
    {
        private readonly NatNetCoreConfiguration configuration;

        /// <summary>
        /// The underlying NatNet device.
        /// </summary>
        private static NatNetClientML natNet = new NatNetML.NatNetClientML();

        /*  List for saving each of datadescriptors */
        private List<DataDescriptor> dataDescriptor = new List<DataDescriptor>();

        /*  Lists and Hashtables for saving data descriptions   */
        private Hashtable hSkelRBs = new Hashtable();
        private List<NatNetML.RigidBody> rigidBodies = new List<NatNetML.RigidBody>();
        private List<Skeleton> skeletons = new List<Skeleton>();
        private List<ForcePlate> forcePlates = new List<ForcePlate>();
        private List<Device> devices = new List<Device>();
        private List<Camera> cameras = new List<Camera>();
        private readonly object connexionOpenLock = new object();
        private Pipeline parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="NatNetCore"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="config">configuration to use for the device.</param>
        public NatNetCore(Pipeline pipeline, NatNetCoreConfiguration? config = null)
        {
            this.configuration = config ?? new NatNetCoreConfiguration();
            this.parent = pipeline;

            // this.Bodies = pipeline.CreateEmitter<List<Skeleton>>(this, nameof(this.Bodies));
            this.OutRigidBodies = pipeline.CreateEmitter<List<RigidBody>>(this, nameof(this.OutRigidBodies));

            // this.ForcePlates = pipeline.CreateEmitter<List<ForcePlate>>(this, nameof(this.ForcePlates));
            // this.Gestures = pipeline.CreateEmitter<List<UserGesturesState>>(this, nameof(this.Gestures));
            // this.FrameRate = pipeline.CreateEmitter<double>(this, nameof(this.FrameRate));
        }

        /// <summary>
        /// Gets the current image from the color camera.
        /// </summary>
        //public Emitter<Shared<Image>> ColorImage { get; private set; }

        /// <summary>
        /// Gets the current depth image.
        /// </summary>
        // public Emitter<Shared<DepthImage>> DepthImage { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked bodies.
        /// </summary>
        // public Emitter<List<Skeleton>> Bodies { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently rigid bodies.
        /// </summary>
        public Emitter<List<RigidBody>> OutRigidBodies { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked users.
        /// </summary>
        /// <returns></returns>
        // public Emitter<List<ForcePlate>> ForcePlates { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked users.
        /// </summary>
        // public Emitter<List<UserGesturesState>> Gestures { get; private set; }

        /// <summary>
        /// Gets the current frames-per-second actually achieved.
        /// </summary>
        // public Emitter<double> FrameRate { get; private set; }

        /// <summary>
        /// Returns the number of Kinect for Azure devices available on the system.
        /// </summary>
        /// <returns>Number of available devices.</returns>
        public static int GetInstalledCount()
        {
            return 0; // mDevices.Count;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            natNet.Dispose();
        }

        /// <summary>
        /// Fetches and processes frame data from the NatNet stream.
        /// </summary>
        /// <param name="data">The frame of motion capture data.</param>
        /// <param name="client">The NatNet client instance.</param>
        private void FetchFrameData(NatNetML.FrameOfMocapData data, NatNetML.NatNetClientML client)
        {
            /*  Exception handler for cases where assets are added or removed.
                Data description is re-obtained in the main function so that contents
                in the frame handler is kept minimal. */
            if ((data.bTrackingModelsChanged == true || data.nRigidBodies != this.rigidBodies.Count || data.nSkeletons != this.skeletons.Count || data.nForcePlates != this.forcePlates.Count))
            {
                this.FetchAndParseDataDescriptor();
            }

            /*  Processing and ouputting frame data every 200th frame.
                This conditional statement is included in order to simplify the program output */
            if (this.configuration.OutputRigidBodies)
            {
                List<RigidBody> rigidBodiesParsed = new List<RigidBody>();
                /*  Parsing Rigid Body Frame Data   */
                for (int i = 0; i < this.rigidBodies.Count; i++)
                {
                    int rbID = this.rigidBodies[i].ID;              // Fetching rigid body IDs from the saved descriptions

                    for (int j = 0; j < data.nRigidBodies; j++)
                    {
                        if (rbID == data.RigidBodies[j].ID) // When rigid body ID of the descriptions matches rigid body ID of the frame data.
                        {
                            NatNetML.RigidBody rb = this.rigidBodies[i];                // Saved rigid body descriptions
                            NatNetML.RigidBodyData rbData = data.RigidBodies[j];    // Received rigid body descriptions

                            if (rbData.Tracked == true)
                            {
                                RigidBody rigidB = default(RigidBody);
                                rigidB.Name = rb.Name;
                                rigidB.Position = new Vector3D(rbData.x, rbData.y, rbData.z);
                                rigidB.Orientation = new Quaternion(rbData.qx, rbData.qy, rbData.qz, rbData.qw);
                                rigidBodiesParsed.Add(rigidB);
                            }
                        }
                    }
                }

                this.OutRigidBodies.Post(rigidBodiesParsed, this.parent.GetCurrentTime());
            }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            // Prevent device open race condition.
            lock (this.connexionOpenLock)
            {
                NatNetClientML.ConnectParams connectParams = new NatNetClientML.ConnectParams();
                connectParams.ConnectionType = this.configuration.ConnectionType;
                connectParams.ServerAddress = this.configuration.ServerIP;
                connectParams.LocalAddress = this.configuration.LocalIP;
                natNet.Connect(connectParams);
            }

            try
            {
                // activate selected device
                NatNetML.ServerDescription m_ServerDescriptor = new NatNetML.ServerDescription();
                int errorCode = natNet.GetServerDescription(m_ServerDescriptor);

                if (errorCode != 0)
                {
                    throw new ArgumentException("Error: Failed to connect. Check the connection settings.");
                }

                this.FetchAndParseDataDescriptor();
                natNet.OnFrameReady += new NatNetML.FrameReadyEventHandler(this.FetchFrameData);
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
            natNet.OnFrameReady -= this.FetchFrameData;

            /*  Clearing Saved Descriptions */
            this.rigidBodies.Clear();
            this.skeletons.Clear();
            this.hSkelRBs.Clear();
            this.forcePlates.Clear();
            natNet.Disconnect();
            notifyCompleted();
        }

        /// <summary>
        /// Fetches and parses data descriptors from the NatNet server.
        /// </summary>
        private void FetchAndParseDataDescriptor()
        {
            if (!natNet.GetDataDescriptions(out this.dataDescriptor))
            {
                throw new ArgumentException("Error: Failed to get data descriptions. Check the connection settings.");
            }

            // [NatNet] Request a description of the Active Model List from the server.
            //  This sample will list only names of the data sets, but you can access
            int numDataSet = this.dataDescriptor.Count;
            Console.WriteLine("Total {0} data sets in the capture:", numDataSet);

            for (int i = 0; i < numDataSet; ++i)
            {
                int dataSetType = this.dataDescriptor[i].type;

                // Parse Data Descriptions for each data sets and save them in the delcared lists and hashtables for later uses.
                switch (dataSetType)
                {
                    case ((int)NatNetML.DataDescriptorType.eMarkerSetData):
                        NatNetML.MarkerSet mkset = (NatNetML.MarkerSet)this.dataDescriptor[i];
                        break;

                    case ((int)NatNetML.DataDescriptorType.eRigidbodyData):
                        NatNetML.RigidBody rb = (NatNetML.RigidBody)this.dataDescriptor[i];

                        // Saving Rigid Body Descriptions
                        this.rigidBodies.Add(rb);
                        break;

                    case ((int)NatNetML.DataDescriptorType.eSkeletonData):
                        NatNetML.Skeleton skl = (NatNetML.Skeleton)this.dataDescriptor[i];

                        // Saving Skeleton Descriptions
                        // skeletons.Add(skl);
                        // Saving Individual Bone Descriptions
                        for (int j = 0; j < skl.nRigidBodies; j++)
                        {
                            int uniqueID = skl.ID * 1000 + skl.RigidBodies[j].ID;
                            int key = uniqueID.GetHashCode();
                            this.hSkelRBs.Add(key, skl.RigidBodies[j]); // Saving the bone segments onto the hashtable
                        }

                        break;

                    case ((int)NatNetML.DataDescriptorType.eForcePlateData):
                        NatNetML.ForcePlate fp = (NatNetML.ForcePlate)this.dataDescriptor[i];

                        // Saving Force Plate Channel Names
                        this.forcePlates.Add(fp);
                        break;

                    case ((int)NatNetML.DataDescriptorType.eDeviceData):
                        NatNetML.Device dd = (NatNetML.Device)this.dataDescriptor[i];

                        // Saving Device Data Channel Names
                        this.devices.Add(dd);
                        break;

                    case ((int)NatNetML.DataDescriptorType.eCameraData):
                        // Saving Camera Names
                        NatNetML.Camera camera = (NatNetML.Camera)this.dataDescriptor[i];

                        // Saving Force Plate Channel Names
                        this.cameras.Add(camera);
                        break;

                    default:
                        // When a Data Set does not match any of the descriptions provided by the SDK.
                        throw new ArgumentException("Error: Invalid Data Set - dataSetType = " + dataSetType);
                }
            }
        }

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        /// <param name="dRads">The angle in radians.</param>
        /// <returns>The angle in degrees.</returns>
        private static double RadiansToDegrees(double dRads)
        {
            return dRads * (180.0f / Math.PI);
        }

        /// <summary>
        /// Extracts the low word (lower 16 bits) from an integer.
        /// </summary>
        /// <param name="number">The integer value.</param>
        /// <returns>The low word value.</returns>
        private static int LowWord(int number)
        {
            return number & 0xFFFF;
        }

        /// <summary>
        /// Extracts the high word (upper 16 bits) from an integer.
        /// </summary>
        /// <param name="number">The integer value.</param>
        /// <returns>The high word value.</returns>
        private static int HighWord(int number)
        {
            return ((number >> 16) & 0xFFFF);
        }
    }
}
