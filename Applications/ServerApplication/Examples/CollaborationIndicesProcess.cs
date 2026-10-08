// <copyright file="CollaborationIndicesProcess.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System.IO;
using System.Numerics;
using Microsoft.Psi;
using SAAC.CollaborationIndices;

namespace ServerApplication.Examples
{
    /// <summary>
    /// The collaboration indices of a session of the server, live or replayed.
    ///
    ///   gathered streams ──► detectors ──► indicators (builder) ──► profiles
    ///
    /// What is computed comes from the configuration the interface fills: participants,
    /// windows, indicators, scores, profiles. The streams are those the use case gathered
    /// (<see cref="GatherProducers"/>). An indicator whose streams were not gathered is left
    /// out and reported, so that no index is published from nothing.
    /// </summary>
    public class CollaborationIndicesProcess
    {
        private readonly Pipeline pipeline;
        private readonly GatherProducers streams;
        private readonly CollaborationSessionConfiguration configuration;
        private readonly Action<string> log;
        private readonly List<uint> participants;
        private readonly Dictionary<TimeSpan, TextWriter?> indexWriters = new Dictionary<TimeSpan, TextWriter?>();
        private readonly List<TextWriter> profileWriters = new List<TextWriter>();

        // What was received and produced, for the log.
        private readonly List<Tally> tallies = new List<Tally>();
        private readonly Dictionary<TimeSpan, CollaborationProfilesComponent> profiles = new Dictionary<TimeSpan, CollaborationProfilesComponent>();

        // What detects; created only when its streams are there.
        private VerbalizationDetectorComponent? speech;
        private TurnTakingDetector? turnTakings;
        private FFormationDetector? formations;
        private GazeEpisodeDetector? gazes;
        private JointVisualAttentionDetector? jointAttention;
        private MutualGazeDetector? mutualGazes;
        private TaskActionDetector? taskActions;

        // What is computed from the detections, one instance per window.
        private SlidingAverageComputationSet? indices;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollaborationIndicesProcess"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline, usually the subpipeline of the process.</param>
        /// <param name="streams">The streams gathered from the connectors of the server.</param>
        /// <param name="configuration">What to compute.</param>
        /// <param name="log">Where to report what is and is not computed.</param>
        public CollaborationIndicesProcess(Pipeline pipeline, GatherProducers streams, CollaborationSessionConfiguration configuration, Action<string> log)
        {
            this.pipeline = pipeline;
            this.streams = streams;
            this.configuration = configuration;
            this.log = log ?? (_ => { });
            this.participants = configuration.ParticipantIds.ToList();
        }

        private bool HasSpeech => this.Complete(this.streams.Stts);

        private bool HasHeads => this.Complete(this.streams.HeadPositionOrientationsUnity);

        private bool HasHands => this.Complete(this.streams.LeftsHandPositionOrientationsUnity) && this.Complete(this.streams.RightsHandPositionOrientationsUnity);

        // A headset publishes its gaze events on either eye.
        private bool HasGaze => this.Complete(this.streams.LeftGazeEvents) || this.Complete(this.streams.RightGazeEvents);

        // The looks at the peers: recomputed on the avatars when that was gathered, in the gaze events otherwise.
        private bool HasPeerGaze => this.HasGaze || this.Complete(this.streams.AvatarGazeEvents);

        private bool HasTaskLogs => this.Complete(this.streams.TaskLogs);

