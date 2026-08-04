using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class EqualityIndexConfiguration : IndexComponentConfiguration
    {
        public EqualityIndexConfiguration()
        {
            // Transform component: it publishes as soon as a distribution arrives.
            this.ComputationInterval = TimeSpan.Zero;
        }

        /// <summary>
        /// If true, an equality score (1 - Gini) is published instead of the inequality index.
        /// </summary>
        public bool PublishAsEquality { get; set; } = false;

        /// <summary>
        /// Below this total, the distribution is considered too sparse for the index to be
        /// meaningful and UndefinedValue is published (legacy: -1 when nobody speaks).
        /// </summary>
        public double MinimumTotal { get; set; } = 0;

        public double UndefinedValue { get; set; } = -1;

        /// <summary>If greater than 2, a score is also published for every subset of this size.</summary>
        public int SubsetSize { get; set; } = 0;

        public override int MinimumParticipantCount => 2;
    }

    /// <summary>
    /// Equality of a distribution over the participants, measured by a normalized Gini index.
    /// The component is generic: connect the speaking times to get the speech equality, the
    /// task participation counts to get the task equality, the movement to get the activity
    /// equality, and so on. The legacy pipeline duplicated this computation for each modality.
    ///
    /// Inputs:
    ///  - In: Dictionary&lt;uint, double&gt;, one value per participant.
    ///
    /// Outputs:
    ///  - Out: group level index;
    ///  - PairOut: index of every pair;
    ///  - SubsetOut: index of every subset of SubsetSize participants.
    /// </summary>
    public class EqualityIndexComponent : IndexComponentBase<EqualityIndexConfiguration>,
                                          IConsumer<Dictionary<uint, double>>,
                                          IProducer<double>
    {
        private readonly List<ParticipantSubset> subsets = new List<ParticipantSubset>();
        private Dictionary<uint, double> lastValues = new Dictionary<uint, double>();

        public EqualityIndexComponent(Pipeline pipeline, EqualityIndexConfiguration configuration, string name = nameof(EqualityIndexComponent))
            : base(pipeline, configuration, name)
        {
            this.In = pipeline.CreateReceiver<Dictionary<uint, double>>(this, this.Receive, $"{name}-In");
            this.Out = pipeline.CreateEmitter<double>(this, $"{name}-Group");
            this.PairOut = pipeline.CreateEmitter<Dictionary<ParticipantPair, double>>(this, $"{name}-Pair");
            this.SubsetOut = pipeline.CreateEmitter<Dictionary<ParticipantSubset, double>>(this, $"{name}-Subset");
            this.PairEmitters = new KeyedEmitters<ParticipantPair>(pipeline, this, configuration.Pairs(), $"{name}-Pair");

            if (configuration.SubsetSize >= 3 && configuration.SubsetSize <= configuration.ParticipantIds.Count)
            {
                this.subsets.AddRange(Combinatorics.Subsets(configuration.ParticipantIds, configuration.SubsetSize));
            }
        }

        public Receiver<Dictionary<uint, double>> In { get; }

        public Emitter<double> Out { get; }

        public Emitter<Dictionary<ParticipantPair, double>> PairOut { get; }

        public Emitter<Dictionary<ParticipantSubset, double>> SubsetOut { get; }

        public KeyedEmitters<ParticipantPair> PairEmitters { get; }

        protected override void Compute(DateTime originatingTime)
        {
            var values = this.lastValues;
            if (values.Count == 0)
            {
                return;
            }

            this.Out.Post(this.Score(values.Values.ToList()), originatingTime);

            var pairScores = new Dictionary<ParticipantPair, double>();
            foreach (ParticipantPair pair in this.configuration.Pairs())
            {
                if (values.TryGetValue(pair.A, out double a) && values.TryGetValue(pair.B, out double b))
                {
                    pairScores[pair] = this.Score(new List<double> { a, b });
                }
            }

            this.PairOut.Post(pairScores, originatingTime);
            this.PairEmitters.PostAll(pairScores, originatingTime);

            if (this.subsets.Count > 0)
            {
                var subsetScores = new Dictionary<ParticipantSubset, double>();
                foreach (ParticipantSubset subset in this.subsets)
                {
                    var subsetValues = subset.Members.Where(values.ContainsKey).Select(m => values[m]).ToList();
                    subsetScores[subset] = this.Score(subsetValues);
                }

                this.SubsetOut.Post(subsetScores, originatingTime);
            }
        }

        private double Score(IReadOnlyList<double> values)
        {
            if (values.Count == 0 || values.Sum() <= this.configuration.MinimumTotal)
            {
                return this.configuration.UndefinedValue;
            }

            double gini = GroupStatistics.GiniIndex(values);
            return this.configuration.PublishAsEquality ? 1.0 - gini : gini;
        }

        private void Receive(Dictionary<uint, double> values, Envelope envelope)
        {
            if (values == null)
            {
                return;
            }

            this.lastValues = values;
            this.TryCompute(envelope.OriginatingTime);
        }
    }
}
