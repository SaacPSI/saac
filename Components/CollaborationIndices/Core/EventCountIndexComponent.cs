using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Level at which an indicator is published. Several levels can be combined.
    /// </summary>
    [Flags]
    public enum IndexLevel
    {
        None = 0,
        Individual = 1,
        UndirectedPair = 2,
        DirectedPair = 4,
        Group = 8,
    }

    public enum GroupAggregation
    {
        Sum,
        Mean,
        Max,
        Min,
    }

    public class EventCountIndexConfiguration : IndexComponentConfiguration
    {
        /// <summary>Event categories summed by this index (e.g. Grab, Place, Color).</summary>
        public List<string> Categories { get; set; } = new List<string>();

        public IndexLevel Levels { get; set; } = IndexLevel.Individual | IndexLevel.Group;

        public GroupAggregation GroupAggregation { get; set; } = GroupAggregation.Sum;

        /// <summary>If true, counts are divided by the window duration to give events per second.</summary>
        public bool AsRatePerSecond { get; set; } = false;

        /// <summary>Normalizer of the individual values. Falls back to Normalizer when null.</summary>
        public IIndexNormalizer IndividualNormalizer { get; set; } = null;

        public IIndexNormalizer PairNormalizer { get; set; } = null;

        public IIndexNormalizer GroupNormalizer { get; set; } = null;

        public IIndexNormalizer IndividualOrDefault => this.IndividualNormalizer ?? this.Normalizer;

        public IIndexNormalizer PairOrDefault => this.PairNormalizer ?? this.Normalizer;

        public IIndexNormalizer GroupOrDefault => this.GroupNormalizer ?? this.Normalizer;
    }

    /// <summary>
    /// Counts events over the sliding window and publishes them at the configured levels.
    ///
    /// This single component covers every count based index of the framework:
    /// task participation, turn taking, overlaps, joint visual attention, gaze on peers,
    /// F-formations. A domain component only has to derive from it when it adds a specific
    /// rule (penalty, in-degree, initiator identity).
    ///
    /// Outputs:
    ///  - Out / IndividualOut: Dictionary&lt;uint, double&gt;;
    ///  - PairOut: Dictionary&lt;ParticipantPair, double&gt;;
    ///  - DirectedPairOut: Dictionary&lt;DirectedParticipantPair, double&gt;;
    ///  - GroupOut: double;
    ///  - one dedicated double emitter per participant / pair / directed pair.
    /// </summary>
    public class EventCountIndexComponent<TConfiguration> : MultiParticipantEventComponent<TConfiguration>,
                                                            IProducer<Dictionary<uint, double>>
        where TConfiguration : EventCountIndexConfiguration
    {
        public EventCountIndexComponent(Pipeline pipeline, TConfiguration configuration, string name)
            : base(pipeline, configuration, name)
        {
            this.Out = pipeline.CreateEmitter<Dictionary<uint, double>>(this, $"{name}-Individual");
            this.PairOut = pipeline.CreateEmitter<Dictionary<ParticipantPair, double>>(this, $"{name}-Pair");
            this.DirectedPairOut = pipeline.CreateEmitter<Dictionary<DirectedParticipantPair, double>>(this, $"{name}-DirectedPair");
            this.GroupOut = pipeline.CreateEmitter<double>(this, $"{name}-Group");
            this.RawIndividualOut = pipeline.CreateEmitter<Dictionary<uint, double>>(this, $"{name}-RawIndividual");

            this.ParticipantEmitters = new KeyedEmitters<uint>(pipeline, this, configuration.ParticipantIds, $"{name}-Individual");
            this.PairEmitters = new KeyedEmitters<ParticipantPair>(pipeline, this, configuration.Pairs(), $"{name}-Pair");
            this.DirectedPairEmitters = new KeyedEmitters<DirectedParticipantPair>(pipeline, this, configuration.DirectedPairs(), $"{name}-DirectedPair");
        }

        /// <summary>Normalized value of every participant.</summary>
        public Emitter<Dictionary<uint, double>> Out { get; }

        /// <summary>Raw counts, before normalization, for analysis and export.</summary>
        public Emitter<Dictionary<uint, double>> RawIndividualOut { get; }

        public Emitter<Dictionary<ParticipantPair, double>> PairOut { get; }

        public Emitter<Dictionary<DirectedParticipantPair, double>> DirectedPairOut { get; }

        public Emitter<double> GroupOut { get; }

        public KeyedEmitters<uint> ParticipantEmitters { get; }

        public KeyedEmitters<ParticipantPair> PairEmitters { get; }

        public KeyedEmitters<DirectedParticipantPair> DirectedPairEmitters { get; }

        /// <summary>
        /// Raw individual values. Overridden by the components that apply a domain rule
        /// (for instance the penalty of the inefficient actions in task participation).
        /// </summary>
        protected virtual Dictionary<uint, double> ComputeIndividualRaw(DateTime currentTime)
            => this.CountsByParticipant(this.configuration.Categories, currentTime);

        protected virtual Dictionary<ParticipantPair, double> ComputePairRaw(DateTime currentTime)
        {
            var totals = new Dictionary<ParticipantPair, double>();
            foreach (ParticipantPair pair in this.configuration.Pairs())
            {
                double total = 0;
                foreach (string category in this.configuration.Categories)
                {
                    total += this.events.CountDirectedWithin(pair.A, pair.B, category, this.WindowStart(currentTime), currentTime);
                    total += this.events.CountDirectedWithin(pair.B, pair.A, category, this.WindowStart(currentTime), currentTime);
                }

                totals[pair] = total;
            }

            return totals;
        }

        protected virtual Dictionary<DirectedParticipantPair, double> ComputeDirectedPairRaw(DateTime currentTime)
        {
            var totals = new Dictionary<DirectedParticipantPair, double>();
            foreach (DirectedParticipantPair pair in this.configuration.DirectedPairs())
            {
                double total = 0;
                foreach (string category in this.configuration.Categories)
                {
                    total += this.events.CountDirectedWithin(pair.From, pair.To, category, this.WindowStart(currentTime), currentTime);
                }

                totals[pair] = total;
            }

            return totals;
        }

        /// <summary>Hook called after the standard outputs have been posted.</summary>
        protected virtual void OnComputed(Dictionary<uint, double> individualRaw, DateTime originatingTime)
        {
        }

        protected override void Compute(DateTime originatingTime)
        {
            var individualRaw = this.ComputeIndividualRaw(originatingTime);
            if (this.configuration.AsRatePerSecond && this.WindowSeconds > double.Epsilon)
            {
                foreach (uint key in individualRaw.Keys.ToList())
                {
                    individualRaw[key] /= this.WindowSeconds;
                }
            }

            this.RawIndividualOut.Post(individualRaw, originatingTime);

            if (this.configuration.Levels.HasFlag(IndexLevel.Individual))
            {
                var normalized = Apply(individualRaw, this.configuration.IndividualOrDefault);
                this.Out.Post(normalized, originatingTime);
                this.ParticipantEmitters.PostAll(normalized, originatingTime);
            }

            if (this.configuration.Levels.HasFlag(IndexLevel.UndirectedPair))
            {
                var normalized = Apply(this.ComputePairRaw(originatingTime), this.configuration.PairOrDefault);
                this.PairOut.Post(normalized, originatingTime);
                this.PairEmitters.PostAll(normalized, originatingTime);
            }

            if (this.configuration.Levels.HasFlag(IndexLevel.DirectedPair))
            {
                var normalized = Apply(this.ComputeDirectedPairRaw(originatingTime), this.configuration.DirectedPairOrDefaultNormalizer());
                this.DirectedPairOut.Post(normalized, originatingTime);
                this.DirectedPairEmitters.PostAll(normalized, originatingTime);
            }

            if (this.configuration.Levels.HasFlag(IndexLevel.Group))
            {
                double aggregated = Aggregate(individualRaw.Values, this.configuration.GroupAggregation);
                this.GroupOut.Post(this.configuration.GroupOrDefault.Normalize(aggregated), originatingTime);
            }

            this.OnComputed(individualRaw, originatingTime);
        }

        protected static Dictionary<TKey, double> Apply<TKey>(Dictionary<TKey, double> values, IIndexNormalizer normalizer)
        {
            var result = new Dictionary<TKey, double>();
            foreach (var entry in values)
            {
                result[entry.Key] = normalizer.Normalize(entry.Value);
            }

            return result;
        }

        protected static double Aggregate(IEnumerable<double> values, GroupAggregation aggregation)
        {
            var list = values as IList<double> ?? values.ToList();
            if (list.Count == 0)
            {
                return 0;
            }

            switch (aggregation)
            {
                case GroupAggregation.Mean:
                    return list.Average();
                case GroupAggregation.Max:
                    return list.Max();
                case GroupAggregation.Min:
                    return list.Min();
                default:
                    return list.Sum();
            }
        }
    }

    /// <summary>Non generic shortcut for the common case.</summary>
    public class EventCountIndexComponent : EventCountIndexComponent<EventCountIndexConfiguration>
    {
        public EventCountIndexComponent(Pipeline pipeline, EventCountIndexConfiguration configuration, string name = nameof(EventCountIndexComponent))
            : base(pipeline, configuration, name)
        {
        }
    }

    internal static class NormalizerExtensions
    {
        /// <summary>Directed pairs reuse the pair normalizer.</summary>
        public static IIndexNormalizer DirectedPairOrDefaultNormalizer(this EventCountIndexConfiguration configuration)
            => configuration.PairOrDefault;
    }
}