        /// <summary>
        /// Creates the detectors, the indices and the profiles, and connects them.
        /// </summary>
        /// <returns>False when nothing can be computed with the streams gathered.</returns>
        public bool Build()
        {
            // 1. Only what the gathered streams allow.
            this.LeaveOutIndicatorsWithoutStreams();
            if (this.configuration.Indicators.Count == 0)
            {
                this.log("Collaboration indices: no selected indicator has its streams, nothing is computed.");
                return false;
            }

            Directory.CreateDirectory(this.configuration.OutputFolder);
            string configurationPath = Path.Combine(this.configuration.OutputFolder, "configuration.json");
            this.configuration.Save(configurationPath);
            this.log($"Collaboration indices: what is computed is saved in {configurationPath}");

            // 2. What detects, and what is computed from the detections.
            this.CreateDetectors();
            this.DeclareCollaborationIndices();

            // 3. The gathered streams, each one connected to what processes it.
            this.ConnectSpeech();
            this.ConnectBodyTracking();
            this.ConnectGaze();
            this.ConnectTaskActions();

            // 4. The detections, connected to the inputs of the indicators that are declared.
            this.ConnectCollaborationIndices();

            // 5. The indices of each window, connected to the profiles computed from them.
            if (this.configuration.ComputeProfiles)
            {
                this.ConnectProfiles();
            }

            // 6. What arrives and what is produced is counted, for the log.
            this.CountStreams();

            this.log($"Collaboration indices: computing {string.Join(", ", this.configuration.Indicators.Select(i => i.Type))}.");
            this.log($"Collaboration indices: {this.participants.Count} participants; windows of {string.Join(", ", this.configuration.Windows.Select(w => $"{w.TotalSeconds:0} s"))}; one row every {this.configuration.ComputationInterval.TotalSeconds:0.###} s; "
                + $"collaboration score {(this.configuration.ComputeCollaborationScores ? "computed" : "not computed")}; profiles {(this.configuration.ComputeProfiles ? "computed" : "not computed")}.");
            this.log($"Collaboration indices: result files {string.Join(", ", this.indexWriters.Keys.Select(window => Path.GetFileName(this.PathOf("indices", window))))}"
                + $"{(this.configuration.ComputeProfiles ? " and profile_*.csv" : string.Empty)} in {this.configuration.OutputFolder}");
            return true;
        }

        /// <summary>
        /// Flushes and closes the result files, and reports what was received and produced.
        /// To call once the pipeline has stopped.
        /// </summary>
        public void Close()
        {
            int files = this.indexWriters.Count + this.profileWriters.Count;
            foreach (TextWriter? writer in this.indexWriters.Values.Concat(this.profileWriters))
            {
                writer?.Dispose();
            }

            this.indexWriters.Clear();
            this.profileWriters.Clear();

            if (files > 0)
            {
                this.ReportTallies();
                this.log($"Collaboration indices: {files} result files closed in {this.configuration.OutputFolder}");
            }
        }

        private bool Complete<T>(List<T> gathered) => gathered != null && gathered.Count >= this.participants.Count;

        // ------------------------------------------------------------------
        // 1. What the streams allow
        // ------------------------------------------------------------------

        private void LeaveOutIndicatorsWithoutStreams()
        {
            var needs = new Dictionary<string, (bool Available, string Streams)>
            {
                { IndexNames.Movement, (this.HasHeads, "head positions") },
                { IndexNames.Synchrony, (this.HasHeads, "head positions") },
                { IndexNames.Proximity, (this.HasHeads, "head positions") },
                { IndexNames.Formation, (this.HasHeads, "head positions") },
                { IndexNames.VerbalParticipation, (this.HasSpeech, "transcriptions") },
                { IndexNames.SpeechEquality, (this.HasSpeech, "transcriptions") },
                { IndexNames.TalkingMost, (this.HasSpeech, "transcriptions") },
                { IndexNames.TurnTaking, (this.HasSpeech, "transcriptions") },
                { IndexNames.Silence, (this.HasSpeech, "transcriptions") },
                { IndexNames.CrossTalk, (this.HasSpeech, "transcriptions") },
                { IndexNames.JointVisualAttention, (this.HasGaze, "gaze events") },
                { IndexNames.GazeOnPeers, (this.HasPeerGaze, "gaze events") },
                { IndexNames.MutualGaze, (this.HasPeerGaze, "gaze events") },
                { IndexNames.TaskParticipation, (this.HasTaskLogs, "piece statuses") },
                { IndexNames.TaskEquality, (this.HasTaskLogs, "piece statuses") },
                { IndexNames.TaskingMost, (this.HasTaskLogs, "piece statuses") },
                { IndexNames.TimeInArea, (false, "area events") },
            };

            foreach (IndicatorConfiguration indicator in this.configuration.Indicators.ToList())
            {
                if (needs.TryGetValue(indicator.Type, out var need) && !need.Available)
                {
                    this.configuration.Indicators.Remove(indicator);
                    this.log($"Collaboration indices: {indicator.Type} is not computed, the {need.Streams} of the {this.participants.Count} participants were not gathered.");
                }
            }

            // Without the hands, the movement is the movement of the head.
            IndicatorConfiguration? movement = this.configuration.Indicators.FirstOrDefault(i => i.Type == IndexNames.Movement);
            if (movement != null && !this.HasHands)
            {
                movement.Options ??= new Dictionary<string, object>();
                movement.Options["BodyParts"] = new List<string> { BodyPartNames.Head };
                this.log("Collaboration indices: Movement is computed on the head only, the hand positions were not gathered.");
            }
        }

