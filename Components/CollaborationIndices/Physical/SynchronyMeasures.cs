using System;
using System.Collections.Generic;
using System.Linq;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Similarity measure between two movement time series of identical length.
    /// Injecting the measure in the configuration allows the same component to be
    /// reused for Pearson correlation, windowed lagged cross correlation, coherence, etc.
    /// </summary>
    public interface ISynchronyMeasure
    {
        string Name { get; }

        /// <summary>
        /// Returns a raw score, usually in [-1, 1].
        /// </summary>
        double Compute(IReadOnlyList<double> seriesA, IReadOnlyList<double> seriesB);
    }

    /// <summary>
    /// Pearson correlation. Same computation as the legacy ComputePearsonCorrelation.
    /// </summary>
    public class PearsonCorrelationMeasure : ISynchronyMeasure
    {
        public string Name => "Pearson";

        public double Compute(IReadOnlyList<double> seriesA, IReadOnlyList<double> seriesB)
        {
            if (seriesA == null || seriesB == null || seriesA.Count != seriesB.Count || seriesA.Count < 2)
            {
                return 0;
            }

            double meanA = 0;
            double meanB = 0;
            for (int i = 0; i < seriesA.Count; i++)
            {
                meanA += seriesA[i];
                meanB += seriesB[i];
            }

            meanA /= seriesA.Count;
            meanB /= seriesB.Count;

            double numerator = 0;
            double denominatorA = 0;
            double denominatorB = 0;
            for (int i = 0; i < seriesA.Count; i++)
            {
                double deltaA = seriesA[i] - meanA;
                double deltaB = seriesB[i] - meanB;
                numerator += deltaA * deltaB;
                denominatorA += deltaA * deltaA;
                denominatorB += deltaB * deltaB;
            }

            if (denominatorA <= double.Epsilon || denominatorB <= double.Epsilon)
            {
                return 0;
            }

            return numerator / Math.Sqrt(denominatorA * denominatorB);
        }
    }

    /// <summary>
    /// Windowed lagged cross correlation: best Pearson correlation over a range of lags,
    /// which captures leader / follower behaviours that a lag-0 correlation misses.
    /// </summary>
    public class LaggedCrossCorrelationMeasure : ISynchronyMeasure
    {
        private readonly PearsonCorrelationMeasure pearson = new PearsonCorrelationMeasure();

        public LaggedCrossCorrelationMeasure(int maximumLagInSamples, bool useAbsoluteValue = true)
        {
            this.MaximumLagInSamples = Math.Max(0, maximumLagInSamples);
            this.UseAbsoluteValue = useAbsoluteValue;
        }

        public string Name => "LaggedCrossCorrelation";

        public int MaximumLagInSamples { get; }

        public bool UseAbsoluteValue { get; }

        /// <summary>
        /// Lag (in samples) of the last computed score. Positive means B follows A.
        /// </summary>
        public int LastBestLag { get; private set; }

        public double Compute(IReadOnlyList<double> seriesA, IReadOnlyList<double> seriesB)
        {
            this.LastBestLag = 0;
            if (seriesA == null || seriesB == null || seriesA.Count != seriesB.Count || seriesA.Count < 2)
            {
                return 0;
            }

            double best = 0;
            double bestMagnitude = -1;
            for (int lag = -this.MaximumLagInSamples; lag <= this.MaximumLagInSamples; lag++)
            {
                var shiftedA = new List<double>();
                var shiftedB = new List<double>();
                for (int i = 0; i < seriesA.Count; i++)
                {
                    int j = i + lag;
                    if (j < 0 || j >= seriesB.Count)
                    {
                        continue;
                    }

                    shiftedA.Add(seriesA[i]);
                    shiftedB.Add(seriesB[j]);
                }

                if (shiftedA.Count < 2)
                {
                    continue;
                }

                double score = this.pearson.Compute(shiftedA, shiftedB);
                double magnitude = this.UseAbsoluteValue ? Math.Abs(score) : score;
                if (magnitude > bestMagnitude)
                {
                    bestMagnitude = magnitude;
                    best = score;
                    this.LastBestLag = lag;
                }
            }

            return best;
        }
    }

    /// <summary>
    /// How a raw score in [-1, 1] is mapped before being published.
    /// </summary>
    public enum SynchronyNormalization
    {
        /// <summary>Raw signed score.</summary>
        None,

        /// <summary>(r + 1) / 2, so anti-phase = 0, no relation = 0.5, in phase = 1.</summary>
        ZeroToOne,

        /// <summary>|r|, so anti-phase and in-phase are both considered synchronous.</summary>
        Absolute,

        /// <summary>max(r, 0), only positive coupling is considered.</summary>
        PositiveOnly,
    }

    public static class SynchronyNormalizer
    {
        public static double Normalize(double rawScore, SynchronyNormalization normalization)
        {
            double clamped = Math.Max(-1.0, Math.Min(1.0, rawScore));
            switch (normalization)
            {
                case SynchronyNormalization.ZeroToOne:
                    return (clamped + 1.0) / 2.0;
                case SynchronyNormalization.Absolute:
                    return Math.Abs(clamped);
                case SynchronyNormalization.PositiveOnly:
                    return Math.Max(0.0, clamped);
                default:
                    return clamped;
            }
        }
    }

    /// <summary>
    /// Aggregation of the pairwise scores into a sub-group or group level score.
    /// </summary>
    public interface IScoreAggregator
    {
        string Name { get; }

        double Aggregate(IEnumerable<double> scores);
    }

    public class MeanAggregator : IScoreAggregator
    {
        public string Name => "Mean";

        public double Aggregate(IEnumerable<double> scores)
        {
            var list = scores as IList<double> ?? scores.ToList();
            return list.Count == 0 ? 0 : list.Average();
        }
    }

    public class MedianAggregator : IScoreAggregator
    {
        public string Name => "Median";

        public double Aggregate(IEnumerable<double> scores)
        {
            var sorted = scores.OrderBy(s => s).ToList();
            if (sorted.Count == 0)
            {
                return 0;
            }

            int middle = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2.0;
        }
    }

    /// <summary>
    /// Weakest link aggregation: the group is only as synchronous as its least coupled pair.
    /// </summary>
    public class MinimumAggregator : IScoreAggregator
    {
        public string Name => "Minimum";

        public double Aggregate(IEnumerable<double> scores)
        {
            var list = scores as IList<double> ?? scores.ToList();
            return list.Count == 0 ? 0 : list.Min();
        }
    }
}
