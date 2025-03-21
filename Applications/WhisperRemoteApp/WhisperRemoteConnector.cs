using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Speech;

namespace WhisperRemoteApp
{
    public class WhisperRemoteConnectorConfiguration
    {
        public string RendezVousAddress { get; set; } = "localhost";
        public int RendezVousPort { get; set; } = 13331;
        public int ExportPort { get; set; } = 11570;
        public TransportKind ConnectionType { get; set; } = TransportKind.Tcp;
        public string RendezVousApplicationName { get; set; } = "WhisperStreaming";
        public int userConnected = 3;
    }

    public class WhisperRemoteConnector : Subpipeline
    {
        public WhisperRemoteConnectorConfiguration Configuration { get; private set; }
        protected Pipeline p;
        public WhisperRemoteConnector(Pipeline pipeline, WhisperRemoteConnectorConfiguration? configuration = null, string name = nameof(WhisperRemoteConnector)) : base(pipeline, name)
        {
            p = pipeline;
            Configuration = configuration ?? new WhisperRemoteConnectorConfiguration();
        }
        public Rendezvous.Process GenerateProcess(List<IProducer<bool>> vads, List<IProducer<AudioBuffer>> audios, List<IProducer<IStreamingSpeechRecognitionResult>> stts)
        {
            int portCount = Configuration.ExportPort + 1;

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();

            for(int i = 0; i < Configuration.userConnected; i++)
            {
                RemoteExporter audioExporter = new RemoteExporter(p, portCount++, Configuration.ConnectionType);
                audioExporter.Exporter.Write(audios[i], $"Audio_{i + 1}");
                exporters.Add(audioExporter.ToRendezvousEndpoint(Configuration.RendezVousAddress));
                RemoteExporter vadExporter = new RemoteExporter(p, portCount++, Configuration.ConnectionType);
                vadExporter.Exporter.Write(vads[i], $"VAD_{i + 1}");
                exporters.Add(vadExporter.ToRendezvousEndpoint(Configuration.RendezVousAddress));
                RemoteExporter sttExporter = new RemoteExporter(p, portCount++, Configuration.ConnectionType);
                sttExporter.Exporter.Write(stts[i], $"STT_{i + 1}");
                exporters.Add(sttExporter.ToRendezvousEndpoint(Configuration.RendezVousAddress));
            }
            return new Rendezvous.Process(Configuration.RendezVousApplicationName, exporters, "Version1.0");
        }
    }
}
