// <copyright file="" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System.IO;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;
using SAAC.Components.CollaborationModules;
using SAAC.PipelineServices;

namespace ServerApplication.Examples
{
    public class RealTimeProcessingUseCaseConfiguration
    {
        // General

        /// <summary>
        /// Boolean flags to keep track of the state of Conversational Checkbox.
        /// </summary>
        public bool IsConversationalEnabled = false;

        /// <summary>
        /// Boolean flags to keep track of the state of Visual Checkbox.
        /// </summary>
        public bool IsVisualEnabled = false;

        /// <summary>
        /// Boolean flags to keep track of the state of Physical Checkbox.
        /// </summary>
        public bool IsPhysicalEnabled = false;

        /// <summary>
        /// Boolean flags to keep track of the state of Spatial Checkbox.
        /// </summary>
        public bool IsSpatialEnabled = false;

        // Conversational

        /// <summary>
        /// Boolean flags to keep track of the state of TurnTakingWithOverlap Checkbox.
        /// </summary>
        public bool IsTurnTakingWithOverlap = false;

        /// <summary>
        /// Boolean flags to keep track of the state of TurnTakingWithoutOverlap Checkbox.
        /// </summary>
        public bool IsTurnTakingWithoutOverlap = false;
        /// <summary>
        /// Boolean flags to keep track of the state of SpeechParticipation Checkbox.
        /// </summary>
        public bool IsSpeechParticipation = false;

        /// <summary>
        /// Boolean flags to keep track of the state of SpeechEquality Checkbox.
        /// </summary>
        public bool IsSpeechEquality = false;

        /// <summary>
        /// Boolean flags to keep track of the state of Silence Checkbox.
        /// </summary>
        public bool IsSilence = false;

        /// <summary>
        /// Boolean flags to keep track of the state of CrossTalk Checkbox.
        /// </summary>
        public bool IsCrossTalk = false;

        // Visual

        /// <summary>
        /// Boolean flags to keep track of the state of JointVisualAttention Checkbox.
        /// </summary>
        public bool IsJointVisualAttention = false;

        /// <summary>
        /// Boolean flags to keep track of the state of MutualGaze Checkbox.
        /// </summary>
        public bool IsMutualGaze = false;

        /// <summary>
        /// Boolean flags to keep track of the state of GazeOnPeers Checkbox.
        /// </summary>
        public bool IsGazeOnPeers = false;


        // Physical

        /// <summary>
        /// Boolean flags to keep track of the state of TaskParticipation Checkbox.
        /// </summary>
        public bool IsTaskParticipation = false;

        /// <summary>
        /// Boolean flags to keep track of the state of TaskEquality Checkbox.
        /// </summary>
        public bool IsTaskEquality = false;

        /// <summary>
        /// Boolean flags to keep track of the state of PhysicalActivityLevel Checkbox.
        /// </summary>
        public bool IsPhysicalActivityLevel = false;

        /// <summary>
        /// Boolean flags to keep track of the state of PhysicalSynchronyScore Checkbox.
        /// </summary>
        public bool IsPhysicalSynchronyScore = false;

        // Spatial

        /// <summary>
        /// Boolean flags to keep track of the state of PhysicalProximity Checkbox.
        /// </summary>
        public bool IsPhysicalProximity = false;

        /// <summary>
        /// Boolean flags to keep track of the state of FacingFormation Checkbox.
        /// </summary>
        public bool IsFacingFormation = false;

        /// <summary>
        /// Boolean flags to keep track of the state of SlidingWindow Checkbox.
        /// </summary>
        public bool IsSlidingWindowEnabled = false;

        /// <summary>
        /// Boolean flags to keep track of the state of CollaborationProfile Checkbox.
        /// </summary>
        public bool IsCollaborationProfileEnabled = false;
    }

    public class RealTimeProcessingUseCase
    {
        /// <summary>
        /// Configuration module.
        /// </summary>
        public RealTimeProcessingUseCaseConfiguration Configuration = new RealTimeProcessingUseCaseConfiguration();

        /// <summary>
        /// List of all the text writers that are used to write data to CSV files for the different modules in the pipeline.
        /// </summary>
        public List<TextWriter> StreamsWriters = new List<TextWriter>();

        /// <summary>
        /// Boolean flags to keep track of the state of the pipeline.
        /// </summary>
        public bool IsPipelineInitialised = false;

        /// <summary>
        /// Boolean flags to keep track of the state of Whisper microphone app recording.
        /// </summary>
        public bool IsMicrophoneRecording = false;

        /// <summary>
        /// Boolean flags to keep track of the state of Video app recording.
        /// </summary>
        public bool IsVideoInitialised = false;

        /// <summary>
        /// Boolean flags to keep track of the state of Unity server app recording.
        /// </summary>
        public bool IsServerInitialised = false;

