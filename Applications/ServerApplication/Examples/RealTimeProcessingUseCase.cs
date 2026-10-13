// <copyright file="" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System.IO;
using System.Numerics;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Speech;
using SAAC.CollaborationIndices;
using SAAC.CollaborationIndices.Verbal;
using SAAC.PipelineServices;
using SAAC.PsiFormats;

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

    /// <summary>
    /// What the file next to a store of speech computed from the audio says of it: the file
    /// has the name of the store, with the extension json, in the folder of the session.
    /// </summary>
    public class SpeechStoreDescription
    {
        /// <summary>Gets or sets the name of the store.</summary>
        public string Store { get; set; } = string.Empty;

        /// <summary>Gets or sets when the speech was computed.</summary>
        public string ComputedOn { get; set; } = string.Empty;

        /// <summary>Gets or sets the streams of the store.</summary>
        public List<string> Streams { get; set; } = new List<string>();

        /// <summary>Gets or sets the audio stream each participant was given, in the order of the participants.</summary>
        public List<string> AudioOfParticipants { get; set; } = new List<string>();

        /// <summary>Gets or sets how the voice activity was obtained.</summary>
        public string VoiceActivity { get; set; } = string.Empty;

        /// <summary>Gets or sets how the transcription was obtained.</summary>
        public string Transcription { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the store holds the speech of the whole
        /// session. False while it is being written, and for good when the replay that wrote
        /// it was stopped before the end of the dataset: such a store is not read.
        /// </summary>
        public bool Complete { get; set; }
    }

    public class RealTimeProcessingUseCase
    {
        /// <summary>
        /// Name of the store of the speech computed from the audio.
        /// </summary>
        public const string SpeechStoreName = "SpeechFromAudio";

        // The session of what the process produces.
        private const string ProcessSessionName = "RawDataPipelineProcess.000";

        /// <summary>
        /// Configuration module.
        /// </summary>
        public RealTimeProcessingUseCaseConfiguration Configuration = new RealTimeProcessingUseCaseConfiguration();

        /// <summary>
        /// What the collaboration indices compute: participants, windows, indicators, profiles.
        /// Filled from the interface; null computes no index.
        /// </summary>
        public CollaborationSessionConfiguration? CollaborationConfiguration = null;

        /// <summary>
        /// The collaboration indices of the running process; null when none is computed.
        /// </summary>
        public CollaborationIndicesProcess? CollaborationIndices = null;

        /// <summary>
        /// Number of participants whose streams are gathered, numbered from 1 in the stream names.
        /// </summary>
        public int NumberOfParticipants = 2;

        /// <summary>
        /// Local axis of the head streams that points where the participant looks, for the
        /// position files: "+Z" for a Unity object, "+Y" for the head bone of the avatars.
        /// The collaboration indices read it from their own configuration.
        /// </summary>
        public string HeadForwardAxis = "+Z";

        /// <summary>
        /// Where the process reports what it gathers and computes; the log of the server when null.
        /// </summary>
        public Action<string>? Log = null;

        /// <summary>
        /// The audio stream of each participant, by id or by name, in the order of the
        /// participants. Only read when the voice activity or the transcription is computed
        /// from the audio. Empty: the audio streams of the session, whatever their names, one
        /// per participant in the order of their ids (<see cref="AudioStreamAssignment"/>).
        /// </summary>
        public List<string> AudioStreamNames = new List<string>();

        /// <summary>
        /// The Whisper model that transcribes the audio of a session without transcription.
        /// </summary>
        public SpeechFromAudioConfiguration WhisperSettings = new SpeechFromAudioConfiguration();

        /// <summary>
        /// True to store the voice activity and the transcription computed from the audio in
        /// the dataset, under the names a session records them (VAD_1, STT_1...): the next
        /// replay reads them and does not compute them again.
        /// </summary>
        public bool StoreSpeechFromAudio = true;

        /// <summary>
        /// True to store what the process detects and computes in the dataset, in partitions
        /// of the session of the process: during a live session and during a post processing.
        /// </summary>
        public bool StoreProcessStreams = true;

        /// <summary>
        /// Set by the interface before it stops a pipeline. A replay that is stopped before the
        /// end of its dataset leaves a store of computed speech that only holds a part of the
        /// session: it is marked as such and is not read afterwards.
        /// </summary>
        public bool StopRequested = false;

        /// <summary>
        /// Gets a value indicating whether the running process stores streams in the dataset.
        /// </summary>
        public bool StoresStreams => this.processStore != null && this.processStore.StoresStreams;

        // Where the running process of a live session stores its streams, and how many times the process was started.
        private ProcessStore? processStore = null;
        private int processRun = 0;

        // The store of computed speech the running process writes, and the file describing it; null when it writes none.
        private SpeechStoreDescription? speechStore = null;
        private string speechStoreFile = string.Empty;
        private bool speechStoreOfReplay = false;

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
            this.positionrotationAWriter = new StreamWriter($@"{this.csvAdress}{this.sessionNumber}-A_position_orientation.csv");
            this.positionrotationBWriter = new StreamWriter($@"{this.csvAdress}{this.sessionNumber}-B_position_orientation.csv");
            this.positionrotationCWriter = new StreamWriter($@"{this.csvAdress}{this.sessionNumber}-C_position_orientation.csv");
            this.taskInteractionEventWriter = new StreamWriter($@"{this.csvAdress}{this.sessionNumber}-interaction_event.csv");

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
        /// Starts the processing of a session: the position files, and the collaboration
        /// indices when <see cref="CollaborationConfiguration"/> is set.
        /// </summary>
        /// <param name="server">The pipeline owning the connectors: the server of a live session, or the replay of a dataset.</param>
        /// <param name="pipelineSessionName">Name of the session of the process.</param>
        /// <param name="session">Session receiving what the process stores; may be null.</param>
        /// <param name="replay">
        /// False for a live session: the streams of the applications that are connected are
        /// gathered, and the process starts now. True for the replay of a dataset: every
        /// stream of the dataset is gathered, and the process starts with the replay.
        /// </param>
        public void StartPipelineCollaborationProcess(DatasetPipeline server, string pipelineSessionName, Session? session, bool replay = false)
        {
            int numberOfConnectedUsers = this.NumberOfParticipants;
            GatherProducers gatherProducers = new GatherProducers();
            Action<string> log = this.Log ?? (message => server.Log(message));

            this.SubPipeline = new Subpipeline(server.Pipeline, "CollaborationProcess");
            this.Session = session;

            // The process stores what it detects and computes in partitions of its own: those
            // of a live session carry the number of the run from the second one on, those
            // of a post processing the number of the post processing of the dataset.
            if (!this.StoreProcessStreams)
            {
                this.processStore = null;
            }
            else if (replay)
            {
                int number = NextPostProcessing(server);
                this.Session = server.CreateOrGetSession(ProcessSessionName);
                this.processStore = new ProcessStore(server, this.Session, this.SubPipeline, $"_post{number}", $"post processing {number}");
            }
            else
            {
                this.processRun++;
                this.processStore = new ProcessStore(server, session, this.SubPipeline, this.processRun <= 1 ? string.Empty : $"_run{this.processRun}", $"run {this.processRun}");
            }
            this.WritersDisposed = false;
            this.StopRequested = false;
            this.speechStore = null;
            log($"Collaboration process: starting for {numberOfConnectedUsers} participants, on {(replay ? "the streams of the replayed dataset" : "the streams of the connected applications")}.");

            // The streams are looked up by name: a missing one leaves its list empty, and
            // what needs it is not computed.
            if (!this.IsMicrophoneRecording && !replay)
            {
                log("Collaboration process: the WhisperStreaming application is not connected, no audio, voice activity or transcription is gathered.");
            }

            if (!this.IsServerInitialised && !replay)
            {
                log("Collaboration process: the UnityServer application is not connected, no position, gaze event or piece status is gathered.");
            }

            if (this.IsMicrophoneRecording || replay)
            {
                this.GatherSpeech(server, this.SubPipeline, gatherProducers, numberOfConnectedUsers, replay, log);
            }

            if (this.IsServerInitialised || replay)
            {
                // "UnityServer-1-Head" when the stores come from the dictionary, "1-Head" when each stream has its store.
                gatherProducers.HeadPositionOrientationsUnity = gatherProducers.FindProducers<Tuple<Vector3, Vector3>>(server, this.SubPipeline, numberOfConnectedUsers, i => UnityServerNames(i, "Head"), log);
                gatherProducers.LeftsHandPositionOrientationsUnity = gatherProducers.FindProducers<Tuple<Vector3, Vector3>>(server, this.SubPipeline, numberOfConnectedUsers, i => UnityServerNames(i, "LeftWrist"), log);
                gatherProducers.RightsHandPositionOrientationsUnity = gatherProducers.FindProducers<Tuple<Vector3, Vector3>>(server, this.SubPipeline, numberOfConnectedUsers, i => UnityServerNames(i, "RightWrist"), log);

                // The last name of each list is the one of the headsets of the puzzle task datasets.
                gatherProducers.TaskLogs = gatherProducers.FindProducers<PieceStatus>(server, this.SubPipeline, numberOfConnectedUsers, i => UnityServerNames(i, "Interactions", $"Quest{i}-PieceState"), log);
                gatherProducers.LeftGazeEvents = gatherProducers.FindProducers<ObjectGazeEvent>(server, this.SubPipeline, numberOfConnectedUsers, i => UnityServerNames(i, "GazeEvent", $"Quest{i}-LeftEyeGaze"), log);

                // A headset publishes on either eye; the looks at the avatars are recomputed by the replay of the scene.
                gatherProducers.RightGazeEvents = gatherProducers.FindProducers<ObjectGazeEvent>(server, this.SubPipeline, numberOfConnectedUsers, i => new[] { $"Quest{i}-RightEyeGaze" }, log, reportMissing: false);
                gatherProducers.AvatarGazeEvents = gatherProducers.FindProducers<ObjectGazeEvent>(server, this.SubPipeline, numberOfConnectedUsers, i => new[] { $"UnityB-H{i}-GazeEvent" }, log, reportMissing: false);
            }

            // The videos are only recorded again from a live session.
            if (!replay)
            {
                this.StoreVideos(server, gatherProducers, log);
            }

            // LofComputerConfig drives the grid and room geometry.
            // Adjust RoomCenter / RoomRadius to match the physical room:
            //   "big"   room → center (-22.5, 0), radius 26.5 m  (configurations.py)
            //   "small" room → center (-13.5, 0), radius  2.5 m
            /*var lofConfig = new LofComputerConfig
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
            );*/

            // The position files need the head and both hands of every participant. They are
            // written during the session: a replay does not write them again, nor declare
            // again the streams a recorded session already has.
            bool writePositions = !replay
                && gatherProducers.HeadPositionOrientationsUnity.Count == numberOfConnectedUsers
                && gatherProducers.LeftsHandPositionOrientationsUnity.Count == numberOfConnectedUsers
                && gatherProducers.RightsHandPositionOrientationsUnity.Count == numberOfConnectedUsers;

            if (writePositions)
            {
                log($"Collaboration process: position files of the {numberOfConnectedUsers} participants written in {this.csvAdress}, look direction of the heads along their {this.HeadForwardAxis} axis.");
                log(this.Session == null
                    ? "Collaboration process: the standardised positions are not stored, the process has no session to store them in."
                    : $"Collaboration process: the standardised positions (1_Head, 1_Left, 1_Right...) are stored in the partition {this.processStore?.Partition("Positions") ?? "LiveVisualization"} of the session {this.Session.Name}.");
            }
            else
            {
                log(replay
                    ? "Collaboration process: the position files are not written again from a replay."
                    : "Collaboration process: the position files are not written, the head and both hands of every participant are needed.");
            }

            for (int i = 0; writePositions && i < numberOfConnectedUsers; i++)
            {
                // gatherProducers.HeadPositionOrientationsUnity[i].PipeTo(lof.GetReceiver(i));
                PositionOrientationPreProcessing posRot = new PositionOrientationPreProcessing(
                    this.SubPipeline,
                    server,
                    new PositionOrientationConfiguration
                    {
                        userID = i,
                        sessionNum = this.sessionNumber,
                        csvAdress = this.csvAdress,
                        headForwardAxis = this.HeadForwardAxis,

                        // The standardised positions are stored in the session of the process.
                        sessionName = this.Session?.Name ?? string.Empty,
                        storeName = this.processStore?.Declare("Positions") ?? string.Empty,
                    });
                this.StreamsWriters.Add(posRot.positionrotationWriter);
                gatherProducers.HeadPositionOrientationsUnity[i].PipeTo(posRot.HeadPositionOrientationIn);
                gatherProducers.LeftsHandPositionOrientationsUnity[i].PipeTo(posRot.LeftHandPositionOrientationIn);
                gatherProducers.RightsHandPositionOrientationsUnity[i].PipeTo(posRot.RightHandPositionOrientationIn);
            }

            // The collaboration indices: what the interface selected, on the streams gathered.
            if (this.CollaborationConfiguration != null)
            {
                // The detections and the results are stored in the partitions of the process, live and in a replay.
                var indices = new CollaborationIndicesProcess(
                    this.SubPipeline, gatherProducers, this.CollaborationConfiguration, log, this.Session == null ? null : this.processStore);
                try
                {
                    this.CollaborationIndices = indices.Build() ? indices : null;
                }
                catch (CollaborationIndicesConfigurationException exception)
                {
                    // Every problem of the configuration, one per line.
                    log($"Collaboration indices are not computed: {exception.Message}");
                    indices.Close();
                    this.CollaborationIndices = null;
                }
            }

            if (this.processStore != null && this.processStore.StoresStreams)
            {
                log($"Collaboration process: {this.processStore.Label}, stored in the session {this.Session!.Name} of the dataset, one partition per kind of data: {this.processStore.Describe()}.");
            }

            // A live process starts now, beside the applications that are already running;
            // the process of a replay starts with the replay, so that it reads the dataset
            // from its first message.
            if (!replay)
            {
                this.SubPipeline.RunAsync();
                log("Collaboration process: started.");
            }
            else
            {
                log("Collaboration process: ready, it starts with the replay.");
            }

            // gatherProducers.LeftGazeEvents = gatherProducers.CreateGazeProducers(server, this.SubPipeline, "Gaze", "GazeEvent", numberOfConnectedUsers);

            // Console.WriteLine("Starting pipeline collaboration process...");
            // 1. Create a pipeline collaboration module (PCM) and add it to the server
            // 2. The PCM will have receivers and emitters that will be connected to other modules in the pipeline
            // 3. The PCM will process data in real-time as it is received from the emitters and send processed data to the receivers
            // 4. The PCM can also send status updates or logs to a monitoring module or UI
        }

        // The kind of a speech stream is its name without the number of the participant: "VAD_" for "VAD_2".
        private static string StreamKind(string streamName)
        {
            return streamName.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
        }

        // The messages the streams of a kind recorded in a session of the replayed dataset; 0 when it is not known.
        private static long RecordedInSession(DatasetPipeline server, string sessionName, string kind)
        {
            try
            {
                return server.Dataset?.Sessions
                    .Where(session => session.Name == sessionName)
                    .SelectMany(session => session.Partitions)
                    .SelectMany(partition => partition.AvailableStreams)
                    .Where(stream => StreamKind(stream.Name) == kind)
                    .Sum(stream => stream.MessageCount) ?? 0;
            }
            catch (Exception)
            {
                // The catalogue of a store that is being written may not be readable.
                return 0;
            }
        }

        // The number of the next post processing of a dataset: one more than the highest number its partitions carry.
        private static int NextPostProcessing(DatasetPipeline server)
        {
            IEnumerable<string> names = server.Connectors.Keys;
            if (server.Dataset != null)
            {
                names = names.Concat(server.Dataset.Sessions.SelectMany(session => session.Partitions).Select(partition => partition.Name));
            }

            int last = 0;
            foreach (string name in names.ToList())
            {
                var match = System.Text.RegularExpressions.Regex.Match(name ?? string.Empty, @"_post(\d+)$");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                {
                    last = Math.Max(last, number);
                }
            }

            return last + 1;
        }

        /// <summary>
        /// Stops the process and leaves the server running: the subpipeline of the process
        /// is disposed, which closes the stores it wrote, its result files are closed, and
        /// what it declared on the server is forgotten so that it can be started again.
        /// </summary>
        /// <param name="server">The server the process runs on.</param>
        public void StopProcess(DatasetPipeline server)
        {
            this.SubPipeline?.Dispose();
            this.SubPipeline = null;
            this.CloseWriters();

            this.processStore?.Forget();
            this.processStore = null;
        }

        /// <summary>
        /// Closes the result files of the process. To call once its pipeline has stopped:
        /// what was not written yet is lost otherwise.
        /// </summary>
        public void CloseWriters()
        {
            // The end of a replay and the interface may both ask for it.
            lock (this.StreamsWriters)
            {
                if (this.WritersDisposed)
                {
                    return;
                }

                this.CloseSpeechStore();
                this.CollaborationIndices?.Close();
                this.CollaborationIndices = null;

                foreach (TextWriter writer in this.StreamsWriters)
                {
                    this.CloseAndDisposeWriter(writer);
                }

                if (this.StreamsWriters.Count > 0)
                {
                    this.Log?.Invoke($"Collaboration process: {this.StreamsWriters.Count} position files closed in {this.csvAdress}");
                }

                this.StreamsWriters.Clear();
                this.WritersDisposed = true;
                this.Log?.Invoke("Collaboration process: stopped, its result files are closed.");
            }
        }

        // What the runs of the process stored in their partition Video: copies, which are not
        // videos of an application.
        private readonly HashSet<string> videoCopies = new HashSet<string>();

        /// <summary>
        /// Stores, in the partition Video of the process, every video the connected applications
        /// send: each stream of JPEG frames, whatever the name of the application and of its
        /// capture area. The stream FullScreen of an application named VideoRemoteApp keeps the
        /// name it always had there, "Unity Server"; the others are named after their
        /// application and their capture area ("VideoCapture1-TopLeft").
        /// </summary>
        private void StoreVideos(DatasetPipeline server, GatherProducers gatherProducers, Action<string> log)
        {
            ProcessStore store = this.processStore ?? new ProcessStore(server, this.Session, this.SubPipeline, string.Empty, string.Empty);
            var stored = new List<string>();
            foreach (var video in FindVideos(server, this.videoCopies))
            {
                string name = video.Name == "VideoRemoteApp-FullScreen" ? "Unity Server" : video.Name;
                IProducer<Shared<EncodedImage>> frames = video.Connector.CreateBridge<Shared<EncodedImage>>(this.SubPipeline);
                gatherProducers.ServerVideo ??= frames;
                if (store.Write("Video", name, frames))
                {
                    this.videoCopies.Add($"{store.Partition("Video")}/{name}");
                    stored.Add(name == video.Name ? name : $"{name} ({video.Name})");
                }
            }

            if (stored.Count > 0)
            {
                log(this.Session == null
                    ? $"Collaboration process: {stored.Count} video stream(s) gathered but not stored, there is no session to store them in: {string.Join(", ", stored)}."
                    : $"Collaboration process: {stored.Count} video stream(s) of the applications stored in the store {store.Partition("Video")} of the session {this.Session.Name}: {string.Join(", ", stored)}.");
            }
            else if (this.IsVideoInitialised)
            {
                log("Collaboration process: the VideoRemoteApp application is connected but none of its streams is a video (JPEG frames), no video is stored.");
            }
        }

        // The videos of the applications: the streams of JPEG frames the server receives, each
        // with the name of its application and of its capture area. Where these two names are
        // depends on the store mode of the session: the store "Application-Area" and the stream
        // "Area" when each stream has its store, the store "Application" and the stream "Area"
        // when the stores are those of the applications, the stream "Application-Area" in the
        // store the dictionary names.
        private static List<(string Name, ConnectorInfo Connector)> FindVideos(DatasetPipeline server, HashSet<string> copies)
        {
            var videos = new List<(string Name, ConnectorInfo Connector)>();
            foreach (var store in server.Connectors.ToList().OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                foreach (var stream in store.Value.ToList().OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    if (stream.Value.DataType != typeof(Shared<EncodedImage>) || copies.Contains($"{store.Key}/{stream.Key}"))
                    {
                        continue;
                    }

                    string name = store.Key.EndsWith("-" + stream.Key, StringComparison.Ordinal) ? store.Key
                        : stream.Key.Contains("-") ? stream.Key
                        : $"{store.Key}-{stream.Key}";

                    // Two streams never get the same name.
                    while (videos.Any(video => video.Name == name))
                    {
                        name = $"{store.Key}-{name}";
                    }

                    videos.Add((name, stream.Value));
                }
            }

            return videos;
        }

        // The indicators computed from who speaks when.
        private static readonly string[] SpeechIndicators =
        {
            IndexNames.TurnTaking, IndexNames.VerbalParticipation, IndexNames.SpeechEquality, IndexNames.TalkingMost, IndexNames.Silence, IndexNames.CrossTalk,
        };

        /// <summary>
        /// The voice activity and the transcription of every participant: those the session
        /// recorded or, when it did not record them and a speech indicator is selected,
        /// computed from its audio by the voice activity detector and by Whisper.
        /// </summary>
        private void GatherSpeech(DatasetPipeline server, Pipeline pipeline, GatherProducers gatherProducers, int participants, bool replay, Action<string> log)
        {
            // In a replay the dataset says how many messages each stream recorded. The voice
            // activity or the transcription streams of a session are passed over when none
            // of them recorded a message: it is what an interrupted recording, or a recogniser
            // that produced nothing, leaves behind. One empty stream among others is kept: a
            // participant who said nothing has no transcription. The counts are only trusted
            // when the audio has some: stores that were never closed report none for every stream.
            bool countsKnown = replay && gatherProducers.FindStreamNames<AudioBuffer>(server).Any(name => gatherProducers.RecordedMessageCount(server, name) > 0);
            var emptyStreams = new SortedSet<string>();

            // A store of speech computed by a replay that was stopped before its end is not read either.
            var storesLeftOut = new HashSet<string>();
            Func<ConnectorInfo, bool> complete = connector =>
            {
                if (countsKnown && RecordedInSession(server, connector.SessionName, StreamKind(connector.SourceName)) == 0)
                {
                    emptyStreams.Add($"{connector.SourceName} ({connector.SessionName}/{connector.StoreName})");
                    return false;
                }

                return this.IsCompleteSpeech(server, connector, storesLeftOut, log);
            };

            gatherProducers.Vads = gatherProducers.FindProducers<bool>(server, pipeline, participants, i => new[] { $"VAD_{i}" }, log, reportMissing: false, accept: complete);
            gatherProducers.Stts = gatherProducers.FindProducers<IStreamingSpeechRecognitionResult>(server, pipeline, participants, i => new[] { $"STT_{i}" }, log, reportMissing: false, accept: complete);
            bool hasVoiceActivity = gatherProducers.Vads.Count == participants;
            bool hasTranscription = gatherProducers.Stts.Count == participants;
            if (emptyStreams.Count > 0)
            {
                log($"Speech: recorded streams passed over, none of their kind has a message in their session: {string.Join(", ", emptyStreams)}.");
            }

            log($"Speech: voice activity streams (VAD_1 to VAD_{participants}) {(hasVoiceActivity ? "found" : "not found")}; transcription streams (STT_1 to STT_{participants}) {(hasTranscription ? "found" : "not found")}.");

            // Those found may come from an earlier run that computed them from the audio.
            var described = new HashSet<string>();
            foreach (ConnectorInfo? connector in new[]
            {
                hasVoiceActivity ? gatherProducers.FindConnector<bool>(server, "VAD_1", complete) : null,
                hasTranscription ? gatherProducers.FindConnector<IStreamingSpeechRecognitionResult>(server, "STT_1", complete) : null,
            })
            {
                SpeechStoreDescription? description = connector == null ? null : ReadSpeechStore(server, connector);
                if (connector != null && description != null && described.Add(connector.StoreName))
                {
                    log($"Speech: {string.Join(", ", description.Streams)} were computed from the audio on {description.ComputedOn} ({Describe(description.AudioOfParticipants)}; voice activity: {description.VoiceActivity}; transcription: {description.Transcription}) "
                        + $"and stored in {connector.SessionName}/{connector.StoreName}. To compute them again, delete the folder {connector.StoreName}.0000 of {Path.Combine(server.StorePath, connector.SessionName)}.");
                }
            }

            if (hasVoiceActivity && hasTranscription)
            {
                log("Speech: the voice activity and the transcription of the session are used, nothing is computed from the audio.");
                return;
            }

            // Only the speech indicators read them.
            string[] selected = this.CollaborationConfiguration == null
                ? Array.Empty<string>()
                : SpeechIndicators.Where(this.CollaborationConfiguration.Has).ToArray();
            if (selected.Length == 0)
            {
                log("Speech: no speech indicator is selected, what is missing is not computed from the audio.");
                return;
            }

            if (!hasTranscription)
            {
                string[] problems = this.WhisperSettings.Validate().ToArray();
                if (problems.Length > 0)
                {
                    log($"Speech: the transcription cannot be computed from the audio: {string.Join("; ", problems)}. {string.Join(", ", selected)} will not be computed.");
                    return;
                }
            }

            List<IProducer<AudioBuffer>> audios = this.FindAudios(server, pipeline, gatherProducers, participants, countsKnown, log, out AudioStreamAssignment assignment);
            if (audios.Count != participants)
            {
                log(hasTranscription
                    ? "Speech: the voice activity is not computed, the audio of every participant is needed."
                    : $"Speech: the transcription is not computed, the audio of every participant is needed. {string.Join(", ", selected)} will not be computed.");
                return;
            }

            var computed = new List<string>();
            if (!hasVoiceActivity)
            {
                computed.Add("the voice activity, by the system voice activity detector");
            }

            if (!hasTranscription)
            {
                computed.Add($"the transcription, by Whisper (model {this.WhisperSettings.WhisperModel}, {this.WhisperSettings.Language}, models of {this.WhisperSettings.WhisperModelDirectory})");
            }

            log($"Speech: computed from the audio for {string.Join(", ", selected)}: {string.Join("; ", computed)}.");

            var voiceActivities = new List<IProducer<bool>>(gatherProducers.Vads);
            var transcriptions = new List<IProducer<IStreamingSpeechRecognitionResult>>(gatherProducers.Stts);
            try
            {
                for (int i = 0; i < participants; i++)
                {
                    // The detector and Whisper read 16 kHz, one channel, 16 bit PCM.
                    var resampler = new AudioResampler(pipeline, new AudioResamplerConfiguration { OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm() });
                    audios[i].PipeTo(resampler);

                    if (!hasVoiceActivity)
                    {
                        voiceActivities.Add(SpeechFromAudio.VoiceActivity(pipeline, server, resampler, i));
                    }

                    if (!hasTranscription)
                    {
                        int participant = i + 1;
                        transcriptions.Add(SpeechFromAudio.Transcription(
                            pipeline, server, resampler, voiceActivities[i], i, this.WhisperSettings, message => log($"Speech: Whisper of participant {participant}: {message}")));
                    }
                }
            }
            catch (Exception exception)
            {
                // The speech recogniser of Windows or the libraries of Whisper may be missing on the machine.
                log($"Speech: the speech could not be computed from the audio ({exception.GetType().Name}: {exception.Message}). {string.Join(", ", selected)} will not be computed.");
                return;
            }

            gatherProducers.Audios = audios;
            gatherProducers.Vads = voiceActivities;
            gatherProducers.Stts = transcriptions;

            if (this.StoreSpeechFromAudio)
            {
                this.StoreSpeech(server, pipeline, hasVoiceActivity ? null : voiceActivities, hasTranscription ? null : transcriptions, assignment, replay, log);
            }
            else
            {
                log("Speech: what is computed from the audio is not stored.");
            }
        }

        /// <summary>
        /// Stores the voice activity and the transcription computed from the audio in the
        /// dataset, under the names a session records them, with a file that says how they
        /// were obtained. The next replay finds them and computes nothing from the audio.
        /// What the session recorded itself is given as null and is not stored again.
        /// </summary>
        private void StoreSpeech(
            DatasetPipeline server, Pipeline pipeline, List<IProducer<bool>>? voiceActivities, List<IProducer<IStreamingSpeechRecognitionResult>>? transcriptions, AudioStreamAssignment assignment, bool replay, Action<string> log)
        {
            // The session of what the process produces: given for a live session, looked up in a replayed dataset.
            Session? session = this.Session ?? server.CreateOrGetSession(ProcessSessionName);
            if (session == null)
            {
                log("Speech: what is computed from the audio is not stored, the pipeline has no dataset.");
                return;
            }

            // A live process has partitions of its own. In a replay, a store that is being
            // read cannot be written, and a name the session already lists cannot be listed
            // again, even when its store is no longer on the disk: the next free name is taken.
            // The speech computed by a post processing is not tied to it: the next ones read it.
            bool ownPartition = this.processStore != null && !replay;
            string storeName = ownPartition ? this.processStore!.Declare(SpeechStoreName) : SpeechStoreName;
            Func<string, bool> taken = name => server.Connectors.ContainsKey(name) || session.Partitions.Any(partition => partition.Name == name);
            for (int version = 2; !ownPartition && taken(storeName); version++)
            {
                storeName = $"{SpeechStoreName}_{version}";
            }

            var stored = new List<string>();
            try
            {
                for (int i = 0; voiceActivities != null && i < voiceActivities.Count; i++)
                {
                    server.CreateConnectorAndStore($"VAD_{i + 1}", storeName, session, pipeline, typeof(bool), voiceActivities[i]);
                    stored.Add($"VAD_{i + 1}");
                }

                for (int i = 0; transcriptions != null && i < transcriptions.Count; i++)
                {
                    server.CreateConnectorAndStore($"STT_{i + 1}", storeName, session, pipeline, typeof(IStreamingSpeechRecognitionResult), transcriptions[i]);
                    stored.Add($"STT_{i + 1}");
                }
            }
            catch (Exception exception)
            {
                log($"Speech: what is computed from the audio could not be stored ({exception.GetType().Name}: {exception.Message}); the indicators still read it.");
                return;
            }

            string folder = Path.Combine(server.StorePath, session.Name);
            this.speechStore = new SpeechStoreDescription
            {
                Store = storeName,
                ComputedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Streams = stored,
                AudioOfParticipants = assignment.Assigned.ToList(),
                VoiceActivity = voiceActivities == null ? "recorded with the session" : "system voice activity detector",
                Transcription = transcriptions == null ? "recorded with the session" : $"Whisper {this.WhisperSettings.WhisperModel}, {this.WhisperSettings.Language}",
                Complete = false,
            };
            this.speechStoreFile = Path.Combine(folder, storeName + ".json");
            this.speechStoreOfReplay = replay;
            this.WriteSpeechStoreDescription(log);

            log($"Speech: stored as {string.Join(", ", stored)} in the store {storeName} of the session {session.Name}, in {folder}. "
                + (replay
                    ? "Once the replay has reached the end of the dataset, the next replays read them and compute nothing from the audio."
                    : "A replay of the session reads them and computes nothing from the audio."));
            if (storeName != SpeechStoreName && !ownPartition)
            {
                log($"Speech: the dataset already lists a store named {SpeechStoreName}, the new one is named {storeName}.");
            }
        }

        /// <summary>
        /// Says in its file whether the store of computed speech holds the whole session: it
        /// does not when a replay is stopped before the end of the dataset.
        /// </summary>
        private void CloseSpeechStore()
        {
            SpeechStoreDescription? store = this.speechStore;
            if (store == null)
            {
                return;
            }

            Action<string> log = this.Log ?? (_ => { });
            store.Complete = !(this.speechStoreOfReplay && this.StopRequested);
            this.WriteSpeechStoreDescription(log);
            this.speechStore = null;

            string folder = Path.GetDirectoryName(this.speechStoreFile) ?? string.Empty;
            log(store.Complete
                ? $"Speech: the store {store.Store} of {folder} holds the speech of the whole session ({string.Join(", ", store.Streams)})."
                : $"Speech: the replay was stopped before the end of the dataset, the store {store.Store} of {folder} only holds a part of the speech. It is marked incomplete and the next replays do not read it; its folder {store.Store}.0000 can be deleted.");
        }

        private void WriteSpeechStoreDescription(Action<string> log)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(this.speechStoreFile) ?? string.Empty);
                File.WriteAllText(this.speechStoreFile, Newtonsoft.Json.JsonConvert.SerializeObject(this.speechStore, Newtonsoft.Json.Formatting.Indented));
            }
            catch (Exception exception)
            {
                log($"Speech: the description of the stored speech could not be written in {this.speechStoreFile} ({exception.Message}).");
            }
        }

        // The description of the store of a stream, when it is a store of speech computed from the audio; null otherwise.
        private static SpeechStoreDescription? ReadSpeechStore(DatasetPipeline server, ConnectorInfo connector)
        {
            try
            {
                string file = Path.Combine(server.StorePath, connector.SessionName, connector.StoreName + ".json");
                SpeechStoreDescription? description = File.Exists(file) ? Newtonsoft.Json.JsonConvert.DeserializeObject<SpeechStoreDescription>(File.ReadAllText(file)) : null;

                // The file names its store: another json file of the same name is not a description.
                return description != null && description.Store == connector.StoreName ? description : null;
            }
            catch (Exception)
            {
                // Not a description: the store is one the session recorded.
                return null;
            }
        }

        // False for the streams of a store of computed speech that does not hold the whole session; said once per store.
        private bool IsCompleteSpeech(DatasetPipeline server, ConnectorInfo connector, HashSet<string> storesLeftOut, Action<string> log)
        {
            SpeechStoreDescription? description = ReadSpeechStore(server, connector);
            if (description == null || description.Complete)
            {
                return true;
            }

            if (storesLeftOut.Add(connector.StoreName))
            {
                log($"Speech: the store {connector.StoreName} of the session {connector.SessionName} is not read: it was written on {description.ComputedOn} by a run that did not reach the end of the session. Its folder {connector.StoreName}.0000 in {Path.Combine(server.StorePath, connector.SessionName)} can be deleted.");
            }

            return false;
        }

        private static string Describe(IEnumerable<string> audioOfParticipants)
            => string.Join(", ", audioOfParticipants.Select((audio, index) => $"participant {index + 1} = {audio}"));

        /// <summary>
        /// The audio of every participant, whatever the streams are named: the audio streams
        /// of the session are numbered in the order of their names, and each participant gets
        /// one, in that order or as <see cref="AudioStreamNames"/> says.
        /// </summary>
        /// <returns>One stream per participant, or none.</returns>
        private List<IProducer<AudioBuffer>> FindAudios(DatasetPipeline server, Pipeline pipeline, GatherProducers gatherProducers, int participants, bool countsKnown, Action<string> log, out AudioStreamAssignment assignment)
        {
            // The audio streams of an interrupted recording hold nothing: they are not candidates.
            List<string> available = gatherProducers.FindStreamNames<AudioBuffer>(server);
            List<string> empty = countsKnown ? available.Where(name => gatherProducers.RecordedMessageCount(server, name) == 0).ToList() : new List<string>();
            if (empty.Count > 0)
            {
                log($"Speech: audio streams that recorded no buffer are left out: {string.Join(", ", empty)}.");
                available = available.Except(empty).ToList();
            }

            assignment = AudioStreamAssignment.Resolve(available, this.AudioStreamNames, participants);
            if (assignment.Available.Count > 0)
            {
                log($"Speech: audio streams found, numbered in the order of their names: {string.Join(", ", assignment.Available.Select((name, index) => $"{index + 1} = {name}{Recorded(gatherProducers.RecordedMessageCount(server, name))}"))}.");
            }

            if (!assignment.IsResolved)
            {
                log($"Speech: {assignment.Problem}{(assignment.Available.Count > 0 ? "; set it in \"Audio streams\"" : string.Empty)}.");
                return new List<IProducer<AudioBuffer>>();
            }

            // Nothing in a session says which audio stream is the one of which participant.
            log(assignment.IsExplicit
                ? $"Speech: audio of the participants, as set in \"Audio streams\": {assignment.Describe()}."
                : $"Speech: audio of the participants, one stream each in the order of the ids: {assignment.Describe()}. For another order, set \"Audio streams\" (ids or names, in the order of the participants).");
            IReadOnlyList<string> streams = assignment.Assigned;
            return gatherProducers.FindProducers<AudioBuffer>(server, pipeline, participants, i => new[] { streams[i - 1] }, log);
        }

        private static string Recorded(long buffers) => buffers > 0 ? $" ({buffers} buffers)" : string.Empty;

        // The names the Unity server gives to the stream of a participant.
        private static string[] UnityServerNames(int participant, string category, params string[] otherNames)
            => new[] { $"UnityServer-{participant}-{category}", $"{participant}-{category}" }.Concat(otherNames).ToArray();

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
