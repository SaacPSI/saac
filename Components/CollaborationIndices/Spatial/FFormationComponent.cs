using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class FFormationConfiguration : EventCountIndexConfiguration
    {
        public FFormationConfiguration()
        {
            this.Categories = new List<string> { IndexCategories.FormationEnd };
            this.Levels = IndexLevel.UndirectedPair | IndexLevel.Group;
            this.GroupAggregation = GroupAggregation.Sum;
        }

        /// <summary>
        /// If greater than 2, formations involving this many participants are also published
        /// (3 for the "All" formation of a triad).
        /// </summary>
        public int SubsetSize { get; set; } = 0;

        public override int MinimumParticipantCount => 2;
    }

    /// <summary>
    /// F-formations: number of shared spatial formations observed over the sliding window.
    /// An event carries the two participants of the formation; for a formation involving more
    /// than two people, post one event per pair, or use the subset level.
    ///
    /// The legacy version kept one queue per hard coded key ("01", "02", "12", "All");
    /// here the keys are ParticipantPair and ParticipantSubset.
    /// </summary>
    public class FFormationComponent : EventCountIndexComponent<FFormationConfiguration>
    {
        private readonly List<ParticipantSubset> subsets = new List<ParticipantSubset>();

        public FFormationComponent(Pipeline pipeline, FFormationConfiguration configuration, string name = nameof(FFormationComponent))
            : base(pipeline, configuration, name)
        {
            this.SubsetOut = pipeline.CreateEmitter<Dictionary<ParticipantSubset, double>>(this, $"{name}-Subset");

            if (configuration.SubsetSize >= 3 && configuration.SubsetSize <= configuration.ParticipantIds.Count)
            {
                this.subsets.AddRange(Combinatorics.Subsets(configuration.ParticipantIds, configuration.SubsetSize));
            }
        }

        public Emitter<Dictionary<ParticipantSubset, double>> SubsetOut { get; }

        protected override void OnComputed(Dictionary<uint, double> individualRaw, DateTime originatingTime)
        {
            if (this.subsets.Count == 0)
            {
                return;
            }

            Dictionary<ParticipantPair, double> pairCounts = this.ComputePairRaw(originatingTime);
            var subsetScores = new Dictionary<ParticipantSubset, double>();

            foreach (ParticipantSubset subset in this.subsets)
            {
                double total = 0;
                foreach (ParticipantPair pair in subset.Pairs())
                {
                    if (pairCounts.TryGetValue(pair, out double value))
                    {
                        total += value;
                    }
                }

                subsetScores[subset] = this.configuration.PairOrDefault.Normalize(total);
            }

            this.SubsetOut.Post(subsetScores, originatingTime);
        }
    }

    public class ProximityConfiguration : IndexComponentConfiguration
    {
        /// <summary>Distance beyond which the proximity score is zero, in the unit of the input.</summary>
        public double MaximumDistance { get; set; } = 3.0;

        /// <summary>If true, the published value is a proximity score in [0, 1] instead of a raw distance.</summary>
        public bool AsProximityScore { get; set; } = true;

        public override int MinimumParticipantCount => 2;
    }

    /// <summary>
    /// Interpersonal distance, one value per pair. The component only buffers the last distance
    /// of each pair and republishes it on the shared clock, so that the spatial dimension is
    /// sampled at the same rate as the other indices.
    /// </summary>
    public class ProximityComponent : IndexComponentBase<ProximityConfiguration>,
                                      IProducer<Dictionary<ParticipantPair, double>>
    {
        private readonly Dictionary<ParticipantPair, Receiver<double>> distanceReceivers = new Dictionary<ParticipantPair, Receiver<double>>();
        private readonly Dictionary<ParticipantPair, double> lastDistance = new Dictionary<ParticipantPair, double>();

        public ProximityComponent(Pipeline pipeline, ProximityConfiguration configuration, string name = nameof(ProximityComponent))
            : base(pipeline, configuration, name)
        {
            this.Out = pipeline.CreateEmitter<Dictionary<ParticipantPair, double>>(this, $"{name}-Pair");
            this.PairEmitters = new KeyedEmitters<ParticipantPair>(pipeline, this, configuration.Pairs(), $"{name}-Pair");

            foreach (ParticipantPair pair in configuration.Pairs())
            {
                ParticipantPair key = pair;
                this.lastDistance[key] = double.NaN;
                this.distanceReceivers[key] = pipeline.CreateReceiver<double>(
                    this,
                    (distance, envelope) =>
                    {
                        this.lastDistance[key] = distance;
                        this.OnDataReceived(envelope.OriginatingTime);
                    },
                    $"{name}-Distance-{key}");
            }
        }

        public Emitter<Dictionary<ParticipantPair, double>> Out { get; }

        public KeyedEmitters<ParticipantPair> PairEmitters { get; }

        public Receiver<double> GetDistanceInput(ParticipantPair pair) => this.distanceReceivers[pair];

        public Receiver<double> GetDistanceInput(uint participantA, uint participantB)
            => this.GetDistanceInput(new ParticipantPair(participantA, participantB));

        protected override void Compute(DateTime originatingTime)
        {
            var values = new Dictionary<ParticipantPair, double>();
            foreach (var entry in this.lastDistance)
            {
                if (double.IsNaN(entry.Value))
                {
                    continue;
                }

                double value = entry.Value;
                if (this.configuration.AsProximityScore && this.configuration.MaximumDistance > double.Epsilon)
                {
                    value = 1.0 - Math.Min(1.0, value / this.configuration.MaximumDistance);
                }

                values[entry.Key] = value;
            }

            this.Out.Post(values, originatingTime);
            this.PairEmitters.PostAll(values, originatingTime);
        }
    }
}
