using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Interop.Rendezvous;
using System.Collections.Generic;
using SAAC.Nuitrack;

namespace SAAC.RemoteConnectors
{
    public class NuitrackRemoteStreams
    {
        public NuitrackRemoteStreamsConfiguration Configuration { get; private set; }
        public NuitrackSensor? Sensor { get; private set; }
        protected Pipeline ParentPipeline;

        public NuitrackRemoteStreams(Pipeline pipeline, NuitrackRemoteStreamsConfiguration? configuration = null, string name = nameof(NuitrackRemoteStreams))
        {
            ParentPipeline = pipeline;
            Configuration = configuration ?? new NuitrackRemoteStreamsConfiguration();
        }

        public Rendezvous.Process GenerateProcess()
        {
            int portCount = Configuration.StartingPort + 1;

            Sensor = new NuitrackSensor(ParentPipeline, Configuration);

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();
            if (Configuration.OutputSkeletonTracking == true)
            {
                RemoteExporter skeletonExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                skeletonExporter.Exporter.Write(Sensor.OutBodies, $"{Configuration.RendezVousApplicationName}_Bodies");
                exporters.Add(skeletonExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.OutputColor == true)
            {
                RemoteExporter imageExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                imageExporter.Exporter.Write(Sensor.OutColorImage.EncodeJpeg(Configuration.EncodingVideoLevel), $"{Configuration.RendezVousApplicationName}_RGB");
                exporters.Add(imageExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.OutputDepth == true)
            {
                RemoteExporter depthExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                depthExporter.Exporter.Write(Sensor.OutDepthImage.EncodePng(), $"{Configuration.RendezVousApplicationName}_Depth");
                exporters.Add(depthExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.OutputHandTracking == true)
            {
                RemoteExporter handsExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                handsExporter.Exporter.Write(Sensor.OutHands, $"{Configuration.RendezVousApplicationName}_Hands");
                exporters.Add(handsExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.OutputUserTracking == true)
            {
                RemoteExporter usersExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                usersExporter.Exporter.Write(Sensor.OutUsers, $"{Configuration.RendezVousApplicationName}_Users");
                exporters.Add(usersExporter.ToRendezvousEndpoint(Configuration.IpToUse));
            }
            if (Configuration.OutputGestureRecognizer == true)
            {
                RemoteExporter gesturesExporter = new RemoteExporter(ParentPipeline, portCount++, Configuration.ConnectionType);
                gesturesExporter.Exporter.Write(Sensor.OutGestures, $"{Configuration.RendezVousApplicationName}_Gestures");
                exporters.Add(gesturesExporter.ToRendezvousEndpoint(Configuration.IpToUse));
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
