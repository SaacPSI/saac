using Microsoft.Psi.Kinect;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Remoting;
using Microsoft.Psi;
using SAAC.PipelineServices;
using Microsoft.Psi.Imaging;
using System.Collections.Generic;

namespace SAAC.RemoteConnectors
{
    public class KinectStreamsComponent
    {
        public KinectRemoteStreamsComponentConfiguration Configuration { get; private set; }
        public bool LocalStorage { get; private set; }
        public KinectSensor? Sensor { get; private set; }
        protected RendezVousPipeline server;
        protected Pipeline pipeline;
        private string name;

        public KinectStreamsComponent(RendezVousPipeline server, KinectRemoteStreamsComponentConfiguration? configuration = null, bool localStorage = true, string name = nameof(KinectStreamsComponent))
        {
            this.server = server;
            this.name = name;
            LocalStorage = localStorage;
            Configuration = configuration ?? new KinectRemoteStreamsComponentConfiguration();
        }

        public Rendezvous.Process GenerateProcess()
        {
            int portCount = Configuration.StartingPort + 1;
            pipeline = server.GetOrCreateSubpipeline(name);
            Sensor = new KinectSensor(pipeline, Configuration);
            var session = server.CreateOrGetSessionFromMode(Configuration.RendezVousApplicationName);

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();
            if (Configuration.OutputAudio == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Audio";
                RemoteExporter soundExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                soundExporter.Exporter.Write(Sensor.Audio, streamName);
                exporters.Add(soundExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.Audio.GetType(), Sensor.Audio, LocalStorage);
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
            if (Configuration.OutputRGBD == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_RGBD";
                RemoteExporter imageExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                var compressed = Sensor.RGBDImage.EncodeJpeg(Configuration.EncodingVideoLevel);
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
            if (Configuration.OutputInfrared == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Infrared";
                RemoteExporter depthExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                var compressed = Sensor.InfraredImage.EncodeJpeg(Configuration.EncodingVideoLevel);
                depthExporter.Exporter.Write(compressed, streamName);
                exporters.Add(depthExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, compressed.GetType(), compressed.Out, LocalStorage);
            }
            if (Configuration.OutputLongExposureInfrared == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_LongExposureInfrared";
                RemoteExporter depthExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                var compressed = Sensor.LongExposureInfraredImage.EncodeJpeg(Configuration.EncodingVideoLevel);
                depthExporter.Exporter.Write(compressed, streamName);
                exporters.Add(depthExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, compressed.GetType(), compressed.Out, LocalStorage);
            }
            if (Configuration.OutputColorToCameraMapping == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_ColorToCameraMapper";
                RemoteExporter depthCalibrationExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                depthCalibrationExporter.Exporter.Write(Sensor.ColorToCameraMapper, streamName);
                exporters.Add(depthCalibrationExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.ColorToCameraMapper.GetType(), Sensor.ColorToCameraMapper, LocalStorage);
            }
            if (Configuration.OutputCalibration == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Calibration";
                RemoteExporter imuExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                imuExporter.Exporter.Write(Sensor.DepthDeviceCalibrationInfo, streamName);
                exporters.Add(imuExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.DepthDeviceCalibrationInfo.GetType(), Sensor.DepthDeviceCalibrationInfo, LocalStorage);
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
