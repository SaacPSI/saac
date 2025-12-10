using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Interop.Rendezvous;
using System.Collections.Generic;
using SAAC.Nuitrack;
using SAAC.PipelineServices;

namespace SAAC.RemoteConnectors
{
    public class NuitrackRemoteStreamsComponent
    {
        public NuitrackRemoteStreamsConfiguration Configuration { get; private set; }
        public bool LocalStorage { get; private set; }
        public NuitrackSensor? Sensor { get; private set; }
        protected RendezVousPipeline server;
        protected Pipeline pipeline;
        private string name;

        public NuitrackRemoteStreamsComponent(RendezVousPipeline server, NuitrackRemoteStreamsConfiguration? configuration = null, bool localStorage = true, string name = nameof(NuitrackRemoteStreamsComponent))
        {
            this.server = server;
            this.name = name;
            LocalStorage = localStorage;
            Configuration = configuration ?? new NuitrackRemoteStreamsConfiguration();
        }

        public Rendezvous.Process GenerateProcess()
        {
            int portCount = Configuration.StartingPort + 1;
            Sensor = new NuitrackSensor(pipeline, Configuration);
            pipeline = server.GetOrCreateSubpipeline(name);
            var session = server.CreateOrGetSessionFromMode(Configuration.RendezVousApplicationName);
            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();
            if (Configuration.OutputSkeletonTracking == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Bodies";
                RemoteExporter skeletonExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                skeletonExporter.Exporter.Write(Sensor.OutBodies, streamName);
                exporters.Add(skeletonExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.OutBodies.GetType(), Sensor.OutBodies, LocalStorage);
            }
            if (Configuration.OutputColor == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_RGB";
                RemoteExporter imageExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                var compressed = Sensor.OutColorImage.EncodeJpeg(Configuration.EncodingVideoLevel);
                imageExporter.Exporter.Write(compressed, streamName);
                exporters.Add(imageExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, compressed.GetType(), compressed, LocalStorage);

            }
            if (Configuration.OutputDepth == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Depth";
                RemoteExporter depthExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                var compressed = Sensor.OutDepthImage.EncodePng();
                depthExporter.Exporter.Write(compressed, streamName);
                exporters.Add(depthExporter.ToRendezvousEndpoint(Configuration.IpToUse)); 
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, compressed.GetType(), compressed, LocalStorage);
            }
            if (Configuration.OutputHandTracking == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Hands";
                RemoteExporter handsExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                handsExporter.Exporter.Write(Sensor.OutHands, streamName);
                exporters.Add(handsExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.OutHands.GetType(), Sensor.OutHands, LocalStorage);
            }
            if (Configuration.OutputUserTracking == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Users";
                RemoteExporter usersExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                usersExporter.Exporter.Write(Sensor.OutUsers, streamName);
                exporters.Add(usersExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.OutUsers.GetType(), Sensor.OutUsers, LocalStorage);
            }
            if (Configuration.OutputGestureRecognizer == true)
            {
                string streamName = $"{Configuration.RendezVousApplicationName}_Gestures";
                RemoteExporter gesturesExporter = new RemoteExporter(pipeline, portCount++, Configuration.ConnectionType);
                gesturesExporter.Exporter.Write(Sensor.OutGestures, streamName);
                exporters.Add(gesturesExporter.ToRendezvousEndpoint(Configuration.IpToUse));
                server.CreateConnectorAndStore(streamName, $"{Configuration.RendezVousApplicationName}-{streamName}", session, pipeline, Sensor.OutGestures.GetType(), Sensor.OutGestures, LocalStorage);
            }

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