        // ------------------------------------------------------------------
        // 2. Detectors and indices
        // ------------------------------------------------------------------

        private void CreateDetectors()
        {
            DetectorSettings settings = this.configuration.Detectors;

            if (this.HasSpeech)
            {
                this.speech = new VerbalizationDetectorComponent(
                    this.pipeline,
                    new VerbalizationDetectorConfiguration { ParticipantIds = this.participants, PublishLegacyType = false, LogToConsole = false });
                this.turnTakings = new TurnTakingDetector(
                    this.pipeline,
                    new TurnTakingDetectorConfiguration { ParticipantIds = this.participants, ConfirmationDelay = settings.TurnTakingConfirmationDelay });
            }

            if (this.HasHeads)
            {
                this.formations = new FFormationDetector(
                    this.pipeline,
                    new FFormationDetectorConfiguration { ParticipantIds = this.participants, TransitionDuration = settings.FormationTransitionDuration, UpAxis = settings.UpAxis() });
            }

            if (this.HasPeerGaze)
            {
                this.gazes = new GazeEpisodeDetector(
                    this.pipeline,
                    new GazeEpisodeDetectorConfiguration { ParticipantIds = this.participants, MinimumDuration = settings.MinimumGazeDuration });
                this.jointAttention = new JointVisualAttentionDetector(
                    this.pipeline,
                    new JointVisualAttentionDetectorConfiguration { ParticipantIds = this.participants, RequireConvergingHeads = settings.RequireConvergingHeads && this.HasHeads });
                this.mutualGazes = new MutualGazeDetector(this.pipeline, new MutualGazeDetectorConfiguration { ParticipantIds = this.participants });
            }

            if (this.HasTaskLogs)
            {
                this.taskActions = new TaskActionDetector(this.pipeline, new TaskActionDetectorConfiguration { ParticipantIds = this.participants, ColorArea = null! });
            }

            var created = new List<string>();
            if (this.speech != null)
            {
                created.Add($"speech intervals and turn takings (confirmed after {settings.TurnTakingConfirmationDelay.TotalSeconds:0.#} s)");
            }

            if (this.formations != null)
            {
                created.Add($"distances and formations (data frame {settings.Frame} with {(settings.Frame == DataFrame.Standard ? "Z" : "Y")} up, head looking along its {settings.HeadForwardAxis} axis)");
            }

            if (this.gazes != null)
            {
                created.Add($"looks at objects and at peers (at least {settings.MinimumGazeDuration.TotalMilliseconds:0} ms)");
                created.Add($"joint visual attention ({(settings.RequireConvergingHeads && this.HasHeads ? "heads must converge" : "no test on the heads")})");
                created.Add("mutual gazes");
            }

            if (this.taskActions != null)
            {
                created.Add("task actions");
            }

            this.log($"Collaboration indices: detectors created for {string.Join("; ", created)}.");
        }

