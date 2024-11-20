using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Imaging;

namespace SAAC.RemoteConnectors
{
    public class KinectAzureRemoteStreams
    {
        public KinectAzureRemoteStreamsConfiguration Configuration { get; private set; }
        public AzureKinectSensor? Sensor { get; private set; }
        protected Pipeline ParentPipeline;

        public KinectAzureRemoteStreams(Pipeline pipeline, KinectAzureRemoteStreamsConfiguration? configuration = null, string name = nameof(KinectAzureRemoteStreams))
        {
            ParentPipeline = pipeline;
            Configuration = configuration ?? new KinectAzureRemoteStreamsConfiguration();
        }

        public Rendezvous.Process GenerateProcess()
        {
            int portCount = Configuration.StartingPort + 1;

            AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
            configKinect.DeviceIndex = Configuration.KinectDeviceIndex;
            if (Configuration.StreamSkeleton == true)
                configKinect.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            Sensor = new AzureKinectSensor(ParentPipeline, configKinect);

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();
            if (Configuration.StreamAudio == true)
            {
                AudioCaptureConfiguration configuration = new AudioCaptureConfiguration();
                AudioCapture audioCapture = new AudioCapture(ParentPipeline, configuration);
                RemoteExporter soundExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                soundExporter.Exporter.Write(audioCapture.Out, $"Kinect_{Configuration.RendezVousApplicationName}_Audio");
                exporters.Add(soundExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.StreamSkeleton == true)
            {
                RemoteExporter skeletonExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                skeletonExporter.Exporter.Write(Sensor.Bodies, $"Kinect_{Configuration.RendezVousApplicationName}_Bodies");
                exporters.Add(skeletonExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.StreamVideo == true)
            {
                RemoteExporter imageExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                if (Configuration.VideoResolution != null)
                    imageExporter.Exporter.Write(Sensor.ColorImage.Resize(Configuration.VideoResolution.Item1, Configuration.VideoResolution.Item2).EncodeJpeg(Configuration.EncodingVideoLevel), $"Kinect_{Configuration.RendezVousApplicationName}_RGB");
                else
                    imageExporter.Exporter.Write(Sensor.ColorImage.EncodeJpeg(Configuration.EncodingVideoLevel), $"Kinect_{Configuration.RendezVousApplicationName}_RGB");
                exporters.Add(imageExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.StreamDepth == true)
            {
                RemoteExporter depthExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                depthExporter.Exporter.Write(Sensor.DepthImage.EncodePng(), $"Kinect_{Configuration.RendezVousApplicationName}_Depth");
                exporters.Add(depthExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.StreamDepthCalibration == true)
            {
                RemoteExporter depthCalibrationExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                depthCalibrationExporter.Exporter.Write(Sensor.DepthDeviceCalibrationInfo, $"Kinect_{Configuration.RendezVousApplicationName}_Calibration");
                exporters.Add(depthCalibrationExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.StreamIMU == true)
            {
                RemoteExporter imuExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                imuExporter.Exporter.Write(Sensor.Imu, $"Kinect_{Configuration.RendezVousApplicationName}_IMU");
                exporters.Add(imuExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }

            return new Rendezvous.Process(Configuration.RendezVousApplicationName, exporters, "Version1.0");
        }

        public void RunAsync()
        {
            ParentPipeline.RunAsync();
        }

        public void Dispose()
        {
            ParentPipeline.Dispose();
        }
    }
}
