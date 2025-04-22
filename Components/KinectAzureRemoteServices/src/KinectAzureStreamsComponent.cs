using Microsoft.Psi.Audio;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Remoting;
using Microsoft.Psi;
using SAAC.RemoteConnectors;
using SAAC.RendezVousPipelineServices;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Data;

namespace SAAC.KinectAzureRemoteServices
{
    public class KinectAzureStreamsComponent
    {
        public KinectAzureRemoteStreamsConfiguration Configuration { get; private set; }
        public AzureKinectSensor? Sensor { get; private set; }
        protected RendezVousPipeline server;
        protected Pipeline pipeline;
        private string name;

        public KinectAzureStreamsComponent(RendezVousPipeline server, KinectAzureRemoteStreamsConfiguration? configuration = null, string name = nameof(KinectAzureStreamsComponent))
        {
            this.server = server;
            this.name = name;
            Configuration = configuration ?? new KinectAzureRemoteStreamsConfiguration();
        }

        public Rendezvous.Process GenerateProcess()
        {
            int portCount = Configuration.StartingPort + 1;

            AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
            configKinect.DeviceIndex = Configuration.KinectDeviceIndex;
            configKinect.ColorResolution = Configuration.VideoResolution;
            configKinect.CameraFPS = Configuration.FPS;
            if (Configuration.StreamSkeleton == true)
                configKinect.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            pipeline = server.CreateSubpipeline(name);
            Sensor = new AzureKinectSensor(pipeline, configKinect);
            var session = server.CreateOrGetSessionFromMode(Configuration.RendezVousApplicationName);

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();
            if (Configuration.StreamAudio == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Audio";
                AudioCaptureConfiguration configuration = new AudioCaptureConfiguration();
                AudioCapture audioCapture = new AudioCapture(pipeline, configuration);
                RemoteExporter soundExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                soundExporter.Exporter.Write(audioCapture.Out, streamName);
                exporters.Add(soundExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}" , session, pipeline, audioCapture.GetType(), audioCapture.Out);
            }
            if (Configuration.StreamSkeleton == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Bodies";
                RemoteExporter skeletonExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                skeletonExporter.Exporter.Write(Sensor.Bodies, streamName);
                exporters.Add(skeletonExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.Bodies.GetType(), Sensor.Bodies);
            }
            if (Configuration.StreamVideo == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_RGB";
                RemoteExporter imageExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                imageExporter.Exporter.Write(Sensor.ColorImage.EncodeJpeg(Configuration.EncodingVideoLevel), streamName);
                exporters.Add(imageExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.ColorImage.EncodeJpeg(Configuration.EncodingVideoLevel).GetType(), Sensor.ColorImage.EncodeJpeg(Configuration.EncodingVideoLevel).Out);
            }
            if (Configuration.StreamDepth == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Depth";
                RemoteExporter depthExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                depthExporter.Exporter.Write(Sensor.DepthImage.EncodePng(), streamName);
                exporters.Add(depthExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.DepthImage.EncodePng().GetType(), Sensor.DepthImage.EncodePng().Out);
            }
            if (Configuration.StreamDepthCalibration == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Calibration";
                RemoteExporter depthCalibrationExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                depthCalibrationExporter.Exporter.Write(Sensor.DepthDeviceCalibrationInfo, streamName);
                exporters.Add(depthCalibrationExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.DepthDeviceCalibrationInfo.GetType(), Sensor.DepthDeviceCalibrationInfo);
            }
            if (Configuration.StreamIMU == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_IMU";
                RemoteExporter imuExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                imuExporter.Exporter.Write(Sensor.Imu, streamName);
                exporters.Add(imuExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.Imu.GetType(), Sensor.Imu);
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