        private void DeclareCollaborationIndices()
        {
            foreach (TimeSpan window in this.configuration.Windows)
            {
                this.indexWriters[window] = new StreamWriter(this.PathOf("indices", window));
            }

            // Participants, period, indicators and scores come from the configuration.
            CollaborationIndicesBuilder builder = this.configuration.CreateBuilder(this.pipeline);

            // The clock: one tick per period of data on a dense stream when there is one, so
            // that a replay gives the indices of the recorded session; the clock of the
            // pipeline otherwise, which only suits a live session.
            if (this.HasHeads)
            {
                builder.WithDataClock(this.streams.HeadPositionOrientationsUnity[0]);
                this.log("Collaboration indices: paced by the time of the head positions of participant 1.");
            }
            else if (this.Complete(this.streams.Vads))
            {
                builder.WithDataClock(this.streams.Vads[0]);
                this.log("Collaboration indices: paced by the time of the voice activity of participant 1, no head position was gathered.");
            }
            else
            {
                builder.WithInternalClock();
                this.log("Collaboration indices: no dense stream to pace the indices, the clock of the pipeline is used (live sessions only).");
            }

            this.indices = builder.BuildSet(this.indexWriters);
        }

        // ------------------------------------------------------------------
        // 3. Gathered streams -> detectors
        // ------------------------------------------------------------------

        private void ConnectSpeech()
        {
            if (this.speech == null || this.turnTakings == null)
            {
                return;
            }

            for (int i = 0; i < this.participants.Count; i++)
            {
                this.streams.Stts[i].PipeTo(this.speech.GetSttInput(this.participants[i]));
            }

            this.speech.Out.PipeTo(this.turnTakings.SpeechIn);

            // A turn taking is confirmed a few seconds after it happened: a clock lets the
            // confirmations expire while nobody speaks.
            if (this.Complete(this.streams.Vads))
            {
                DataClock.FromStream(this.streams.Vads[0], this.configuration.ComputationInterval).PipeTo(this.turnTakings.TickIn);
            }
            else
            {
                this.log("Collaboration indices: no voice activity stream, a turn taking is only confirmed when the next speech arrives.");
            }
        }

        private void ConnectBodyTracking()
        {
            if (this.formations == null)
            {
                return;
            }

            for (int i = 0; i < this.participants.Count; i++)
            {
                IProducer<Tuple<Vector3, Vector3>> headPose = this.streams.HeadPositionOrientationsUnity[i].Select(this.configuration.Detectors.ToHeadPose);
                headPose.PipeTo(this.formations.GetHeadPoseInput(this.participants[i]));
                if (this.jointAttention != null)
                {
                    headPose.PipeTo(this.jointAttention.GetHeadPoseInput(this.participants[i]));
                }
            }
        }

