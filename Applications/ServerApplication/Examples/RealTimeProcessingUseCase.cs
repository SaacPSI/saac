// <copyright file="" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System.IO;
using Microsoft.Psi;
using Microsoft.Psi.Data;
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

        /// <summary>
        /// ...
        /// </summary>
        public void StartPipelineCollaborationProcess(DatasetPipeline server, string pipelineSessionName)
        {
            int numberOfConnectedUsers = 2;
            GatherProducers gatherProducers = new GatherProducers();

            this.SubPipeline = new Subpipeline(server.Pipeline, "CollaborationProcess");
            this.Session = server.GetSession($"RawData{pipelineSessionName}.000");
            server.Log("Starting pipeline collaboration process...");

            gatherProducers.Audios = gatherProducers.GetAudioProducers(server, this.SubPipeline,"WhisperStreaming", "Audio_", numberOfConnectedUsers);
            gatherProducers.Vads = gatherProducers.GetVadProducers(server, this.SubPipeline,"WhisperStreaming", "VAD_", numberOfConnectedUsers, true);
            gatherProducers.Stts = gatherProducers.GetSTTProducers(server, this.SubPipeline,"WhisperStreaming", "STT_", numberOfConnectedUsers, true);
            gatherProducers.HeadPositionOrientationsUnity = gatherProducers.CreateTupleVector3Producers(server, this.SubPipeline, "Heads", "Head", "UnityServer-", numberOfConnectedUsers);
            gatherProducers.LeftsHandPositionOrientationsUnity = gatherProducers.CreateTupleVector3Producers(server, this.SubPipeline, "LeftHands", "LeftWrist", "UnityServer-", numberOfConnectedUsers);
            gatherProducers.RightsHandPositionOrientationsUnity = gatherProducers.CreateTupleVector3Producers(server, this.SubPipeline, "RightHands", "RightWrist", "UnityServer-", numberOfConnectedUsers);
            gatherProducers.TaskLogs = gatherProducers.CreatePieceInteractionProducers(server, this.SubPipeline, "Task", "Interactions", numberOfConnectedUsers);
            gatherProducers.LeftGazeEventsStrings = gatherProducers.CreateStringProducers(server, this.SubPipeline, "Gaze", "GazeEventString", numberOfConnectedUsers);

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
        public void WriteCSV()
        {
            throw new NotImplementedException();
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
