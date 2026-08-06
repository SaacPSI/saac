// <copyright file="VadWhisperComponent.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Protocols.WSTrust;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Components;
using Microsoft.Psi.Data;
using Microsoft.Psi.Speech;
using SAAC.PipelineServices;
using SAAC.Whisper;
using Whisper.net.Ggml;

namespace SAAC.CollaborationIndices.Verbal
{
    public class VadWhisperConfiguration
    {
        public int ParticipantId { get; set; } = 0;

    }

    public class VadWhisper
    {
        VadWhisperConfiguration vadWhisperConfiguration;

        public IProducer<bool> SetupVad(Pipeline subpipeline, DatasetPipeline server, VadWhisperConfiguration configuration, IProducer<AudioBuffer> audio, int id, SpeechProcessing process)
        {
            this.vadWhisperConfiguration = configuration;
            var sessionName = server.GetSession("RawDataPipelineProcess.000");

            // Write vad for referential
            var config = new SystemVoiceActivityDetectorConfiguration()
            {
                Language = "en-Gb",
                Grammars = null,
                BufferLengthInMs = 1000,
                VoiceActivityStartOffsetMs = -250,
                VoiceActivityEndOffsetMs = -250,
                InputFormat = WaveFormat.Create16kHz1Channel16BitPcm(),
                InitialSilenceTimeoutMs = 0,
                BabbleTimeoutMs = 0,
                EndSilenceTimeoutAmbiguousMs = 200,
                EndSilenceTimeoutMs = 150
            };
            SystemVoiceActivityDetector vad = new SystemVoiceActivityDetector(subpipeline, config);

            audio.PipeTo(vad.In);

            var audioFeatures = new AcousticFeaturesExtractor(subpipeline);
            DateTime time = DateTime.MinValue;

            /*audio.PipeTo(audioFeatures.In);
            audioFeatures.LogEnergy.PipeTo(process.CheckLogReceiver(id));

            // Create a voice-activity stream by thresholding the log energy
            var vadWithHistory = audioFeatures.LogEnergy
                .Window(RelativeTimeInterval.Past(TimeSpan.FromMilliseconds(450)))
                .Select(buffer =>
                {
                    bool value = false;
                    if (!buffer.Any())
                    {
                        return value;
                    }

                    double avg = buffer.Average();
                    double last = buffer.Last();
                    double recentAvg = buffer.Skip(Math.Max(0, buffer.Count() - 3)).Average(); // ≈ 90 ms

                    bool trigger = avg > 4.6 || last > 6.5 || recentAvg > 5;

                    return trigger;
                }

                );
            vadWithHistory.PipeTo(process.CheckVadReceiver(id));

            var value = vad.Join(vadWithHistory, DeliveryPolicy.LatestMessage);*/

            server.CreateConnectorAndStore($"VAD_{this.vadWhisperConfiguration.ParticipantId + 1}", "LiveVisualization", sessionName, subpipeline, typeof(bool), vad.Out, true);

            // server.CreateConnectorAndStore($"VAD_LOG_{this.vadWhisperConfiguration.ParticipantId + 1}", "LiveVisualization", sessionName, subpipeline, typeof(bool), vadWithHistory.Out, true);
            server.CreateConnectorAndStore($"LOG_{this.vadWhisperConfiguration.ParticipantId + 1}", "LiveVisualization", sessionName, subpipeline, typeof(float), audioFeatures.LogEnergy.Out, true);
            return vad;
        }

        public IProducer<IStreamingSpeechRecognitionResult> SetupWhisper(Pipeline subpipeline, DatasetPipeline server, VadWhisperConfiguration vadconfiguration, IProducer<(AudioBuffer, bool)> annotatedAudioWhisper, WhisperSpeechRecognizerConfiguration configuration, int id)
        {
            this.vadWhisperConfiguration = vadconfiguration;
            var whisper = new WhisperSpeechRecognizer(subpipeline, configuration);
            var sessionName = server.GetSession("RawDataPipelineProcess.000");

            annotatedAudioWhisper.PipeTo(whisper);

            var finalWhisperResults = whisper.FinalOut.Where(result => result.IsFinal).Do((m, e) =>
            {
                e.CreationTime = e.OriginatingTime;
                Console.WriteLine($"{id}_{m?.ToString()}");
            });
            server.CreateConnectorAndStore($"STT_{this.vadWhisperConfiguration.ParticipantId + 1}", "LiveVisualization", sessionName, subpipeline, finalWhisperResults.GetType(), finalWhisperResults.Out, true);

            /*exporter.Write(whisper.PartialOut.Where(presult =>!presult.IsFinal)
                .Do((m, e) =>
                {
                    e.CreationTime = e.OriginatingTime;

                }), $"STT_Partial_{id}");*/

            return finalWhisperResults;
        }
    }
}
