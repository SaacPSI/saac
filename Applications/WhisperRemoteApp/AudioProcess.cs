using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAAC.AudioRecording;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Speech;
using SAAC.PipelineServices;
using SAAC.Whisper;

namespace WhisperRemoteApp
{
    internal class AudioProcess
    {
        RendezVousPipelineConfiguration Configuration;
        WhisperRemoteConnectorConfiguration whisperConfiguration = new WhisperRemoteConnectorConfiguration();
        List<WhisperAudioProcessing> whisperAudios = new List<WhisperAudioProcessing>();
        Pipeline whisperSubP;
        public void StartAudioPipeline(RendezVousPipeline server, RendezVousPipelineConfiguration configuration, int connectedUser)
        {
            Configuration = configuration;
            Pipeline subP = server.CreateSubpipeline("Audio");
            //server.Log("Audio subpipeline initialization started");
            server.CreateOrGetSessionFromMode("Audio");
            // Audio
            SetupAudioRecording audioRecording = new SetupAudioRecording();
            audioRecording.SetupAudio(subP, server, false, server.Configuration.DatasetPath, connectedUser); //manual setup of the number of microphones connected
            //audioRecording.SetupAudioWithoutRDV(subP, server.GetSession("RawDataAudio.000"), false, server.Configuration.DatasetPath); //manual setup of the number of microphones connected
            subP.RunAsync();
        }

        public void StartWhisperPipeline(RendezVousPipeline server, RendezVousPipelineConfiguration configuration, int connectedUser)
        {
            /*WhisperRemoteConnectorConfiguration whisperConfiguration = new WhisperRemoteConnectorConfiguration(){ userConnected = connectedUser };
            WhisperRemoteConnector whisperConnector = new WhisperRemoteConnector(server.CreateSubpipeline("PipelineProcess"), whisperConfiguration);*/
            whisperSubP = server.CreateSubpipeline("PipelineProcess");

            // Verbal
            var audios = CreateAudioProducers(server, whisperSubP, "Audio", connectedUser);
            List<IProducer<bool>> vads = new List<IProducer<bool>>();
            List<IProducer<IStreamingSpeechRecognitionResult>> stts = new List<IProducer<IStreamingSpeechRecognitionResult>>();
            for (int i = 0; i < connectedUser; i++)
            {
                // Audio
                IProducer<bool> vad;
                IProducer<IStreamingSpeechRecognitionResult> stt;

                vad = SetupVad(server, whisperSubP, audios[i], i);
                vads.Add(vad);
                server.Log($"VAD_{i + 1} initialized");
                var annotatedAudio = audios[i].Join(vad);
                stt = TestWhisper(server, whisperSubP, annotatedAudio, i); // generated stt based on vad and audio. 'stt' is the stream of the stt event
                stts.Add(stt);
                server.Log($"Whisper_{i + 1} initialized");
            }
            server.Log("VAD and STT Initialized");
            server.AddProcess(GenerateProcess(vads, audios, stts));
            server.Log("Start sending Audio, VAD and STT to Main Pipeline");
            whisperSubP.RunAsync();
        }

        public static List<IProducer<AudioBuffer>> CreateAudioProducers(RendezVousPipeline server, Pipeline subP, string store, int numberOfQuests)
        {
            var producers = new List<IProducer<AudioBuffer>>();
            for (int i = 1; i <= numberOfQuests; i++)
            {
                var connectorKey = $"{i}_Audio";
                if (server.Connectors.ContainsKey(store))
                {
                    producers.Add(server.Connectors[store][connectorKey].CreateBridge<AudioBuffer>(subP));
                }
                else { return null; }
            }
            return producers;
        }
        private static IProducer<bool> SetupVad(RendezVousPipeline server, Pipeline subP, IProducer<AudioBuffer> audio, int id)
        {
            var config = new SystemVoiceActivityDetectorConfiguration()
            {
                Language = "fr-fr",
                Grammars = null,
                BufferLengthInMs = 500,
                VoiceActivityStartOffsetMs = -250,
                VoiceActivityEndOffsetMs = -250,
                InputFormat = WaveFormat.Create16kHz1Channel16BitPcm(),
                InitialSilenceTimeoutMs = 250,
                BabbleTimeoutMs = 1000,
                EndSilenceTimeoutAmbiguousMs = 250,
                EndSilenceTimeoutMs = 150
            };
            SystemVoiceActivityDetector vad = new SystemVoiceActivityDetector(subP, config);
            audio.PipeTo(vad.In);
            var sessionName = server.GetSession("RawDataPipelineProcess.000");
            server.CreateConnectorAndStore($"VAD_{id + 1}", "VAD", sessionName, subP, vad.GetType(), vad.Out, true);
            return vad.Out;
        }
        static IProducer<IStreamingSpeechRecognitionResult> TestWhisper(RendezVousPipeline server, Pipeline subP, IProducer<(AudioBuffer, bool)> annotatedAudioWhisper, int id)
        {
            WhisperSpeechRecognizerConfiguration configuration = new WhisperSpeechRecognizerConfiguration()
            {
                Language = Language.French,
                ModelType = Whisper.net.Ggml.GgmlType.Medium,
                QuantizationType = Whisper.net.Ggml.QuantizationType.Q5_0,
                ModelDirectory = @"C:\Path\...\WhisperModel\",
                LibrairyPath = @"C:\Path\...\whisper.dll"
            };

            var whisper = new WhisperSpeechRecognizer(subP, configuration);
                                                                                                                                                                                                                                                                                                       
            annotatedAudioWhisper.PipeTo(whisper);
            var finalWhisperResults = whisper.FinalOut.Where(result => result.IsFinal).Do((m, e) =>
            {
                e.CreationTime = e.OriginatingTime;
                server.Log($"{id + 1}_{m?.ToString()}");
            });
            var sessionName = server.GetSession("RawDataPipelineProcess.000");
            server.CreateConnectorAndStore($"STT_{id + 1}", "STT", sessionName, subP, typeof(IProducer<IStreamingSpeechRecognitionResult>), finalWhisperResults.Out, true);
            return finalWhisperResults;
        }
        public Rendezvous.Process GenerateProcess(List<IProducer<bool>> vads, List<IProducer<AudioBuffer>> audios, List<IProducer<IStreamingSpeechRecognitionResult>> stts)
        {
            int portCount = whisperConfiguration.ExportPort + 1;

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();

            for (int i = 0; i < whisperConfiguration.userConnected; i++)
            {
                RemoteExporter audioExporter = new RemoteExporter(whisperSubP, portCount++, whisperConfiguration.ConnectionType);
                audioExporter.Exporter.Write(audios[i], $"Audio_{i + 1}");
                exporters.Add(audioExporter.ToRendezvousEndpoint(whisperConfiguration.RendezVousAddress));
                RemoteExporter vadExporter = new RemoteExporter(whisperSubP, portCount++, whisperConfiguration.ConnectionType);
                vadExporter.Exporter.Write(vads[i], $"VAD_{i + 1}");
                exporters.Add(vadExporter.ToRendezvousEndpoint(whisperConfiguration.RendezVousAddress));
                RemoteExporter sttExporter = new RemoteExporter(whisperSubP, portCount++, whisperConfiguration.ConnectionType);
                sttExporter.Exporter.Write(stts[i], $"STT_{i + 1}");
                exporters.Add(sttExporter.ToRendezvousEndpoint(whisperConfiguration.RendezVousAddress));
            }
            return new Rendezvous.Process(whisperConfiguration.RendezVousApplicationName, exporters, "Version1.0");
        }
    }
}
