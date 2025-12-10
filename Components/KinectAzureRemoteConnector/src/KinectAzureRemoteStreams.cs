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


            if (Configuration.OutputBodies == true)
                Configuration.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            Sensor = new AzureKinectSensor(ParentPipeline, Configuration);

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();
            if (Configuration.OutputAudio == true)
            {
                AudioCaptureConfiguration configuration = new AudioCaptureConfiguration();
                AudioCapture audioCapture = new AudioCapture(ParentPipeline, configuration);
                RemoteExporter soundExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                soundExporter.Exporter.Write(audioCapture.Out, $"{Configuration.RendezVousApplicationName}_Audio");
                exporters.Add(soundExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.OutputBodies == true)
            {
                RemoteExporter skeletonExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                skeletonExporter.Exporter.Write(Sensor.Bodies, $"{Configuration.RendezVousApplicationName}_Bodies");
                exporters.Add(skeletonExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.OutputColor == true)
            {
                RemoteExporter imageExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                imageExporter.Exporter.Write(Sensor.ColorImage.EncodeJpeg(Configuration.EncodingVideoLevel), $"{Configuration.RendezVousApplicationName}_RGB");
                exporters.Add(imageExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.OutputInfrared == true)
            {
                RemoteExporter imageExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                imageExporter.Exporter.Write(Sensor.InfraredImage.EncodeJpeg(Configuration.EncodingVideoLevel), $"{Configuration.RendezVousApplicationName}_Infrared");
                exporters.Add(imageExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.OutputDepth == true)
            {
                RemoteExporter depthExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                depthExporter.Exporter.Write(Sensor.DepthImage.EncodePng(), $"{Configuration.RendezVousApplicationName}_Depth");
                exporters.Add(depthExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.OutputCalibration == true)
            {
                RemoteExporter depthCalibrationExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                depthCalibrationExporter.Exporter.Write(Sensor.DepthDeviceCalibrationInfo, $"{Configuration.RendezVousApplicationName}_Calibration");
                exporters.Add(depthCalibrationExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.OutputImu == true)
            {
                RemoteExporter imuExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                imuExporter.Exporter.Write(Sensor.Imu, $"{Configuration.RendezVousApplicationName}_IMU");
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
