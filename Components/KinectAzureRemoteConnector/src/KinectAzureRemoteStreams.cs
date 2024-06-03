using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Imaging;

namespace SAAC.RemoteConnectors
{
    public class KinectAzureRemoteStreams : Subpipeline
    {
        public KinectAzureRemoteStreamsConfiguration Configuration { get; private set; }
        public AzureKinectSensor? Sensor { get; private set; }

        public KinectAzureRemoteStreams(Pipeline pipeline, KinectAzureRemoteStreamsConfiguration? configuration = null, string name = nameof(KinectAzureRemoteStreams))
            : base(pipeline, name)
        {
            Configuration = configuration ?? new KinectAzureRemoteStreamsConfiguration();
        }

        public Rendezvous.Process GenerateProcess()
        {
            int portCount = Configuration.RendezVousPort + 1;

            AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
            configKinect.DeviceIndex = Configuration.KinectDeviceIndex;
            if (Configuration.StreamSkeleton == true)
                configKinect.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            Sensor = new AzureKinectSensor(this, configKinect);

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();
            if (Configuration.StreamAudio == true)
            {
                AudioCaptureConfiguration configuration = new AudioCaptureConfiguration();
                AudioCapture audioCapture = new AudioCapture(this, configuration);
                RemoteExporter soundExporter = new RemoteExporter(this, portCount++, Configuration.ConnectionType);
                soundExporter.Exporter.Write(audioCapture.Out, $"Kinect_{Configuration.RendezVousApplicationName}_Audio");
                exporters.Add(soundExporter.ToRendezvousEndpoint(Configuration.RendezVousAddress));
            }
            if (Configuration.StreamSkeleton == true)
            {
                RemoteExporter skeletonExporter = new RemoteExporter(this, portCount++, Configuration.ConnectionType);
                skeletonExporter.Exporter.Write(Sensor.Bodies, $"Kinect_{Configuration.RendezVousApplicationName}_Bodies");
                exporters.Add(skeletonExporter.ToRendezvousEndpoint(Configuration.RendezVousAddress));
            }
            if (Configuration.StreamVideo == true)
            {
                RemoteExporter imageExporter = new RemoteExporter(this, portCount++, Configuration.ConnectionType);
                if (Configuration.VideoResolution != null)
                    imageExporter.Exporter.Write(Sensor.ColorImage.Resize(Configuration.VideoResolution.Item1, Configuration.VideoResolution.Item2).EncodeJpeg(Configuration.EncodingVideoLevel), $"Kinect_{Configuration.RendezVousApplicationName}_RGB");
                else
                    imageExporter.Exporter.Write(Sensor.ColorImage.EncodeJpeg(Configuration.EncodingVideoLevel), $"Kinect_{Configuration.RendezVousApplicationName}_RGB");
                exporters.Add(imageExporter.ToRendezvousEndpoint(Configuration.RendezVousAddress));
            }
            if (Configuration.StreamDepth == true)
            {
                RemoteExporter depthExporter = new RemoteExporter(this, portCount++, Configuration.ConnectionType);
                depthExporter.Exporter.Write(Sensor.DepthImage.EncodePng(), $"Kinect_{Configuration.RendezVousApplicationName}_Depth");
                exporters.Add(depthExporter.ToRendezvousEndpoint(Configuration.RendezVousAddress));
            }
            if (Configuration.StreamDepthCalibration == true)
            {
                RemoteExporter depthCalibrationExporter = new RemoteExporter(this, portCount++, Configuration.ConnectionType);
                depthCalibrationExporter.Exporter.Write(Sensor.DepthDeviceCalibrationInfo, $"Kinect_{Configuration.RendezVousApplicationName}_Calibration");
                exporters.Add(depthCalibrationExporter.ToRendezvousEndpoint(Configuration.RendezVousAddress));
            }
            if (Configuration.StreamIMU == true)
            {
                RemoteExporter imuExporter = new RemoteExporter(this, portCount++, Configuration.ConnectionType);
                imuExporter.Exporter.Write(Sensor.Imu, $"Kinect_{Configuration.RendezVousApplicationName}_IMU");
                exporters.Add(imuExporter.ToRendezvousEndpoint(Configuration.RendezVousAddress));
            }

            return new Rendezvous.Process(Configuration.RendezVousApplicationName, exporters, "Version1.0");
        }
    }
}
