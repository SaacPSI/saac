using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Physical synchrony between N participants, over the last WindowDuration.
    /// Pipeline of the index:
    ///  1. all the participants are resampled on a common time grid anchored on absolute time
    ///     (only the new grid points are computed at each iteration, see IncrementalGridResampler),
    ///  2. a movement series is built for each participant (weighted displacement between two grid points),
    ///  3. every pair of series is compared with the configured ISynchronyMeasure (Pearson by default),
    ///  4. the raw scores are normalized then aggregated at the sub-group and group levels.
    ///
    /// Real time cost per computation: O(newGridPoints * P * B * log n) for the resampling,
    /// plus O(P^2 * W) for the correlations, where W is the number of grid points in the window.
    /// The latency of the index is about EffectiveSettlingDelay + ComputationInterval.
    ///
    /// Outputs:
    ///  - Out: normalized score of every pair;
    ///  - PairCorrelationsOut: raw (signed) score of every pair, useful for analysis;
    ///  - GetPairSynchronyEmitter(pair): one stream per pair, for storage and display;
    ///  - SubsetSynchronyOut: score of every sub-group of SubsetSize participants (triads when 3);
    ///  - GroupSynchronyOut: single score for the whole group.
    /// </summary>
    public class PhysicalSynchronyComponent : MultiParticipantSlidingWindowComponent<PhysicalSynchronyConfiguration>,
                                              IProducer<Dictionary<ParticipantPair, double>>
    {
        private readonly List<ParticipantPair> pairs = new List<ParticipantPair>();
        private readonly List<ParticipantSubset> subsets = new List<ParticipantSubset>();
        private readonly Dictionary<ParticipantPair, Emitter<double>> pairEmitters = new Dictionary<ParticipantPair, Emitter<double>>();
        private readonly IncrementalGridResampler resampler;

        public PhysicalSynchronyComponent(Pipeline pipeline, PhysicalSynchronyConfiguration configuration, string name = nameof(PhysicalSynchronyComponent))
            : base(pipeline, configuration, name)
        {
            this.resampler = new IncrementalGridResampler(
                this.buffer,
                configuration.ParticipantIds,
                configuration.BodyParts,
                configuration.SamplingInterval,
                configuration.MaxDelta);

            this.Out = pipeline.CreateEmitter<Dictionary<ParticipantPair, double>>(this, $"{name}-PairSynchrony");
            this.PairCorrelationsOut = pipeline.CreateEmitter<Dictionary<ParticipantPair, double>>(this, $"{name}-PairCorrelation");
            this.SubsetSynchronyOut = pipeline.CreateEmitter<Dictionary<ParticipantSubset, double>>(this, $"{name}-SubsetSynchrony");
            this.GroupSynchronyOut = pipeline.CreateEmitter<double>(this, $"{name}-GroupSynchrony");

            foreach (ParticipantPair pair in Combinatorics.Pairs(configuration.ParticipantIds))
            {
                this.pairs.Add(pair);
                this.pairEmitters[pair] = pipeline.CreateEmitter<double>(this, $"{name}-Synchrony-{pair}");
            }

            if (configuration.ComputeSubsets && configuration.SubsetSize >= 3 && configuration.SubsetSize <= configuration.ParticipantIds.Count)
            {
                this.subsets.AddRange(Combinatorics.Subsets(configuration.ParticipantIds, configuration.SubsetSize));
            }
        }

        /// <summary>
        /// Normalized synchrony of every pair.
        /// </summary>
        public Emitter<Dictionary<ParticipantPair, double>> Out { get; }

        /// <summary>
        /// Raw, signed measure of every pair (before normalization).
        /// </summary>
        public Emitter<Dictionary<ParticipantPair, double>> PairCorrelationsOut { get; }

        /// <summary>
        /// Score of every sub-group of SubsetSize participants. Only posted when ComputeSubsets is true.
        /// </summary>
        public Emitter<Dictionary<ParticipantSubset, double>> SubsetSynchronyOut { get; }

        /// <summary>
        /// Single score for the whole group. Only posted when ComputeGroupScore is true.
        /// </summary>
        public Emitter<double> GroupSynchronyOut { get; }

        public IReadOnlyList<ParticipantPair> Pairs => this.pairs;

        public IReadOnlyList<ParticipantSubset> Subsets => this.subsets;

        public Emitter<double> GetPairSynchronyEmitter(ParticipantPair pair)
        {
            if (!this.pairEmitters.TryGetValue(pair, out var emitter))
            {
                throw new ArgumentException($"Pair {pair} is not part of the configuration of {this.name}.", nameof(pair));
            }

            return emitter;
        }

        public Emitter<double> GetPairSynchronyEmitter(uint participantA, uint participantB)
            => this.GetPairSynchronyEmitter(new ParticipantPair(participantA, participantB));

        protected override bool CanCompute(DateTime originatingTime)
        {
            // Cheap guard: every participant must still be tracked and have at least two samples.
            string referenceBodyPart = this.configuration.BodyParts[0];
            foreach (uint participantId in this.configuration.ParticipantIds)
            {
                var track = this.buffer.GetTrack(participantId, referenceBodyPart);
                if (track.Count < 2)
                {
                    return false;
                }

                if (this.configuration.RequireAllParticipants && track.IsStale(originatingTime, this.configuration.MaximumSampleAge))
                {
                    return false;
                }
            }

            return true;
        }

        protected override void Compute(DateTime originatingTime)
        {
            this.resampler.Update(originatingTime, this.configuration.WindowDuration, this.configuration.EffectiveSettlingDelay);

            Dictionary<uint, Dictionary<long, double>> movementSeries = this.BuildMovementSeries();

            HashSet<long> commonGridPoints = null;
            if (this.configuration.RequireAllParticipants)
            {
                commonGridPoints = IntersectKeys(movementSeries.Values);
                if (commonGridPoints.Count < this.configuration.MinimumSampleCount && this.configuration.WarmUp == WarmUpBehavior.WaitForEnoughData)
                {
                    return;
                }
            }

            var rawScores = new Dictionary<ParticipantPair, double>();
            var normalizedScores = new Dictionary<ParticipantPair, double>();

            foreach (ParticipantPair pair in this.pairs)
            {
                ExtractAlignedSeries(movementSeries[pair.A], movementSeries[pair.B], commonGridPoints, out var seriesA, out var seriesB);

                double raw = seriesA.Count >= this.configuration.MinimumSampleCount
                    ? this.configuration.Measure.Compute(seriesA, seriesB)
                    : 0;
                double normalized = SynchronyNormalizer.Normalize(raw, this.configuration.Normalization);

                rawScores[pair] = raw;
                normalizedScores[pair] = normalized;
                this.pairEmitters[pair].Post(normalized, originatingTime);
            }

            this.PairCorrelationsOut.Post(rawScores, originatingTime);
            this.Out.Post(normalizedScores, originatingTime);

            if (this.subsets.Count > 0)
            {
                var subsetScores = new Dictionary<ParticipantSubset, double>();
                foreach (ParticipantSubset subset in this.subsets)
                {
                    var scores = new List<double>();
                    foreach (ParticipantPair pair in subset.Pairs())
                    {
                        if (normalizedScores.TryGetValue(pair, out double score))
                        {
                            scores.Add(score);
                        }
                    }

                    subsetScores[subset] = this.configuration.Aggregator.Aggregate(scores);
                }

                this.SubsetSynchronyOut.Post(subsetScores, originatingTime);
            }

            if (this.configuration.ComputeGroupScore)
            {
                this.GroupSynchronyOut.Post(this.configuration.Aggregator.Aggregate(normalizedScores.Values), originatingTime);
            }
        }

        /// <summary>
        /// Scalar movement series of each participant, keyed by grid index so that two
        /// participants can always be realigned even when some grid points are missing.
        /// </summary>
        private Dictionary<uint, Dictionary<long, double>> BuildMovementSeries()
        {
            var bodyParts = this.configuration.BodyParts;
            double totalWeight = this.configuration.NormalizeWeights ? this.configuration.TotalWeight() : 1.0;
            double intervalSeconds = this.configuration.SamplingInterval.TotalSeconds;

            var series = new Dictionary<uint, Dictionary<long, double>>();

            foreach (uint participantId in this.configuration.ParticipantIds)
            {
                var participantSeries = new Dictionary<long, double>();
                var gridSamples = this.resampler.GetSamples(participantId);

                for (int i = 1; i < gridSamples.Count; i++)
                {
                    Vector3[] previous = gridSamples[i - 1].Positions;
                    Vector3[] current = gridSamples[i].Positions;

                    double displacement = 0;
                    for (int b = 0; b < bodyParts.Count; b++)
                    {
                        displacement += this.configuration.GetWeight(bodyParts[b]) * Vector3.Distance(current[b], previous[b]);
                    }

                    if (this.configuration.NormalizeWeights && totalWeight > double.Epsilon)
                    {
                        displacement /= totalWeight;
                    }

                    if (this.configuration.Unit == MovementUnit.DisplacementPerSecond)
                    {
                        double elapsedSeconds = (gridSamples[i].Index - gridSamples[i - 1].Index) * intervalSeconds;
                        displacement = elapsedSeconds > double.Epsilon ? displacement / elapsedSeconds : 0;
                    }

                    participantSeries[gridSamples[i].Index] = displacement;
                }

                series[participantId] = participantSeries;
            }

            return series;
        }

        private static HashSet<long> IntersectKeys(IEnumerable<Dictionary<long, double>> series)
        {
            HashSet<long> common = null;
            foreach (var serie in series)
            {
                if (common == null)
                {
                    common = new HashSet<long>(serie.Keys);
                }
                else
                {
                    common.IntersectWith(serie.Keys);
                }
            }

            return common ?? new HashSet<long>();
        }

        private static void ExtractAlignedSeries(
            Dictionary<long, double> serieA,
            Dictionary<long, double> serieB,
            HashSet<long> restriction,
            out List<double> alignedA,
            out List<double> alignedB)
        {
            alignedA = new List<double>();
            alignedB = new List<double>();

            var indices = new List<long>(serieA.Keys);
            indices.Sort();
            foreach (long index in indices)
            {
                if (restriction != null && !restriction.Contains(index))
                {
                    continue;
                }

                if (serieB.TryGetValue(index, out double valueB))
                {
                    alignedA.Add(serieA[index]);
                    alignedB.Add(valueB);
                }
            }
        }
    }
}
