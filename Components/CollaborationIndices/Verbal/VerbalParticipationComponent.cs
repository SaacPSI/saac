using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using SAAC.PipelineServices;

namespace SAAC.CollaborationIndices
{
    public class VerbalParticipationConfiguration : IndexComponentConfiguration
    {
        /// <summary>Category of the speaking intervals.</summary>
        public string SpeakingCategory { get; set; } = IndexCategories.Speaking;

        /// <summary>
        /// If true, the index is the share of the window spent speaking (0 to 1).
        /// If false, the raw duration in seconds is published.
        /// </summary>
        public bool AsRatioOfWindow { get; set; } = true;

        /// <summary>
        /// Below this participation ratio, the pair or group is considered too silent for
        /// an equality index to be meaningful (legacy threshold of 0.15).
        /// </summary>
        public double MinimumRatioForEquality { get; set; } = 0.15;
    }

    /// <summary>
    /// Verbal participation: time spent speaking by each participant over the sliding window.
    /// Generalizes the legacy SlidingAverageUpdateVerbalParticipation to N participants, and
    /// replaces its three branches of window edge handling by InteractionInterval.DurationWithin.
    ///
    /// Outputs:
    ///  - Out: ratio (or duration) per participant;
    ///  - SpeakingTimesOut: raw seconds per participant, for the equality index and the export;
    ///  - PairOut: mean participation of each pair;
    ///  - GroupOut: mean participation of the group;
    ///  - EqualityUsableOut: true when the group speaks enough for an equality index to make sense.
    /// </summary>
    public class VerbalParticipationComponent : MultiParticipantIntervalComponent<VerbalParticipationConfiguration>,
                                                IProducer<Dictionary<uint, double>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VerbalParticipationComponent"/> class,
        /// storing its speaking time and group streams through the dataset pipeline.
        /// </summary>
        public VerbalParticipationComponent(Pipeline pipeline, DatasetPipeline server, VerbalParticipationConfiguration configuration, string name = nameof(VerbalParticipationComponent))
            : this(pipeline, configuration, name, server == null ? null : new IndexStore(server))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VerbalParticipationComponent"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline hosting the component.</param>
        /// <param name="configuration">Configuration of the indicator.</param>
        /// <param name="name">Name of the component, prefix of its streams.</param>
        /// <param name="store">Destination of the speaking time and group streams. Null stores nothing.</param>
        public VerbalParticipationComponent(Pipeline pipeline, VerbalParticipationConfiguration configuration, string name = nameof(VerbalParticipationComponent), IndexStore? store = null)
            : base(pipeline, configuration, name)
        {
            this.Out = pipeline.CreateEmitter<Dictionary<uint, double>>(this, $"{name}-Individual");
            this.SpeakingTimesOut = pipeline.CreateEmitter<Dictionary<uint, double>>(this, $"{name}-SpeakingTimes");
            this.SpeakingTimesStringOut = pipeline.CreateEmitter<string>(this, $"{name}-SpeakingTimesString");
            this.PairOut = pipeline.CreateEmitter<Dictionary<ParticipantPair, double>>(this, $"{name}-Pair");
            this.GroupOut = pipeline.CreateEmitter<double>(this, $"{name}-Group");
            this.EqualityUsableOut = pipeline.CreateEmitter<bool>(this, $"{name}-EqualityUsable");
            store?.Write(pipeline, $"{name}-SpeakingTimes", this.SpeakingTimesOut);
            store?.Write(pipeline, $"{name}-SpeakingTimesString", this.SpeakingTimesStringOut);
            store?.Write(pipeline, $"{name}-Group", this.GroupOut);

            this.ParticipantEmitters = new KeyedEmitters<uint>(pipeline, this, configuration.ParticipantIds, $"{name}-Individual");
            this.PairEmitters = new KeyedEmitters<ParticipantPair>(pipeline, this, configuration.Pairs(), $"{name}-Pair");
        }

        public Emitter<Dictionary<uint, double>> Out { get; }

        public Emitter<Dictionary<uint, double>> SpeakingTimesOut { get; }

        public Emitter<string> SpeakingTimesStringOut { get; }

        public Emitter<Dictionary<ParticipantPair, double>> PairOut { get; }

        public Emitter<double> GroupOut { get; }

        public Emitter<bool> EqualityUsableOut { get; }

        public KeyedEmitters<uint> ParticipantEmitters { get; }

        public KeyedEmitters<ParticipantPair> PairEmitters { get; }

        protected override void Compute(DateTime originatingTime)
        {
            Dictionary<uint, double> speakingTimes = this.DurationsByParticipant(this.configuration.SpeakingCategory, originatingTime);
            this.SpeakingTimesOut.Post(speakingTimes, originatingTime);
            double windowSeconds = this.WindowSeconds;

            // One entry per declared participant, whatever their identifiers: the message used
            // to read speakingTimes[0] and [1], which threw for any other numbering.
            var ids = this.configuration.ParticipantIds;
            string message = "Speaking Time for "
                + string.Join(" and ", ids.Select((id, index) => $"User {index + 1}: {speakingTimes[id]}"))
                + $" over the last {windowSeconds:0.###} seconds window";
            this.SpeakingTimesStringOut.Post(message, originatingTime);
            var participation = new Dictionary<uint, double>();
            foreach (var entry in speakingTimes)
            {
                double value = this.configuration.AsRatioOfWindow && windowSeconds > double.Epsilon
                    ? entry.Value / windowSeconds
                    : entry.Value;
                participation[entry.Key] = this.Normalize(value);
            }

            this.Out.Post(participation, originatingTime);
            this.ParticipantEmitters.PostAll(participation, originatingTime);

            var pairParticipation = new Dictionary<ParticipantPair, double>();
            foreach (ParticipantPair pair in this.configuration.Pairs())
            {
                double sum = speakingTimes[pair.A] + speakingTimes[pair.B];
                pairParticipation[pair] = windowSeconds > double.Epsilon ? sum / (windowSeconds * 2) : 0;
            }

            this.PairOut.Post(pairParticipation, originatingTime);
            this.PairEmitters.PostAll(pairParticipation, originatingTime);

            double groupParticipation = windowSeconds > double.Epsilon
                ? speakingTimes.Values.Sum() / (windowSeconds * this.configuration.ParticipantIds.Count)
                : 0;

            this.GroupOut.Post(groupParticipation, originatingTime);
            this.EqualityUsableOut.Post(groupParticipation > this.configuration.MinimumRatioForEquality, originatingTime);
        }
    }

    /// <summary>
    /// Conventional category names. Using them keeps the adapters, the components and the
    /// export columns aligned across the framework.
    /// </summary>
    public static class IndexCategories
    {
        // Intervals
        public const string Speaking = "Speaking";
        public const string InArea = "InArea";
        public const string GazeOnPeer = "GazeOnPeer";
        public const string Formation = "Formation";

        // Events
        public const string TurnTakingWithOverlap = "TurnTakingWithOverlap";
        public const string TurnTakingWithoutOverlap = "TurnTakingWithoutOverlap";
        public const string Overlap = "Overlap";
        public const string Silence = "Silence";
        public const string JointVisualAttention = "JointVisualAttention";
        public const string MutualGaze = "MutualGaze";
        public const string Grab = "Grab";
        public const string Ungrab = "Ungrab";
        public const string Place = "Place";
        public const string Unplace = "Unplace";
        public const string Color = "Color";
        public const string Uncolor = "Uncolor";
        public const string GeneratorInteraction = "GeneratorInteraction";
        public const string FormationEnd = "FormationEnd";
    }
}
