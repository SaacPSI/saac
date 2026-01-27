using Microsoft.Psi.Audio;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Remoting;
using Microsoft.Psi;
using SAAC.RemoteConnectors;
using SAAC.PipelineServices;
using Microsoft.Psi.Imaging;

namespace SAAC.RemoteConnectors
{
    public class KinectAzureStreamsComponent
    {
        public KinectAzureRemoteStreamsConfiguration Configuration { get; private set; }
        public bool LocalStorage { get; private set; }
        public AzureKinectSensor? Sensor { get; private set; }
        protected RendezVousPipeline server;
        protected Pipeline pipeline;
        private string name;

        public KinectAzureStreamsComponent(RendezVousPipeline server, KinectAzureRemoteStreamsConfiguration? configuration = null, bool localStorage = true, string name = nameof(KinectAzureStreamsComponent))
        {
            this.server = server;
            this.name = name;
            LocalStorage = localStorage;
            Configuration = configuration ?? new KinectAzureRemoteStreamsConfiguration();
        }

        public Rendezvous.Process GenerateProcess()
        {
            int portCount = Configuration.StartingPort + 1;

            if (Configuration.OutputBodies == true)
            {
                Configuration.OutputDepth = Configuration.OutputInfrared = Configuration.OutputCalibration = true;
                Configuration.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            }

            pipeline = server.GetOrCreateSubpipeline(name);
            Sensor = new AzureKinectSensor(pipeline, Configuration);
            var session = server.CreateOrGetSessionFromMode(Configuration.RendezVousApplicationName);

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();
            if (Configuration.OutputAudio == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Audio";
                AudioCaptureConfiguration configuration = new AudioCaptureConfiguration();
                int index = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevices().ToList().FindIndex(value => { return value.Contains("Azure"); });
                configuration.DeviceName = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevices().ElementAt(index);
                AudioCapture audioCapture = new AudioCapture(pipeline, configuration);
                RemoteExporter soundExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                soundExporter.Exporter.Write(audioCapture.Out, streamName);
                exporters.Add(soundExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}" , session, pipeline, audioCapture.GetType(), audioCapture.Out, LocalStorage);
            }
            if (Configuration.OutputBodies == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Bodies";
                RemoteExporter skeletonExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                skeletonExporter.Exporter.Write(Sensor.Bodies, streamName);
                exporters.Add(skeletonExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.Bodies.GetType(), Sensor.Bodies, LocalStorage);
            }
            if (Configuration.OutputColor == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_RGB";
                RemoteExporter imageExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                var compressed = Sensor.ColorImage.EncodeJpeg(Configuration.EncodingVideoLevel);
                imageExporter.Exporter.Write(compressed, streamName);
                exporters.Add(imageExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, compressed.GetType(), compressed.Out, LocalStorage);
            }
            if (Configuration.OutputInfrared == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Infrared";
                RemoteExporter imageExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                var compressed = Sensor.InfraredImage.EncodeJpeg(Configuration.EncodingVideoLevel);
                imageExporter.Exporter.Write(compressed, streamName);
                exporters.Add(imageExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, compressed.GetType(), compressed.Out, LocalStorage);
            }
            if (Configuration.OutputDepth == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Depth";
                RemoteExporter depthExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                var compressed = Sensor.DepthImage.EncodePng();
                depthExporter.Exporter.Write(compressed, streamName);
                exporters.Add(depthExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, compressed.GetType(), compressed.Out, LocalStorage);
            }
            if (Configuration.OutputCalibration == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Calibration";
                RemoteExporter depthCalibrationExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                depthCalibrationExporter.Exporter.Write(Sensor.DepthDeviceCalibrationInfo, streamName);
                exporters.Add(depthCalibrationExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.DepthDeviceCalibrationInfo.GetType(), Sensor.DepthDeviceCalibrationInfo, LocalStorage);
            }
            if (Configuration.OutputImu == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_IMU";
                RemoteExporter imuExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                imuExporter.Exporter.Write(Sensor.Imu, streamName);
                exporters.Add(imuExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.Imu.GetType(), Sensor.Imu, LocalStorage);
            }

            server.TriggerNewProcessEvent(Configuration.RendezVousApplicationName);
            return new Rendezvous.Process(Configuration.RendezVousApplicationName, exporters, "Version1.0");
        }

        public void RunAsync()
        {
            pipeline.RunAsync();
        }

        public void Dispose()
        {
            pipeline.Dispose();
        }
    }
}
