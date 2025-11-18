
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Speech;

namespace SAAC.RemoteConnectors
{
    public class WhisperRemoteStreams
    {
        public WhisperRemoteStreamsConfiguration Configuration { get; private set; }
        public string Name { get; private set; }

        protected Pipeline p;

        public WhisperRemoteStreams(Pipeline pipeline, WhisperRemoteStreamsConfiguration? configuration = null, string name = nameof(WhisperRemoteStreams))
        {
            p = pipeline;
            Name = name;
            Configuration = configuration ?? new WhisperRemoteStreamsConfiguration();
        }

        public override string ToString() => Name;

        public Rendezvous.Process GenerateProcess(Dictionary<string, (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>)> producersByUsers)
        {
            return GenerateProcess(p, Configuration, producersByUsers);
        }

        static public Rendezvous.Process GenerateProcess(Pipeline pipeline, WhisperRemoteStreamsConfiguration configuration, Dictionary<string, (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>)> producersByUsers)
        {
            int portCount = configuration.ExportPort + 1;
            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();

            foreach (var userProducers in producersByUsers)
            {
                (IProducer<AudioBuffer> audio, IProducer<bool> vad, IProducer<IStreamingSpeechRecognitionResult> stt) = userProducers.Value;

                RemoteExporter audioExporter = new RemoteExporter(pipeline, portCount++, configuration.ConnectionType);
                audioExporter.Exporter.Write(audio, $"Audio_{userProducers.Key}");
                exporters.Add(audioExporter.ToRendezvousEndpoint(configuration.RendezVousAddress));

                RemoteExporter vadExporter = new RemoteExporter(pipeline, portCount++, configuration.ConnectionType);
                vadExporter.Exporter.Write(vad, $"VAD_{userProducers.Key}");
                exporters.Add(vadExporter.ToRendezvousEndpoint(configuration.RendezVousAddress));

                RemoteExporter sttExporter = new RemoteExporter(pipeline, portCount++, configuration.ConnectionType);
                sttExporter.Exporter.Write(stt, $"STT_{userProducers.Key}");
                exporters.Add(sttExporter.ToRendezvousEndpoint(configuration.RendezVousAddress));
            }
            return new Rendezvous.Process(configuration.RendezVousApplicationName, exporters, "Version1.0");
        }
    }
}
