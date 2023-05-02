using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Imaging;
using Microsoft.Azure.Kinect.Sensor;

// Enum that define the type of data available.
// Bodies give the skeletons avec the depth calibration for visualistion.
//public enum DataType { SOUND, BODIES, RGB, DEPTH };

namespace RemoteConnectors
{
    public class KinectAzureRemoteConnectorConfiguration 
    {
        /// <summary>
        /// Get or set the list of data to connect to.
        /// </summary>
        public uint ActiveStreamNumber { get; set; } = 1;

        /// <summary>
        /// Gets or sets port number where the iteration begin.
        /// </summary>
        public uint StartPort { get; set; } = 11411;

        /// <summary>
        /// Gets or sets the ip of the remote exporter.
        /// </summary>
        public string Address { get; set; } = "localhost";
    }

    public class KinectAzureRemoteConnector : Subpipeline
    {
        /// <summary>
        /// Gets the emitter of color image.
        /// </summary>
        public Emitter<Shared<EncodedImage>>? OutColorImage { get; private set; }

        /// <summary>
        /// Gets the emitter of depth image.
        /// </summary>
        public Emitter<Shared<EncodedDepthImage>>? OutDepthImage { get; private set; }

        /// <summary>
        /// Gets the emitter of bodies.
        /// </summary>
        public Emitter<List<AzureKinectBody>>? OutBodies { get; private set; }

        /// <summary>
        /// Gets the emitter of depth calibration.
        /// </summary>
        public Emitter<Microsoft.Psi.Calibration.IDepthDeviceCalibrationInfo>? OutDepthDeviceCalibrationInfo { get; private set; }

        /// <summary>
        /// Gets the emitter of audio.
        /// </summary>
        public Emitter<AudioBuffer>? OutAudio{ get; private set; }

        /// <summary>
        /// Gets the emitter of IMU data.
        /// </summary>
        public Emitter<ImuSample>? OutIMU { get; private set; }

        private KinectAzureRemoteConnectorConfiguration Configuration { get; }

        public KinectAzureRemoteConnector(Pipeline parent, KinectAzureRemoteConnectorConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
          : base(parent, name, defaultDeliveryPolicy)
        {
            Configuration = configuration ?? new KinectAzureRemoteConnectorConfiguration();

            OutColorImage = null;
            OutDepthImage = null;
            OutBodies = null;
            OutDepthDeviceCalibrationInfo = null;
            OutAudio = null;

            int count = 0;
            while(count < Configuration.ActiveStreamNumber)
            {
                int port = (int)Configuration.StartPort + count;
                RemoteImporter importer = new RemoteImporter(parent, Configuration.Address, port);
                if (!importer.Connected.WaitOne(-1))
                {
                    throw new Exception("Error while connecting to: " + Configuration.Address + ":" + port);
                }
                count++;

                foreach(var stream in importer.Importer.AvailableStreams)
                {
                    string streamName = stream.Name;
                    Console.WriteLine(name ?? "KinectAzureRemoteConnector : " + " Available stream: " + streamName);
                    // Could do better probably 
                    if(streamName.Contains("Audio"))
                    {
                        OutAudio = importer.Importer.OpenStream<AudioBuffer>(streamName).Out;
                        break;
                    }
                    if (streamName.Contains("Bodies"))
                    {
                        OutBodies = importer.Importer.OpenStream<List<AzureKinectBody>>(streamName).Out;
                        break;
                    }
                    if (streamName.Contains("Calibration"))
                    {
                        OutDepthDeviceCalibrationInfo = importer.Importer.OpenStream<Microsoft.Psi.Calibration.IDepthDeviceCalibrationInfo>(streamName).Out;
                        break;
                    }
                    if (streamName.Contains("RGB"))
                    {
                        Console.WriteLine(stream.TypeName);
                        OutColorImage = importer.Importer.OpenStream<Shared<EncodedImage>>(streamName).Out;
                        break;
                    }
                    if (streamName.Contains("Depth"))
                    {
                        Console.WriteLine(stream.TypeName);
                        OutDepthImage = importer.Importer.OpenStream<Shared<EncodedDepthImage>>(streamName).Out;
                        break;
                    }
                    if (streamName.Contains("IMU"))
                    {
                        Console.WriteLine(stream.TypeName);
                        OutIMU = importer.Importer.OpenStream<ImuSample>(streamName).Out;
                        break;
                    }
                }
            }
        }
    }
}
