// <copyright file="CallExample.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Speech;
using SAAC.CollaborationIndices.Verbal;
using SAAC.PipelineServices;
using SAAC.Whisper;
using Whisper.net.Ggml;

namespace SAAC.CollaborationIndices
{
    public class CallExample
    {
        public void StartProcess(DatasetPipeline server, string pipelineSessionName, Session session)
        {

            var SubPipeline = new Subpipeline(server.Pipeline, "CollaborationProcess");
            var Session = session;

            Console.WriteLine("Starting pipeline collaboration process...");

            List<uint> Pids = new List<uint>();

            for (int i = 0; i < participantNumber; i++)
            {
                Pids.Add((uint)i);
            }

            // Get data streams from existing dataset and stores

            GatherProducers gatherProducers = new GatherProducers();

            var registry = StreamRegistry.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, @"streams.json")));
            var resolver = new StreamResolver(registry, this.StoreMode, Console.WriteLine);
            IProducer<TimeSpan> timer66ms, timer1s;
            SetupTimers(out timer66ms, out timer1s);

            var IndexesWriter = new StreamWriter($@"{csvAdress}{sessionNumber}-20_individual_and_team_metrics_indexes.csv");
            var Indexes2Writer = new StreamWriter($@"{csvAdress}{sessionNumber}-2_individual_and_team_metrics_indexes.csv");

            var speechConfiguration = new SpeechProcessingConfiguration
            {
                ParticipantIds = Pids,
            };
            SpeechProcessing speechProcessing = new SpeechProcessing(SubPipeline, speechConfiguration);

            var template = new SlidingAverageConfiguration();
            template.UseTaskIndices = false;
            template.UseSpatialIndices = false;
            template.UseVerbalIndices = false;
            template.UseVisualIndices = false;
            template.UsePhysicalIndices = true;
            template.ComputeCollaborationScores = true;
            template.GenerateGraph = false;
            template.UseInternalClock = false;
            template.ParticipantIds = Pids;

            var indicesSet = new SlidingAverageComputationSet(SubPipeline, server, template, new Dictionary<TimeSpan, TextWriter>
            {
                { TimeSpan.FromSeconds(2), IndexesWriter },
                { TimeSpan.FromSeconds(20), Indexes2Writer },
                /*{ TimeSpan.FromSeconds(30), Indexes30Writer },
                { TimeSpan.FromSeconds(45), Indexes45Writer },*/
        });

            StreamsWriters.Add(IndexesWriter);
            StreamsWriters.Add(Indexes2Writer);
            indicesSet.ConnectClocks(timer1s, timer66ms);

            if (this.IsMicrophoneRecording)
            {
                gatherProducers.Audios = resolver.GetAudioProducers(server, this.SubPipeline, "UserAudio", participantNumber);
                int i = 0;
                foreach (var audio in gatherProducers.Audios)
                {
                    var name = gatherProducers.Audios[i].Out.Name.ToString().Split(new[] { "->" }, StringSplitOptions.None)[0];
                    server.CreateConnectorAndStore($"{name}", "Audios", this.Session, this.SubPipeline, typeof(AudioBuffer), gatherProducers.Audios[i]);
                    gatherProducers.Audios[i].PipeTo(new WaveFileWriter(this.SubPipeline, $@"{csvAdress}\AudioFiles\{this.sessionNumber}_{this.condition}_{name}.wav"));

                    IProducer<bool> vad;
                    IProducer<IStreamingSpeechRecognitionResult> stt;
                    var setupvadwhisper = new VadWhisper();

                    var resampledAudio = gatherProducers.GetResampledAudio(SubPipeline, gatherProducers.Audios[i]);

                    vad = setupvadwhisper.SetupVad(SubPipeline, server, new VadWhisperConfiguration { ParticipantId = i }, resampledAudio, i, speechProcessing);
                    var annotatedAudio = resampledAudio.Join(vad, RelativeTimeInterval.Past());

                    var whisperConfiguration = new WhisperSpeechRecognizerConfiguration
                    {
                        Language = Language.English,
                        ModelType = GgmlType.Medium,
                        QuantizationType = QuantizationType.NoQuantization,
                        ModelDirectory = @"C:\Users\aurel\Desktop\Fusion\WhisperModels\",
                        // SpecificModelPath = @"C:\Users\dapi\Desktop\WhisperModel\"
                    };

                    stt = setupvadwhisper.SetupWhisper(SubPipeline, server, new VadWhisperConfiguration { ParticipantId = i }, annotatedAudio, whisperConfiguration, i);
                    i++;
                }
            }

            if (this.IsServerInitialised)
            {
                gatherProducers.HeadPositionOrientationsUnity = gatherProducers.CreateTupleVector3Producers(server, this.SubPipeline, "Heads", "Head", "UnityServer-", participantNumber, this.StoreMode, false);
                gatherProducers.LeftsHandPositionOrientationsUnity = gatherProducers.CreateTupleVector3Producers(server, this.SubPipeline, "LeftHands", "LeftWrist", "UnityServer-", participantNumber, this.StoreMode, false);
                gatherProducers.RightsHandPositionOrientationsUnity = gatherProducers.CreateTupleVector3Producers(server, this.SubPipeline, "RightHands", "RightWrist", "UnityServer-", participantNumber, this.StoreMode, false);

                GatherSpecificStreamAccordingToExperiment(server, gatherProducers);
            }

            if (this.IsVideoInitialised && gatherProducers.Audios?.Count >= 2)
            {
                gatherProducers.ServerVideo = resolver.GetVideoProducer(server, this.SubPipeline, "SceneVideo");
                server.CreateConnectorAndStore("Unity Server", "Video", this.Session, this.SubPipeline, typeof(Shared<EncodedImage>), gatherProducers.ServerVideo);

                if (GenerateVideo)
                {
                    const uint TargetWidth = 1920;
                    const uint TargetHeight = 1080;
                    const int SampleRate = 48000;
                    bool hasAudio = true;

                    uint audioChannels = 1;
                    Func<AudioBuffer, AudioBuffer, AudioBuffer> mix = null;

                    switch (videoAudioType)
                    {
                        case "Mono":
                            audioChannels = 1;
                            var monoOut = WaveFormat.Create16BitPcm(SampleRate, 1);
                            mix = (a, b) => MixToMono(a, b, monoOut);
                            hasAudio = true;
                            break;
                        case "None":
                            hasAudio = false;
                            break;
                    }

                    this.videoExport = new FfmpegVideoExport(this.csvAdress, this.sessionNumber, this.condition, this.replayType, this.videoAudioType);
                    this.videoExport.CaptureVideo(gatherProducers.ServerVideo);

                    if (this.videoAudioType != "None" && gatherProducers.Audios?.Count != null)
                    {
                        for (int i = 0; i < gatherProducers.Audios.Count; i++)
                        {
                            this.videoExport.CaptureAudio(gatherProducers.Audios[i], i, this.condition, sessionNumber);
                        }
                    }
                }
            }

            for (int i = 0; i < participantNumber; i++)
            {
                PositionOrientationPreProcessing posRot = new PositionOrientationPreProcessing(
                    this.SubPipeline,
                    server,
                    new PositionOrientationConfiguration
                    {
                        userID = i,
                        sessionNum = this.sessionNumber,
                        csvAdress = $@"{this.csvAdress}\CSV",
                        condition = condition,
                        isOrb = false
                    });
                this.StreamsWriters.Add(posRot.positionrotationWriter);
                gatherProducers.HeadPositionOrientationsUnity[i].PipeTo(posRot.HeadPositionOrientationIn);
                gatherProducers.LeftsHandPositionOrientationsUnity[i].PipeTo(posRot.LeftHandPositionOrientationIn);
                gatherProducers.RightsHandPositionOrientationsUnity[i].PipeTo(posRot.RightHandPositionOrientationIn);
                gatherProducers.HeadPositions.Add(posRot.HeadPositionOut);
                gatherProducers.LeftHandsPositions.Add(posRot.LeftHandPositionOut);
                gatherProducers.RightHandsPositions.Add(posRot.RightHandPositionOut);
                // gatherProducers.HeadPositions[i].PipeTo(slidingAverage.CheckReceiverPositionUsers(i, "Head"));
                // gatherProducers.LeftHandsPositions[i].PipeTo(slidingAverage.CheckReceiverPositionUsers(i, "Left"));
                // gatherProducers.RightHandsPositions[i].PipeTo(slidingAverage.CheckReceiverPositionUsers(i, "Right"));

                indicesSet.ForEach(sa =>
                {
                    gatherProducers.HeadPositions[i].ToPositions(i).PipeTo(sa.ActivityLevel.GetPositionInput((uint)i, BodyPartNames.Head));
                    gatherProducers.LeftHandsPositions[i].ToPositions(i).PipeTo(sa.ActivityLevel.GetPositionInput((uint)i, BodyPartNames.LeftHand));
                    gatherProducers.RightHandsPositions[i].ToPositions(i).PipeTo(sa.ActivityLevel.GetPositionInput((uint)i, BodyPartNames.RightHand));
                    gatherProducers.HeadPositions[i].ToPositions(i).PipeTo(sa.Synchrony.GetPositionInput((uint)i, BodyPartNames.Head));
                });

                RecordOrbDataRossExperiment(server, gatherProducers, i);
                RecordInteractionsDataDespoinaExperiment(server, gatherProducers, i);
            }
            Console.WriteLine($"Currently {server.Connectors.Count} Connectors");

            server.TriggerNewProcessEvent("PipelineProcessInitialized");
            Console.WriteLine("Pipeline Initialized");

            this.SubPipeline.RunAsync();
        }
    }
}