        private void ConnectGaze()
        {
            if (this.gazes == null || this.jointAttention == null || this.mutualGazes == null)
            {
                return;
            }

            // The gaze events of a participant, one stream per eye that was gathered. Each
            // input of the detector reads the events of its kind: objects, or peers.
            var eyes = new List<List<IProducer<SAAC.PsiFormats.ObjectGazeEvent>>>();
            if (this.Complete(this.streams.LeftGazeEvents))
            {
                eyes.Add(this.streams.LeftGazeEvents);
            }

            if (this.Complete(this.streams.RightGazeEvents))
            {
                eyes.Add(this.streams.RightGazeEvents);
            }

            // The looks at the peers come from the avatars when that was gathered: the two
            // sources do not number the peers the same way and are not mixed.
            bool peersFromAvatars = this.Complete(this.streams.AvatarGazeEvents);

            for (int i = 0; i < this.participants.Count; i++)
            {
                uint id = this.participants[i];
                for (int eye = 0; eye < eyes.Count; eye++)
                {
                    eyes[eye][i].PipeTo(this.gazes.GetObjectGazeInput(id, eye));
                    if (!peersFromAvatars)
                    {
                        eyes[eye][i].PipeTo(this.gazes.GetPeerGazeInput(id, eye));
                    }
                }

                if (peersFromAvatars)
                {
                    this.streams.AvatarGazeEvents[i].PipeTo(this.gazes.GetPeerGazeInput(id));
                }
            }

            this.gazes.ObjectGazeOut.PipeTo(this.jointAttention.ObjectGazeIn);

            // Two looks at each other that overlap are a mutual gaze.
            this.gazes.PeerGazeIntervalOut.PipeTo(this.mutualGazes.PeerGazeIn);

            this.log($"Collaboration indices: looks at objects read on {eyes.Count} gaze stream(s) per participant; looks at peers read "
                + (peersFromAvatars ? "on the gaze events recomputed on the avatars." : "on the same gaze events."));
        }

        private void ConnectTaskActions()
        {
            if (this.taskActions == null)
            {
                return;
            }

            for (int i = 0; i < this.participants.Count; i++)
            {
                this.streams.TaskLogs[i].PipeTo(this.taskActions.GetPieceInput(this.participants[i]));
            }
        }

        // ------------------------------------------------------------------
        // 4. Detectors -> indicators
        // ------------------------------------------------------------------

        private void ConnectCollaborationIndices()
        {
            this.indices!.ForEach(window =>
            {
                for (int i = 0; i < this.participants.Count; i++)
                {
                    uint id = this.participants[i];

                    // Positions -> movement (head, and hands when gathered) and synchrony (head).
                    if (window.Contains(IndexNames.Movement))
                    {
                        var movement = window.Get(Indicators.Movement).Component;
                        this.streams.HeadPositionOrientationsUnity[i].Select(pose => pose.Item1).PipeTo(movement.GetPositionInput(id, BodyPartNames.Head));
                        if (this.HasHands)
                        {
                            this.streams.LeftsHandPositionOrientationsUnity[i].Select(pose => pose.Item1).PipeTo(movement.GetPositionInput(id, BodyPartNames.LeftHand));
                            this.streams.RightsHandPositionOrientationsUnity[i].Select(pose => pose.Item1).PipeTo(movement.GetPositionInput(id, BodyPartNames.RightHand));
                        }
                    }

                    if (window.Contains(IndexNames.Synchrony))
                    {
                        this.streams.HeadPositionOrientationsUnity[i].Select(pose => pose.Item1).PipeTo(window.Get(Indicators.Synchrony).Component.GetPositionInput(id, BodyPartNames.Head));
                    }

                    // Speech intervals -> verbal participation, silence and cross-talk.
                    if (window.Contains(IndexNames.VerbalParticipation))
                    {
                        this.speech!.GetSpeakingEmitter(id).PipeTo(window.Get(Indicators.VerbalParticipation).Component.GetIntervalInput(id));
                    }

                    if (window.Contains(IndexNames.Silence))
                    {
                        this.speech!.GetSpeakingEmitter(id).PipeTo(window.Get(Indicators.Silence).Component.GetIntervalInput(id));
                    }

                    if (window.Contains(IndexNames.CrossTalk))
                    {
                        this.speech!.GetSpeakingEmitter(id).PipeTo(window.Get(Indicators.CrossTalk).Component.GetIntervalInput(id));
                    }
                }

                // Events of the whole group, each detector to the indicator that counts them.
                if (window.Contains(IndexNames.TurnTaking))
                {
                    this.turnTakings!.Out.PipeTo(window.Get(Indicators.TurnTaking).Component.EventIn);
                }

                if (window.Contains(IndexNames.JointVisualAttention))
                {
                    this.jointAttention!.Out.PipeTo(window.Get(Indicators.JointVisualAttention).Component.EventIn);
                }

                if (window.Contains(IndexNames.GazeOnPeers))
                {
                    this.gazes!.PeerGazeOut.PipeTo(window.Get(Indicators.GazeOnPeers).Component.EventIn);
                }

                if (window.Contains(IndexNames.MutualGaze))
                {
                    this.mutualGazes!.Out.PipeTo(window.Get(Indicators.MutualGaze).Component.EventIn);
                }

                if (window.Contains(IndexNames.TaskParticipation))
                {
                    this.taskActions!.Out.PipeTo(window.Get(Indicators.TaskParticipation).Component.EventIn);
                }

                if (window.Contains(IndexNames.Formation))
                {
                    this.formations!.FormationEndOut.PipeTo(window.Get(Indicators.Formation).Component.EventIn);
                }

                if (window.Contains(IndexNames.Proximity))
                {
                    foreach (ParticipantPair pair in Combinatorics.Pairs(this.participants))
                    {
                        this.formations!.GetDistanceEmitter(pair).PipeTo(window.Get(Indicators.Proximity).Component.GetDistanceInput(pair));
                    }
                }
            });
        }

