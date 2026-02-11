// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Remoting;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Provides remote streaming capabilities for Whisper audio processing.
    /// </summary>
    public class WhisperRemoteStreams
    {
        private Pipeline pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhisperRemoteStreams"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="name">The name of the component.</param>
        public WhisperRemoteStreams(Pipeline pipeline, WhisperRemoteStreamsConfiguration? configuration = null, string name = nameof(WhisperRemoteStreams))
        {
            this.pipeline = pipeline;
            this.Name = name;
            this.Configuration = configuration ?? new WhisperRemoteStreamsConfiguration();
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public WhisperRemoteStreamsConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Generates a rendezvous process for the given producers.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="producersByUsers">The producers by users.</param>
        /// <returns>The rendezvous process.</returns>
        public static Rendezvous.Process GenerateProcess(Pipeline pipeline, WhisperRemoteStreamsConfiguration configuration, Dictionary<string, (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>)> producersByUsers)
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

        /// <summary>
        /// Generates a rendezvous process for the given producers.
        /// </summary>
        /// <param name="producersByUsers">The producers by users.</param>
        /// <returns>The rendezvous process.</returns>
        public Rendezvous.Process GenerateProcess(Dictionary<string, (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>)> producersByUsers)
        {
            return GenerateProcess(this.pipeline, this.Configuration, producersByUsers);
        }

        /// <inheritdoc/>
        public override string ToString() => this.Name;
    }
}