        /// <summary>
        /// Boolean flags to keep track of the state of Server app recording.
        /// </summary>
        public bool IsPsiPipelineStarted = false;

        /// <summary>
        /// Boolean flags to keep track of the state of CSV writers.
        /// </summary>
        public bool WritersDisposed = false;

        /// <summary>
        /// Boolean flags to keep track of the state of CSV writers.
        /// </summary>
        public Subpipeline? SubPipeline = null;

        /// <summary>
        /// Boolean flags to keep track of the state of CSV writers.
        /// </summary>
        public Session? Session;

        public RendezVousPipeline.StoreMode StoreMode;

        public string csvAdress = string.Empty; // Get the dataset Path instead
        public int sessionNumber;
        public TextWriter positionrotationAWriter;
        public TextWriter positionrotationBWriter;
        public TextWriter positionrotationCWriter;
        public TextWriter taskInteractionEventWriter;

        public string interactionEventHeadupEU = "utc_timestamp_ms,participant_id,color_id,interaction_type,interaction_state,object_id,area".Replace(',', ';');
        public Dictionary<string, List<string>> stringsMessage = new Dictionary<string, List<string>>();
        private bool isHeadup;

        /// <summary>
        /// ...
        /// </summary>
        public void WriteCSV()
        {
            this.positionrotationAWriter = new StreamWriter($@"{this.csvAdress}{sessionNumber}-A_position_orientation.csv");
            this.positionrotationBWriter = new StreamWriter($@"{this.csvAdress}{sessionNumber}-B_position_orientation.csv");
            this.positionrotationCWriter = new StreamWriter($@"{this.csvAdress}{sessionNumber}-C_position_orientation.csv");
            this.taskInteractionEventWriter = new StreamWriter($@"{this.csvAdress}{sessionNumber}-interaction_event.csv");

            if (!this.isHeadup)
            {
                /*this.positionrotationAWriter.WriteLine(this.headWristHeadupEU);
                this.positionrotationBWriter.WriteLine(this.headWristHeadupEU);
                this.positionrotationCWriter.WriteLine(this.headWristHeadupEU);*/
                this.taskInteractionEventWriter.WriteLine(this.interactionEventHeadupEU);
                this.isHeadup = true;
            }

            foreach (var list in this.stringsMessage)
            {
                switch (list.Key)
                {
                    case "I1":
                        foreach (var value in list.Value)
                        {
                            this.taskInteractionEventWriter.WriteLine(value);
                        }

                        break;
                    case "I2":
                        foreach (var value in list.Value)
                        {
                            this.taskInteractionEventWriter.WriteLine(value);
                        }

                        break;
                    case "I3":
                        foreach (var value in list.Value)
                        {
                            this.taskInteractionEventWriter.WriteLine(value);
                        }

                        break;
                    case "P1":
                        foreach (var value in list.Value)
                        {
                            this.positionrotationAWriter.WriteLine(value);
                        }

                        break;
                    case "P2":
                        foreach (var value in list.Value)
                        {
                            this.positionrotationBWriter.WriteLine(value);
                        }

                        break;
                    case "P3":
                        foreach (var value in list.Value)
                        {
                            this.positionrotationCWriter.WriteLine(value);
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// ...
        /// </summary>
        public void StartPipelineCollaborationProcess(DatasetPipeline server, string pipelineSessionName, Session session)
        {
            int numberOfConnectedUsers = 4;
            GatherProducers gatherProducers = new GatherProducers();

            this.SubPipeline = new Subpipeline(server.Pipeline, "CollaborationProcess");
            this.Session = session;
            server.Log("Starting pipeline collaboration process...");

            if (this.IsMicrophoneRecording)
            {
                gatherProducers.Audios = gatherProducers.GetAudioProducers(server, this.SubPipeline, "WhisperStreaming", "Audio_", numberOfConnectedUsers, this.StoreMode);
                gatherProducers.Vads = gatherProducers.GetVadProducers(server, this.SubPipeline, "WhisperStreaming", "VAD_", numberOfConnectedUsers, this.StoreMode, true);
                gatherProducers.Stts = gatherProducers.GetSTTProducers(server, this.SubPipeline, "WhisperStreaming", "STT_", numberOfConnectedUsers, this.StoreMode, true);
            }

            if (this.IsServerInitialised)
            {
                gatherProducers.HeadPositionOrientationsUnity = gatherProducers.CreateTupleVector3Producers(server, this.SubPipeline, "Heads", "Head", "UnityServer-", numberOfConnectedUsers, this.StoreMode);
                gatherProducers.LeftsHandPositionOrientationsUnity = gatherProducers.CreateTupleVector3Producers(server, this.SubPipeline, "LeftHands", "LeftWrist", "UnityServer-", numberOfConnectedUsers, this.StoreMode);
                gatherProducers.RightsHandPositionOrientationsUnity = gatherProducers.CreateTupleVector3Producers(server, this.SubPipeline, "RightHands", "RightWrist", "UnityServer-", numberOfConnectedUsers, this.StoreMode);

                // gatherProducers.TaskLogs = gatherProducers.CreatePieceInteractionProducers(server, this.SubPipeline, "Task", "Interactions", numberOfConnectedUsers, this.StoreMode);

                // gatherProducers.LeftGazeEventsStrings = gatherProducers.CreateStringProducers(server, this.SubPipeline, "Gaze", "GazeEventString", numberOfConnectedUsers);
                // gatherProducers.LeftGazeEvents = gatherProducers.CreateGazeProducers(server, this.SubPipeline, "Gaze", "GazeEvent", numberOfConnectedUsers, this.StoreMode);
            }

            if (this.IsVideoInitialised)
            {
                gatherProducers.ServerVideo = server.Connectors["VideoRemoteApp-FullScreen"]["FullScreen"].CreateBridge<Shared<EncodedImage>>(this.SubPipeline);
                server.CreateConnectorAndStore("Unity Server", "Video", this.Session, this.SubPipeline, typeof(Shared<EncodedImage>), gatherProducers.ServerVideo);
            }

            // LofComputerConfig drives the grid and room geometry.
            // Adjust RoomCenter / RoomRadius to match the physical room:
            //   "big"   room → center (-22.5, 0), radius 26.5 m  (configurations.py)
            //   "small" room → center (-13.5, 0), radius  2.5 m
            var lofConfig = new LofComputerConfig
            {
                RoomCenter = new System.Numerics.Vector2(-22.5f, 0f),
                RoomRadius = 26.5f,
                FovDegrees = 104f,          // HMD horizontal FOV (φ)
                RangeFalloff = 15f,           // range scale α
                RangeExponent = 2f,            // range exponent β
                Threshold = 0.9f,
                GridResolution = 64,            // 64×64 grid, good real-time balance
                StoreField = true,          // keep heatmap array for the visualiser
                KfInitialCov = 1e-3,
                KfTransitionCov = 3e-3,
                KfObservationCov = 1e-1,
                Dt = 1.0 / 30.0,    // expected frame rate
            };

            LOF lof = new LOF(
                this.SubPipeline,
                server,
                numberOfConnectedUsers,
                lofConfig,
                this.Session // session used by PSI store connector
            );

            for (int i = 0; i < numberOfConnectedUsers; i++)
            {
                gatherProducers.HeadPositionOrientationsUnity[i].PipeTo(lof.GetReceiver(i));
                PositionOrientationPreProcessing posRot = new PositionOrientationPreProcessing(
                    this.SubPipeline,
                    server,
                    new PositionOrientationConfiguration
                    {
                        userID = i,
                        sessionNum = sessionNumber,
                        csvAdress = this.csvAdress
                    });
                this.StreamsWriters.Add(posRot.positionrotationWriter);
                gatherProducers.HeadPositionOrientationsUnity[i].PipeTo(posRot.HeadPositionOrientationIn);
                gatherProducers.LeftsHandPositionOrientationsUnity[i].PipeTo(posRot.LeftHandPositionOrientationIn);
                gatherProducers.RightsHandPositionOrientationsUnity[i].PipeTo(posRot.RightHandPositionOrientationIn);
            }

            this.SubPipeline.RunAsync();

            // gatherProducers.LeftGazeEvents = gatherProducers.CreateGazeProducers(server, this.SubPipeline, "Gaze", "GazeEvent", numberOfConnectedUsers);

            // Console.WriteLine("Starting pipeline collaboration process...");
            // 1. Create a pipeline collaboration module (PCM) and add it to the server
            // 2. The PCM will have receivers and emitters that will be connected to other modules in the pipeline
            // 3. The PCM will process data in real-time as it is received from the emitters and send processed data to the receivers
            // 4. The PCM can also send status updates or logs to a monitoring module or UI
        }

        /// <summary>
        /// ...
        /// </summary>
        public void CloseAndDisposeWriter(TextWriter writer)
        {
            StreamWriter streamWriter = (StreamWriter)writer;
            if (writer == null)
            {
                return;
            }

            if (streamWriter.BaseStream.CanWrite)
            {
                try
                {
                    writer.Flush();
                }
                catch (ObjectDisposedException) { Console.WriteLine($"TextWriter {writer.ToString()} Flush exception"); }
                try
                {
                    writer.Close();
                }
                catch (ObjectDisposedException) { Console.WriteLine($"TextWriter {writer.ToString()} Close exception"); }
                try
                {
                    writer.Dispose();
                }
                catch (ObjectDisposedException) { Console.WriteLine($"TextWriter {writer.ToString()} Dispose exception"); }
            }
        }
    }
}
