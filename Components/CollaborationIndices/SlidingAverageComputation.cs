using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Calibration of an index: the reference value (typically the P95 measured on a corpus)
    /// that should map to ReferenceScore once normalized.
    /// Replaces the alpha1..alpha12bis fields and the DefineAlpha switch of the legacy class.
    /// </summary>
    public class IndexCalibration
    {
        public Dictionary<string, double> ReferenceValues { get; set; } = new Dictionary<string, double>();

        public double ReferenceScore { get; set; } = 0.95;

        public IIndexNormalizer NormalizerFor(string indexName)
        {
            if (this.ReferenceValues != null && this.ReferenceValues.TryGetValue(indexName, out double reference) && reference > 0)
            {
                return ExponentialSaturationNormalizer.FromReference(reference, this.ReferenceScore);
            }

            return new IdentityNormalizer();
        }

        /// <summary>
        /// Calibration measured on the 27 session corpus, for a 30 s window.
        /// The keys are the index names used by SlidingAverageComputation.
        /// </summary>
        public static IndexCalibration Threshold20Seconds() => new IndexCalibration
        {
            ReferenceValues = new Dictionary<string, double>
            {
                { IndexNames.JointVisualAttention, 8 },
                { IndexNames.JointVisualAttentionPair, 5 },
                { IndexNames.GazeOnPeers, 7 },
                { IndexNames.TaskParticipation, 18 },
                { IndexNames.Formation, 4 },
                { IndexNames.Movement, 0.041 },
                { IndexNames.VerbalParticipation, 20 },
                { IndexNames.TurnTakingWithOverlap, 2 },
                { IndexNames.TurnTakingWithoutOverlap, 4 },
                { IndexNames.TurnTakingWithoutOverlapPair, 3 },
            },
        };

        public static IndexCalibration Threshold30Seconds() => new IndexCalibration
        {
            ReferenceValues = new Dictionary<string, double>
            {
                { IndexNames.JointVisualAttention, 11 },
                { IndexNames.JointVisualAttentionPair, 8 },
                { IndexNames.GazeOnPeers, 10 },
                { IndexNames.TaskParticipation, 27 },
                { IndexNames.Formation, 5 },
                { IndexNames.Movement, 0.041 },
                { IndexNames.VerbalParticipation, 30 },
                { IndexNames.TurnTakingWithOverlap, 2 },
                { IndexNames.TurnTakingWithoutOverlap, 5 },
                { IndexNames.TurnTakingWithoutOverlapPair, 4 },
            },
        };

        public static IndexCalibration Threshold45Seconds() => new IndexCalibration
        {
            ReferenceValues = new Dictionary<string, double>
            {
                { IndexNames.JointVisualAttention, 18 },
                { IndexNames.JointVisualAttentionPair, 11 },
                { IndexNames.GazeOnPeers, 14 },
                { IndexNames.TaskParticipation, 32 },
                { IndexNames.Formation, 8 },
                { IndexNames.Movement, 0.037 },
                { IndexNames.VerbalParticipation, 45 },
                { IndexNames.TurnTakingWithOverlap, 3 },
                { IndexNames.TurnTakingWithoutOverlap, 5 },
                { IndexNames.TurnTakingWithoutOverlapPair, 3 },
            },
        };

        /// <summary>
        /// Calibration matching a window duration. The calibration and the window must always
        /// be chosen together: a P95 measured on 20 s means nothing on a 45 s window, and using
        /// the wrong one silently compresses or stretches every normalized score.
        /// Windows without a measured calibration fall back to the closest one.
        /// </summary>
        public static IndexCalibration ForWindow(TimeSpan window)
        {
            int seconds = (int)Math.Round(window.TotalSeconds);
            switch (seconds)
            {
                case 20:
                    return Threshold20Seconds();
                case 30:
                    return Threshold30Seconds();
                case 45:
                    return Threshold45Seconds();
                default:
                    return seconds < 25 ? Threshold20Seconds() : (seconds < 35 ? Threshold30Seconds() : Threshold45Seconds());
            }
        }
    }

    /// <summary>Names of the indices, shared by the calibration, the fusion and the export.</summary>
    public static class IndexNames
    {
        public const string Movement = "Movement";
        public const string Synchrony = "Synchrony";
        public const string VerbalParticipation = "VerbalParticipation";
        public const string SpeechEquality = "SpeechEquality";
        public const string TaskParticipation = "TaskParticipation";
        public const string TaskEquality = "TaskEquality";
        public const string TurnTakingWithOverlap = "TurnTakingWithOverlap";
        public const string TurnTakingWithoutOverlap = "TurnTakingWithoutOverlap";
        public const string TurnTakingWithoutOverlapPair = "TurnTakingWithoutOverlapPair";
        public const string Overlap = "Overlap";
        public const string JointVisualAttention = "JointVisualAttention";
        public const string JointVisualAttentionPair = "JointVisualAttentionPair";
        public const string GazeOnPeers = "GazeOnPeers";
        public const string Formation = "Formation";
        public const string Proximity = "Proximity";
        public const string TimeInArea = "TimeInArea";
        public const string AttentionLevel = "AttentionLevel";
        public const string CollaborationScore = "CollaborationScore";
    }

    /// <summary>
    /// Configuration of the whole indicator pipeline. One object replaces the dozen of
    /// scattered fields of SlidingAverageSpeechConfiguration.
    /// </summary>
    public class SlidingAverageConfiguration
    {
        /// <summary>Participants of the session. Any number, contiguous or not.</summary>
        public List<uint> ParticipantIds { get; set; } = new List<uint> { 0, 1, 2 };

        /// <summary>Sliding window of every index (the legacy threshold, in milliseconds).</summary>
        public TimeSpan WindowDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Publication period of the indices.</summary>
        public TimeSpan ComputationInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>Period of the fast clock used by the attention accumulator.</summary>
        public TimeSpan AttentionInterval { get; set; } = TimeSpan.FromMilliseconds(50);

        public IndexCalibration Calibration { get; set; } = IndexCalibration.Threshold20Seconds();

        /// <summary>Areas of the environment tracked by the spatial indices.</summary>
        public List<string> Areas { get; set; } = new List<string>();

        /// <summary>Areas whose group occupancy is aggregated (planning area).</summary>
        public List<string> PlanningAreas { get; set; } = new List<string>();

        /// <summary>Body parts used by the activity level.</summary>
        public List<string> ActivityBodyParts { get; set; } = new List<string> { BodyPartNames.Head, BodyPartNames.LeftHand, BodyPartNames.RightHand };

        /// <summary>Body parts used by the synchrony.</summary>
        public List<string> SynchronyBodyParts { get; set; } = new List<string> { BodyPartNames.Head };

        /// <summary>Destination of the CSV export. Null disables it.</summary>
        public TextWriter IndicesWriter { get; set; }

        /// <summary>Enables the components that are only relevant for a scripted task.</summary>
        public bool UseTaskIndices { get; set; } = true;

        public bool UseSpatialIndices { get; set; } = true;

        /// <summary>
        /// If true, the instance creates its own clock generators. Set it to false when several
        /// instances run side by side, and drive ClockIn and AttentionClockIn from a single
        /// shared generator: two generators of the same period produce two slightly different
        /// tick sequences, which would make the instances impossible to compare row by row.
        /// </summary>
        public bool UseInternalClock { get; set; } = true;

        /// <summary>Deep enough copy to give another instance an independent configuration.</summary>
        public SlidingAverageConfiguration Clone() => new SlidingAverageConfiguration
        {
            ParticipantIds = new List<uint>(this.ParticipantIds),
            WindowDuration = this.WindowDuration,
            ComputationInterval = this.ComputationInterval,
            AttentionInterval = this.AttentionInterval,
            Calibration = this.Calibration,
            Areas = new List<string>(this.Areas),
            PlanningAreas = new List<string>(this.PlanningAreas),
            ActivityBodyParts = new List<string>(this.ActivityBodyParts),
            SynchronyBodyParts = new List<string>(this.SynchronyBodyParts),
            IndicesWriter = this.IndicesWriter,
            UseTaskIndices = this.UseTaskIndices,
            UseSpatialIndices = this.UseSpatialIndices,
            UseInternalClock = this.UseInternalClock,
        };
    }

    /// <summary>
    /// Orchestrator of the collaboration indices. It is not a computing class any more: it
    /// instantiates the indicator components, connects them to each other and exposes their
    /// outputs. All the computation lives in the components, which are independently testable
    /// and reusable in another study.
    ///
    /// What used to be one class of about three thousand lines, tied to three participants,
    /// is now roughly two hundred lines of wiring over a dozen generic components.
    ///
    /// Connect the raw streams with the adapters of IndexAdapters, then connect the phase
    /// boundaries; everything else is internal.
    /// </summary>
    public class SlidingAverageComputation
    {
        private readonly Pipeline pipeline;
        private readonly SlidingAverageConfiguration configuration;

        public SlidingAverageComputation(Pipeline pipeline, SlidingAverageConfiguration configuration, string name = nameof(SlidingAverageComputation))
        {
            this.pipeline = pipeline;
            this.configuration = configuration;

            var participants = configuration.ParticipantIds;
            var calibration = configuration.Calibration;

            // ---------- Clock and phase gating ----------
            this.Gate = new PhaseGateComponent(pipeline, new PhaseGateConfiguration
            {
                WarmUpDuration = configuration.WindowDuration,
                TickInterval = configuration.ComputationInterval,
            }, $"{name}-Gate");

            if (configuration.UseInternalClock)
            {
                Generators.Repeat(pipeline, true, configuration.ComputationInterval).PipeTo(this.Gate.ClockIn);
            }

            // ---------- Physical ----------
            this.ActivityLevel = new PhysicalActivityLevelComponent(pipeline, new PhysicalActivityLevelConfiguration
            {
                ParticipantIds = participants,
                BodyParts = configuration.ActivityBodyParts,
                WindowDuration = configuration.WindowDuration,
                ComputationInterval = configuration.ComputationInterval,
                AdditionalWindows = new List<TimeSpan> { TimeSpan.FromSeconds(5) },
                ComputeOnDataReception = false,
            }, $"{name}-ActivityLevel");

            this.Synchrony = new PhysicalSynchronyComponent(pipeline, new PhysicalSynchronyConfiguration
            {
                ParticipantIds = participants,
                BodyParts = configuration.SynchronyBodyParts,
                WindowDuration = configuration.WindowDuration,
                ComputationInterval = configuration.ComputationInterval,
                ComputeSubsets = participants.Count >= 3,
                SubsetSize = 3,
                ComputeOnDataReception = false,
            }, $"{name}-Synchrony");

            // ---------- Verbal ----------
            this.VerbalParticipation = new VerbalParticipationComponent(pipeline, new VerbalParticipationConfiguration
            {
                ParticipantIds = participants,
                WindowDuration = configuration.WindowDuration,
                ComputationInterval = configuration.ComputationInterval,
            }, $"{name}-VerbalParticipation");

            this.SpeechEquality = new EqualityIndexComponent(pipeline, new EqualityIndexConfiguration
            {
                ParticipantIds = participants,
                WindowDuration = configuration.WindowDuration,
                SubsetSize = participants.Count >= 3 ? 3 : 0,
            }, $"{name}-SpeechEquality");

            this.TurnTaking = new TurnTakingComponent(pipeline, new TurnTakingConfiguration
            {
                ParticipantIds = participants,
                WindowDuration = configuration.WindowDuration,
                ComputationInterval = configuration.ComputationInterval,
                CategoryNormalizers = new Dictionary<string, IIndexNormalizer>
                {
                    { IndexCategories.TurnTakingWithOverlap, calibration.NormalizerFor(IndexNames.TurnTakingWithOverlap) },
                    { IndexCategories.TurnTakingWithoutOverlap, calibration.NormalizerFor(IndexNames.TurnTakingWithoutOverlap) },
                },
                PairNormalizers = new Dictionary<string, IIndexNormalizer>
                {
                    { IndexCategories.TurnTakingWithoutOverlap, calibration.NormalizerFor(IndexNames.TurnTakingWithoutOverlapPair) },
                },
            }, $"{name}-TurnTaking");

            // ---------- Gaze ----------
            this.JointVisualAttention = new JointVisualAttentionComponent(pipeline, new JointVisualAttentionConfiguration
            {
                ParticipantIds = participants,
                WindowDuration = configuration.WindowDuration,
                ComputationInterval = configuration.ComputationInterval,
                GroupNormalizer = calibration.NormalizerFor(IndexNames.JointVisualAttention),
                PairNormalizer = calibration.NormalizerFor(IndexNames.JointVisualAttentionPair),
            }, $"{name}-JVA");

            this.GazeOnPeers = new GazeOnPeersComponent(pipeline, new GazeOnPeersConfiguration
            {
                ParticipantIds = participants,
                WindowDuration = configuration.WindowDuration,
                ComputationInterval = configuration.ComputationInterval,
                GroupNormalizer = calibration.NormalizerFor(IndexNames.GazeOnPeers),
            }, $"{name}-GazeOnPeers");

            this.AttentionLevel = new AttentionLevelComponent(pipeline, new AttentionLevelConfiguration
            {
                ParticipantIds = participants,
                Step = configuration.AttentionInterval,
            }, $"{name}-AttentionLevel");

            if (configuration.UseInternalClock)
            {
                Generators.Repeat(pipeline, true, configuration.AttentionInterval).PipeTo(this.AttentionLevel.TickIn);
            }

            // ---------- Task ----------
            if (configuration.UseTaskIndices)
            {
                this.TaskParticipation = new TaskParticipationComponent(pipeline, new TaskParticipationConfiguration
                {
                    ParticipantIds = participants,
                    WindowDuration = configuration.WindowDuration,
                    ComputationInterval = configuration.ComputationInterval,
                    GroupNormalizer = calibration.NormalizerFor(IndexNames.TaskParticipation),
                }, $"{name}-TaskParticipation");

                this.TaskEquality = new EqualityIndexComponent(pipeline, new EqualityIndexConfiguration
                {
                    ParticipantIds = participants,
                    WindowDuration = configuration.WindowDuration,
                    SubsetSize = participants.Count >= 3 ? 3 : 0,
                }, $"{name}-TaskEquality");
            }

            // ---------- Spatial ----------
            if (configuration.UseSpatialIndices)
            {
                this.TimeInArea = new TimeInAreaComponent(pipeline, new TimeInAreaConfiguration
                {
                    ParticipantIds = participants,
                    Areas = configuration.Areas,
                    GroupAggregatedAreas = configuration.PlanningAreas,
                    WindowDuration = configuration.WindowDuration,
                    ComputationInterval = configuration.ComputationInterval,
                }, $"{name}-TimeInArea");

                this.FFormation = new FFormationComponent(pipeline, new FFormationConfiguration
                {
                    ParticipantIds = participants,
                    WindowDuration = configuration.WindowDuration,
                    ComputationInterval = configuration.ComputationInterval,
                    GroupNormalizer = calibration.NormalizerFor(IndexNames.Formation),
                    PairNormalizer = calibration.NormalizerFor(IndexNames.Formation),
                    SubsetSize = participants.Count >= 3 ? 3 : 0,
                }, $"{name}-FFormation");

                this.Proximity = new ProximityComponent(pipeline, new ProximityConfiguration
                {
                    ParticipantIds = participants,
                    ComputationInterval = configuration.ComputationInterval,
                }, $"{name}-Proximity");
            }

            // ---------- Dominance ----------
            this.TalkingMost = this.CreateDominance($"{name}-TalkingMost", participants);
            this.TaskingMost = this.CreateDominance($"{name}-TaskingMost", participants);

            // ---------- Fusion ----------
            this.CollaborationScore = new CollaborationScoreComponent(pipeline, new CollaborationScoreConfiguration
            {
                ParticipantIds = participants,
                Dimensions = DefaultDimensions(),
            }, $"{name}-CollaborationScore");

            this.Graph = new InteractionGraphComponent(pipeline, new InteractionGraphConfiguration
            {
                ParticipantIds = participants,
            }, $"{name}-Graph");

            if (configuration.IndicesWriter != null)
            {
                this.Export = new IndexExportComponent(pipeline, new IndexExportConfiguration
                {
                    Writer = configuration.IndicesWriter,
                    Columns = DefaultExportColumns(participants),
                }, $"{name}-Export");
            }

            this.ConnectInternals();
        }

        // ---------- Components, exposed so that raw streams can be connected ----------
        public PhaseGateComponent Gate { get; }

        /// <summary>Clock of the windowed indices. Connect it when UseInternalClock is false.</summary>
        public Receiver<bool> ClockIn => this.Gate.ClockIn;

        /// <summary>Clock of the attention accumulator. Connect it when UseInternalClock is false.</summary>
        public Receiver<bool> AttentionClockIn => this.AttentionLevel.TickIn;

        /// <summary>Window of this instance, useful to label its outputs and its store.</summary>
        public TimeSpan WindowDuration => this.configuration.WindowDuration;

        public PhysicalActivityLevelComponent ActivityLevel { get; }

        public PhysicalSynchronyComponent Synchrony { get; }

        public VerbalParticipationComponent VerbalParticipation { get; }

        public EqualityIndexComponent SpeechEquality { get; }

        public TurnTakingComponent TurnTaking { get; }

        public JointVisualAttentionComponent JointVisualAttention { get; }

        public GazeOnPeersComponent GazeOnPeers { get; }

        public AttentionLevelComponent AttentionLevel { get; }

        public TaskParticipationComponent TaskParticipation { get; }

        public EqualityIndexComponent TaskEquality { get; }

        public TimeInAreaComponent TimeInArea { get; }

        public FFormationComponent FFormation { get; }

        public ProximityComponent Proximity { get; }

        public DominanceIdentityComponent TalkingMost { get; }

        public DominanceIdentityComponent TaskingMost { get; }

        public CollaborationScoreComponent CollaborationScore { get; }

        public InteractionGraphComponent Graph { get; }

        public IndexExportComponent Export { get; }

        /// <summary>Complete state of the interaction at every tick.</summary>
        public IProducer<InteractionGraph> Out => this.Graph;

        private DominanceIdentityComponent CreateDominance(string name, List<uint> participants)
            => new DominanceIdentityComponent(this.pipeline, new DominanceIdentityConfiguration { ParticipantIds = participants }, name);

        /// <summary>
        /// Wiring between the components. This is the only place where the dependencies
        /// between the indices are expressed.
        /// </summary>
        private void ConnectInternals()
        {
            // The gate paces every windowed component.
            this.Gate.Out.PipeTo(this.ActivityLevel.TickIn);
            this.Gate.Out.PipeTo(this.Synchrony.TickIn);
            this.Gate.Out.PipeTo(this.VerbalParticipation.TickIn);
            this.Gate.Out.PipeTo(this.TurnTaking.TickIn);
            this.Gate.Out.PipeTo(this.JointVisualAttention.TickIn);
            this.Gate.Out.PipeTo(this.GazeOnPeers.TickIn);
            this.TaskParticipation?.TickIn.PipeFrom(this.Gate.Out);
            this.TimeInArea?.TickIn.PipeFrom(this.Gate.Out);
            this.FFormation?.TickIn.PipeFrom(this.Gate.Out);
            this.Proximity?.TickIn.PipeFrom(this.Gate.Out);

            // Equality and dominance derive from the participation distributions.
            this.VerbalParticipation.SpeakingTimesOut.PipeTo(this.SpeechEquality.In);
            this.VerbalParticipation.SpeakingTimesOut.PipeTo(this.TalkingMost.In);

            if (this.TaskParticipation != null)
            {
                this.TaskParticipation.RawIndividualOut.PipeTo(this.TaskEquality.In);
                this.TaskParticipation.RawIndividualOut.PipeTo(this.TaskingMost.In);
            }

            // Fusion of the group level indices.
            this.ActivityLevel.GroupActivityLevelOut.PipeTo(this.CollaborationScore.GetIndexInput(IndexNames.Movement));
            this.Synchrony.GroupSynchronyOut.PipeTo(this.CollaborationScore.GetIndexInput(IndexNames.Synchrony));
            this.VerbalParticipation.GroupOut.PipeTo(this.CollaborationScore.GetIndexInput(IndexNames.VerbalParticipation));
            this.JointVisualAttention.GroupOut.PipeTo(this.CollaborationScore.GetIndexInput(IndexNames.JointVisualAttention));
            this.GazeOnPeers.GroupOut.PipeTo(this.CollaborationScore.GetIndexInput(IndexNames.GazeOnPeers));
            this.TurnTaking.GetGroupEmitter(IndexCategories.TurnTakingWithoutOverlap).PipeTo(this.CollaborationScore.GetIndexInput(IndexNames.TurnTakingWithoutOverlap));

            // An equality index only makes sense when the group is active enough; the
            // validity flag excludes it from its dimension instead of biasing the score.
            this.SpeechEquality.Out
                .Select(gini => 1.0 - gini)
                .PipeTo(this.CollaborationScore.GetIndexInput(IndexNames.SpeechEquality));
            this.VerbalParticipation.EqualityUsableOut.PipeTo(this.CollaborationScore.GetValidityInput(IndexNames.SpeechEquality));

            if (this.TaskParticipation != null)
            {
                this.TaskParticipation.GroupOut.PipeTo(this.CollaborationScore.GetIndexInput(IndexNames.TaskParticipation));
                this.TaskEquality.Out
                    .Select(gini => 1.0 - gini)
                    .PipeTo(this.CollaborationScore.GetIndexInput(IndexNames.TaskEquality));
            }

            if (this.FFormation != null)
            {
                this.FFormation.GroupOut.PipeTo(this.CollaborationScore.GetIndexInput(IndexNames.Formation));
            }

            // Interaction graph.
            this.ActivityLevel.Out.PipeTo(this.Graph.GetNodeMetricInput(IndexNames.Movement));
            this.VerbalParticipation.Out.PipeTo(this.Graph.GetNodeMetricInput(IndexNames.VerbalParticipation));
            this.AttentionLevel.Out.PipeTo(this.Graph.GetNodeMetricInput(IndexNames.AttentionLevel));
            this.Synchrony.Out.PipeTo(this.Graph.GetEdgeMetricInput(IndexNames.Synchrony));
            this.SpeechEquality.PairOut.PipeTo(this.Graph.GetEdgeMetricInput(IndexNames.SpeechEquality));
            this.JointVisualAttention.PairOut.PipeTo(this.Graph.GetEdgeMetricInput(IndexNames.JointVisualAttention));
            this.GazeOnPeers.DirectedPairOut.PipeTo(this.Graph.GetDirectedEdgeMetricInput(IndexNames.GazeOnPeers));
            this.CollaborationScore.Out.PipeTo(this.Graph.GetGroupMetricInput(IndexNames.CollaborationScore));

            if (this.TaskParticipation != null)
            {
                this.TaskParticipation.Out.PipeTo(this.Graph.GetNodeMetricInput(IndexNames.TaskParticipation));
                this.TaskEquality.PairOut.PipeTo(this.Graph.GetEdgeMetricInput(IndexNames.TaskEquality));
            }

            if (this.Proximity != null)
            {
                this.Proximity.Out.PipeTo(this.Graph.GetEdgeMetricInput(IndexNames.Proximity));
            }

            // Export.
            if (this.Export != null)
            {
                this.Gate.Out.PipeTo(this.Export.TickIn);
                this.ActivityLevel.Out.PipeTo(this.Export.GetParticipantColumnsInput(IndexNames.Movement));
                this.VerbalParticipation.Out.PipeTo(this.Export.GetParticipantColumnsInput(IndexNames.VerbalParticipation));
                this.Synchrony.Out.PipeTo(this.Export.GetPairColumnsInput(IndexNames.Synchrony));
                this.SpeechEquality.Out.PipeTo(this.Export.GetColumnInput(IndexNames.SpeechEquality));
                this.JointVisualAttention.GroupOut.PipeTo(this.Export.GetColumnInput(IndexNames.JointVisualAttention));
                this.GazeOnPeers.GroupOut.PipeTo(this.Export.GetColumnInput(IndexNames.GazeOnPeers));
                this.CollaborationScore.Out.PipeTo(this.Export.GetColumnInput(IndexNames.CollaborationScore));

                if (this.TaskParticipation != null)
                {
                    this.TaskParticipation.Out.PipeTo(this.Export.GetParticipantColumnsInput(IndexNames.TaskParticipation));
                    this.TaskEquality.Out.PipeTo(this.Export.GetColumnInput(IndexNames.TaskEquality));
                }
            }
        }

        /// <summary>
        /// Dimensions of the legacy collaboration model. Redefine them in the configuration
        /// to test another decomposition without touching any component.
        /// </summary>
        public static List<ScoreDimension> DefaultDimensions() => new List<ScoreDimension>
        {
            new ScoreDimension
            {
                Name = "Dominance",
                IndexNames = new List<string> { IndexNames.SpeechEquality, IndexNames.TaskEquality },
                ConditionalIndexNames = new List<string> { IndexNames.SpeechEquality, IndexNames.TaskEquality },
            },
            new ScoreDimension
            {
                Name = "JointAttention",
                IndexNames = new List<string> { IndexNames.JointVisualAttention, IndexNames.GazeOnPeers },
            },
            new ScoreDimension
            {
                Name = "CommunicationProcessManagement",
                IndexNames = new List<string> { IndexNames.VerbalParticipation, IndexNames.TurnTakingWithoutOverlap },
            },
            new ScoreDimension
            {
                Name = "SpatialBehaviour",
                IndexNames = new List<string> { IndexNames.Formation, IndexNames.Synchrony },
            },
            new ScoreDimension
            {
                Name = "Engagement",
                IndexNames = new List<string> { IndexNames.Movement, IndexNames.TaskParticipation },
            },
        };

        private static List<string> DefaultExportColumns(List<uint> participants)
        {
            var columns = new List<string>();
            foreach (uint participantId in participants)
            {
                columns.Add($"{IndexNames.Movement}_{participantId}");
                columns.Add($"{IndexNames.VerbalParticipation}_{participantId}");
                columns.Add($"{IndexNames.TaskParticipation}_{participantId}");
            }

            foreach (ParticipantPair pair in Combinatorics.Pairs(participants))
            {
                columns.Add($"{IndexNames.Synchrony}_{pair}");
            }

            columns.AddRange(new[]
            {
                IndexNames.SpeechEquality,
                IndexNames.TaskEquality,
                IndexNames.JointVisualAttention,
                IndexNames.GazeOnPeers,
                IndexNames.CollaborationScore,
            });

            return columns;
        }
    }

    internal static class ReceiverExtensions
    {
        /// <summary>Reversed PipeTo, so that a null component can be skipped with ?.</summary>
        public static void PipeFrom<T>(this Receiver<T> receiver, IProducer<T> source) => source.PipeTo(receiver);
    }
}
