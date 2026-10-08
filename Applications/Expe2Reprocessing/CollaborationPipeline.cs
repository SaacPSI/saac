// <copyright file="CollaborationPipeline.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

namespace Expe2Reprocessing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using Microsoft.Psi;
    using SAAC.CollaborationIndices;

    /// <summary>
    /// The processing of one session of the puzzle task: what is detected in the raw
    /// streams, which indices are computed from the detections, and how they are connected.
    ///
    ///   raw streams ──► detectors ──► indicators (builder) ──► profiles
    ///                                       │                      │
    ///                                       └── one CSV file per window, for each
    ///
    /// What is computed comes from the configuration: participants, windows, indicators,
    /// scores, profiles. Where the streams are is in <see cref="SessionStreams"/>. This class
    /// only connects: an indicator that is not declared has nothing connected to it.
    /// </summary>
    internal sealed class CollaborationPipeline
    {
        private readonly Pipeline pipeline;
        private readonly SessionStreams streams;
        private readonly CollaborationSessionConfiguration configuration;
        private readonly List<uint> participants;
        private readonly Dictionary<TimeSpan, TextWriter> indexWriters = new Dictionary<TimeSpan, TextWriter>();
        private readonly List<TextWriter> profileWriters = new List<TextWriter>();

        // What detects.
        private TaskPhaseDetector phases;
        private AreaPresenceDetector areas;
        private FFormationDetector formations;
        private VerbalizationDetectorComponent speech;
        private TurnTakingDetector turnTakings;
        private GazeEpisodeDetector gazes;
        private JointVisualAttentionDetector jointAttention;
        private MutualGazeDetector mutualGazes;
        private TaskActionDetector taskActions;
        private ObjectHandoverDetector handovers;

        // What is computed from the detections, one instance per window.
        private SlidingAverageComputationSet indices;

        public CollaborationPipeline(Pipeline pipeline, SessionStreams streams, CollaborationSessionConfiguration configuration)
        {
            this.pipeline = pipeline;
            this.streams = streams;
            this.configuration = configuration;
            this.participants = configuration.ParticipantIds.ToList();
        }

        /// <summary>Handovers detected, for whoever wants them: no indicator counts them.</summary>
        public IProducer<InteractionEvent> Handovers => this.handovers.Out;

        /// <summary>The CSV file of the indices of each window.</summary>
        public IEnumerable<string> OutputFiles => this.configuration.Windows.Select(window => this.PathOf("indices", window));

        public void Build()
        {
            // 1. What detects, and what is computed from the detections.
            this.CreateDetectors();
            this.DeclareCollaborationIndices();

            // 2. The streams of the session, each one connected to what processes it.
            this.ConnectTask();
            this.ConnectBodyTracking();
            this.ConnectSpeech();
            this.ConnectGaze();
            this.ConnectTaskActions();

            // 3. The detections, connected to the inputs of the indicators that are declared.
            this.ConnectCollaborationIndices();

            // 4. The indices of each window, connected to the profiles computed from them.
            if (this.configuration.ComputeProfiles)
            {
                this.ConnectProfiles();
            }
        }

        /// <summary>Flushes and closes the result files. To call once the pipeline has run.</summary>
        public void Close()
        {
            foreach (TextWriter writer in this.indexWriters.Values.Concat(this.profileWriters))
            {
                writer.Dispose();
            }

            this.indexWriters.Clear();
            this.profileWriters.Clear();
        }

        // ------------------------------------------------------------------
        // 1. Detectors and indices
        // ------------------------------------------------------------------

        private void CreateDetectors()
        {
            DetectorSettings settings = this.configuration.Detectors;

            this.phases = new TaskPhaseDetector(this.pipeline, new TaskPhaseDetectorConfiguration { LogToConsole = true });
            this.areas = new AreaPresenceDetector(this.pipeline, new AreaPresenceDetectorConfiguration { ParticipantIds = this.participants });
            this.formations = new FFormationDetector(
                this.pipeline,
                new FFormationDetectorConfiguration { ParticipantIds = this.participants, TransitionDuration = settings.FormationTransitionDuration, UpAxis = settings.UpAxis() });

            this.speech = new VerbalizationDetectorComponent(
                this.pipeline,
                new VerbalizationDetectorConfiguration { ParticipantIds = this.participants, PublishLegacyType = false, LogToConsole = false });
            this.turnTakings = new TurnTakingDetector(
                this.pipeline,
                new TurnTakingDetectorConfiguration { ParticipantIds = this.participants, ConfirmationDelay = settings.TurnTakingConfirmationDelay });

            // The peers of the replayed gaze events are numbered from 0.
            this.gazes = new GazeEpisodeDetector(
                this.pipeline,
                new GazeEpisodeDetectorConfiguration { ParticipantIds = this.participants, PeerIdMap = ParticipantIdMap.ZeroBased, MinimumDuration = settings.MinimumGazeDuration });
            this.jointAttention = new JointVisualAttentionDetector(
                this.pipeline,
                new JointVisualAttentionDetectorConfiguration { ParticipantIds = this.participants, RequireConvergingHeads = settings.RequireConvergingHeads });

            this.mutualGazes = new MutualGazeDetector(this.pipeline, new MutualGazeDetectorConfiguration { ParticipantIds = this.participants });

            this.taskActions = new TaskActionDetector(this.pipeline, new TaskActionDetectorConfiguration { ParticipantIds = this.participants });
            this.handovers = new ObjectHandoverDetector(this.pipeline);
        }

        private void DeclareCollaborationIndices()
        {
            foreach (TimeSpan window in this.configuration.Windows)
            {
                this.indexWriters[window] = new StreamWriter(this.PathOf("indices", window));
            }

            // Participants, period, phases, indicators and scores come from the configuration.
            // The clock is one tick per period of data, on the head of the first participant.
            this.indices = this.configuration.CreateBuilder(this.pipeline)
                .WithDataClock(this.streams.Participants[this.participants[0]].Head)
                .BuildSet(this.indexWriters);
        }

        // ------------------------------------------------------------------
        // 2. Raw streams -> detectors
        // ------------------------------------------------------------------

        private void ConnectTask()
        {
            this.streams.TaskEvents.PipeTo(this.phases.TaskEventIn);
            this.streams.PuzzleStatus.PipeTo(this.phases.PuzzleStatusIn);
        }

        private void ConnectBodyTracking()
        {
            foreach (uint id in this.participants)
            {
                ParticipantStreams participant = this.streams.Participants[id];
                IProducer<Tuple<Vector3, Vector3>> headPose = participant.Head.Select(this.configuration.Detectors.ToHeadPose);

                headPose.PipeTo(this.formations.GetHeadPoseInput(id));
                headPose.PipeTo(this.jointAttention.GetHeadPoseInput(id));
                participant.Area.PipeTo(this.areas.GetAreaInput(id));
            }
        }

        private void ConnectSpeech()
        {
            foreach (uint id in this.participants)
            {
                this.streams.Participants[id].Transcription.PipeTo(this.speech.GetSttInput(id));
            }

            // Speech before the task is not part of the session.
            this.phases.TaskStartOut.PipeTo(this.speech.TaskStartIn);
            this.speech.Out.PipeTo(this.turnTakings.SpeechIn);

            // A turn taking is confirmed a few seconds after it happened: the clock lets the
            // confirmations expire while nobody speaks.
            DataClock.FromStream(this.streams.Participants[this.participants[0]].Head, this.configuration.ComputationInterval).PipeTo(this.turnTakings.TickIn);
        }

        private void ConnectGaze()
        {
            foreach (uint id in this.participants)
            {
                ParticipantStreams participant = this.streams.Participants[id];
                for (int eye = 0; eye < participant.EyeGaze.Count; eye++)
                {
                    participant.EyeGaze[eye].PipeTo(this.gazes.GetObjectGazeInput(id, eye));
                }

                participant.PeerGaze.PipeTo(this.gazes.GetPeerGazeInput(id));
            }

            this.gazes.ObjectGazeOut.PipeTo(this.jointAttention.ObjectGazeIn);

            // Two looks at each other that overlap are a mutual gaze.
            this.gazes.PeerGazeIntervalOut.PipeTo(this.mutualGazes.PeerGazeIn);
        }

        private void ConnectTaskActions()
        {
            foreach (uint id in this.participants)
            {
                ParticipantStreams participant = this.streams.Participants[id];
                participant.Pieces.PipeTo(this.taskActions.GetPieceInput(id));
                for (int generator = 0; generator < participant.Generators.Count; generator++)
                {
                    participant.Generators[generator].PipeTo(this.taskActions.GetGeneratorInput(id, generator + 1));
                }

                // A grab or a release is located by the area the participant stands in.
                this.areas.GetCurrentAreaEmitter(id).PipeTo(this.taskActions.GetCurrentAreaInput(id));
            }

            this.taskActions.Out.PipeTo(this.handovers.ActionsIn);
        }

        // ------------------------------------------------------------------
        // 3. Detectors -> indicators
        // ------------------------------------------------------------------

        private void ConnectCollaborationIndices()
        {
            // The puzzles are the phases of the indices.
            this.indices.ConnectPhases(this.phases.PhaseStartOut, this.phases.PhaseEndOut);

            var positions = this.participants.ToDictionary(
                id => id,
                id => new Dictionary<string, IProducer<Vector3>>
                {
                    { BodyPartNames.Head, this.streams.Participants[id].Head.Select(pose => pose.Item1) },
                    { BodyPartNames.LeftHand, this.streams.Participants[id].LeftWrist.Select(pose => pose.Item1) },
                    { BodyPartNames.RightHand, this.streams.Participants[id].RightWrist.Select(pose => pose.Item1) },
                });

            this.indices.ForEach(window =>
            {
                foreach (uint id in this.participants)
                {
                    // Positions -> movement (head and hands) and synchrony (head).
                    if (window.Contains(IndexNames.Movement))
                    {
                        foreach (var bodyPart in positions[id])
                        {
                            bodyPart.Value.PipeTo(window.Get(Indicators.Movement).Component.GetPositionInput(id, bodyPart.Key));
                        }
                    }

                    if (window.Contains(IndexNames.Synchrony))
                    {
                        positions[id][BodyPartNames.Head].PipeTo(window.Get(Indicators.Synchrony).Component.GetPositionInput(id, BodyPartNames.Head));
                    }

                    // Speech intervals -> verbal participation, silence and cross-talk; presence intervals -> time in area.
                    if (window.Contains(IndexNames.VerbalParticipation))
                    {
                        this.speech.GetSpeakingEmitter(id).PipeTo(window.Get(Indicators.VerbalParticipation).Component.GetIntervalInput(id));
                    }

                    if (window.Contains(IndexNames.Silence))
                    {
                        this.speech.GetSpeakingEmitter(id).PipeTo(window.Get(Indicators.Silence).Component.GetIntervalInput(id));
                    }

                    if (window.Contains(IndexNames.CrossTalk))
                    {
                        this.speech.GetSpeakingEmitter(id).PipeTo(window.Get(Indicators.CrossTalk).Component.GetIntervalInput(id));
                    }

                    if (window.Contains(IndexNames.TimeInArea))
                    {
                        this.areas.GetPresenceEmitter(id).PipeTo(window.Get(Indicators.TimeInArea).Component.GetIntervalInput(id));
                    }
                }

                // Events of the whole group, each detector to the indicator that counts them.
                if (window.Contains(IndexNames.TurnTaking))
                {
                    this.turnTakings.Out.PipeTo(window.Get(Indicators.TurnTaking).Component.EventIn);
                }

                if (window.Contains(IndexNames.JointVisualAttention))
                {
                    this.jointAttention.Out.PipeTo(window.Get(Indicators.JointVisualAttention).Component.EventIn);
                }

                if (window.Contains(IndexNames.GazeOnPeers))
                {
                    this.gazes.PeerGazeOut.PipeTo(window.Get(Indicators.GazeOnPeers).Component.EventIn);
                }

                if (window.Contains(IndexNames.MutualGaze))
                {
                    this.mutualGazes.Out.PipeTo(window.Get(Indicators.MutualGaze).Component.EventIn);
                }

                if (window.Contains(IndexNames.TaskParticipation))
                {
                    this.taskActions.Out.PipeTo(window.Get(Indicators.TaskParticipation).Component.EventIn);
                }

                if (window.Contains(IndexNames.Formation))
                {
                    this.formations.FormationEndOut.PipeTo(window.Get(Indicators.Formation).Component.EventIn);
                }

                if (window.Contains(IndexNames.Proximity))
                {
                    foreach (ParticipantPair pair in Combinatorics.Pairs(this.participants))
                    {
                        this.formations.GetDistanceEmitter(pair).PipeTo(window.Get(Indicators.Proximity).Component.GetDistanceInput(pair));
                    }
                }
            });
        }

        // ------------------------------------------------------------------
        // 4. Indices -> profiles
        // ------------------------------------------------------------------

        private void ConnectProfiles()
        {
            List<string> missing = this.configuration.MissingProfileIndicators();
            if (missing.Count > 0)
            {
                Console.WriteLine($"Profiles: these indicators are not declared and count as 0: {string.Join(", ", missing)}");
            }

            // Everything the profiles read is in the snapshot of the indices of the window.
            this.indices.ForEach((window, instance) => this.configuration.CreateProfiles(
                this.pipeline,
                window,
                instance,
                new CollaborationProfilesConfiguration
                {
                    SessionId = Path.GetFileName(this.streams.DatasetPath),
                    PairWriter = this.OpenProfileWriter("profile_confidence", window),
                    GroupWriter = this.OpenProfileWriter("profile_computation", window),
                    DurationWriter = this.OpenProfileWriter("profile_duration_computation", window),
                }));
        }

        private TextWriter OpenProfileWriter(string name, TimeSpan window)
        {
            var writer = new StreamWriter(this.PathOf(name, window));
            this.profileWriters.Add(writer);
            return writer;
        }

        private string PathOf(string name, TimeSpan window)
            => Path.Combine(this.configuration.OutputFolder, $"{name}{SlidingAverageComputationSet.StoreSuffix(window)}.csv");
    }
}