        // ------------------------------------------------------------------
        // 5. Indices -> profiles
        // ------------------------------------------------------------------

        private void ConnectProfiles()
        {
            List<string> missing = this.configuration.MissingProfileIndicators();
            if (missing.Count > 0)
            {
                this.log($"Collaboration profiles: these indicators are not computed and count as 0: {string.Join(", ", missing)}");
            }

            // Everything the profiles read is in the snapshot of the indices of the window.
            this.indices!.ForEach((window, instance) =>
            {
                this.profiles[window] = this.configuration.CreateProfiles(
                    this.pipeline,
                    window,
                    instance,
                    new CollaborationProfilesConfiguration
                    {
                        SessionId = this.configuration.Name,
                        PairWriter = this.OpenProfileWriter("profile_confidence", window),
                        GroupWriter = this.OpenProfileWriter("profile_computation", window),
                        DurationWriter = this.OpenProfileWriter("profile_duration_computation", window),
                    });
            });

            this.log($"Collaboration profiles: pairs and group, for the windows of {string.Join(", ", this.configuration.Windows.Select(w => $"{w.TotalSeconds:0} s"))}.");
        }

        // ------------------------------------------------------------------
        // 6. What arrives and what is produced
        // ------------------------------------------------------------------

        private void CountStreams()
        {
            // The gathered streams, per participant: a stream that stays silent explains an index that stays at zero.
            this.CountInputs("transcriptions", this.streams.Stts);
            this.CountInputs("voice activity samples", this.streams.Vads);
            this.CountInputs("head positions", this.streams.HeadPositionOrientationsUnity);
            this.CountInputs("left hand positions", this.streams.LeftsHandPositionOrientationsUnity);
            this.CountInputs("right hand positions", this.streams.RightsHandPositionOrientationsUnity);
            this.CountInputs("gaze events", this.streams.LeftGazeEvents);
            this.CountInputs("gaze events of the second eye", this.streams.RightGazeEvents);
            this.CountInputs("gaze events on the avatars", this.streams.AvatarGazeEvents);
            this.CountInputs("piece statuses", this.streams.TaskLogs);

            // What the detectors find.
            if (this.speech != null && this.turnTakings != null)
            {
                this.CountOutput("speech intervals", this.speech.Out);
                this.CountOutput("turn takings and overlaps", this.turnTakings.Out);
            }

            if (this.gazes != null && this.jointAttention != null && this.mutualGazes != null)
            {
                this.CountOutput("looks at objects", this.gazes.ObjectGazeOut);
                this.CountOutput("looks at peers", this.gazes.PeerGazeOut);
                this.CountOutput("joint visual attentions", this.jointAttention.Out);
                this.CountOutput("mutual gazes", this.mutualGazes.Out);
            }

            if (this.taskActions != null)
            {
                this.CountOutput("task actions", this.taskActions.Out);
            }

            if (this.formations != null)
            {
                this.CountOutput("formations", this.formations.FormationEndOut);
            }

            // What is written: one row of indices per tick and per window.
            this.indices!.ForEach((window, instance) => this.CountOutput($"rows of {Path.GetFileName(this.PathOf("indices", window))}", instance.SnapshotOut));
            foreach (var profile in this.profiles)
            {
                this.CountOutput($"rows of {Path.GetFileName(this.PathOf("profile_computation", profile.Key))}", profile.Value.Out);
            }
        }

        private void CountInputs<T>(string name, List<IProducer<T>> gathered)
        {
            if (!this.Complete(gathered))
            {
                return;
            }

            var tally = new Tally(name, this.participants.Count, isInput: true);
            this.tallies.Add(tally);
            for (int i = 0; i < this.participants.Count; i++)
            {
                int participant = i;
                gathered[i].Do((_, envelope) => this.Add(tally, participant, envelope.OriginatingTime));
            }
        }

        private void CountOutput<T>(string name, IProducer<T> stream)
        {
            var tally = new Tally(name, 1, isInput: false);
            this.tallies.Add(tally);
            stream.Do((_, envelope) => this.Add(tally, 0, envelope.OriginatingTime));
        }

        // The first message of a stream is reported: it tells that the stream is alive.
        private void Add(Tally tally, int index, DateTime time)
        {
            bool first;
            lock (tally)
            {
                first = tally.Counts.Sum() == 0;
                tally.Counts[index]++;
            }

            if (first)
            {
                this.log($"Collaboration indices: first {tally.Name} at {time:HH:mm:ss} (time of the data).");
            }
        }

        private void ReportTallies()
        {
            List<Tally> inputs = this.tallies.Where(tally => tally.IsInput).ToList();
            List<Tally> outputs = this.tallies.Where(tally => !tally.IsInput).ToList();

            if (inputs.Count > 0)
            {
                this.log("Collaboration indices: received, per participant: "
                    + string.Join("; ", inputs.Select(tally => $"{tally.Name} {string.Join(" / ", tally.Counts)}")) + ".");
            }

            // A stream that was gathered and stayed silent explains an index that stays at zero.
            List<string> silent = inputs
                .Where(tally => tally.Counts.Any(count => count == 0))
                .Select(tally => tally.Counts.All(count => count == 0)
                    ? $"{tally.Name} (no participant)"
                    : $"{tally.Name} (participant {string.Join(", ", Enumerable.Range(1, tally.Counts.Length).Where(participant => tally.Counts[participant - 1] == 0))})")
                .ToList();
            if (silent.Count > 0)
            {
                this.log($"Collaboration indices: nothing was received for: {string.Join("; ", silent)}.");
            }

            if (outputs.Count > 0)
            {
                this.log("Collaboration indices: produced: " + string.Join("; ", outputs.Select(tally => $"{tally.Name} {tally.Counts[0]}")) + ".");
            }
        }

        private TextWriter OpenProfileWriter(string name, TimeSpan window)
        {
            var writer = new StreamWriter(this.PathOf(name, window));
            this.profileWriters.Add(writer);
            return writer;
        }

        private string PathOf(string name, TimeSpan window)
            => Path.Combine(this.configuration.OutputFolder, $"{name}{SlidingAverageComputationSet.StoreSuffix(window)}.csv");

        /// <summary>Number of messages of one kind of stream: per participant for a gathered stream, as a whole otherwise.</summary>
        private sealed class Tally
        {
            public Tally(string name, int size, bool isInput)
            {
                this.Name = name;
                this.IsInput = isInput;
                this.Counts = new int[Math.Max(1, size)];
            }

            public string Name { get; }

            public bool IsInput { get; }

            public int[] Counts { get; }
        }
    }
}
