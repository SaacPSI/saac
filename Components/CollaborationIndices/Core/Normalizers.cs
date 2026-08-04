using System;
using System.Collections.Generic;
using System.Linq;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Maps a raw index onto a comparable scale, usually [0, 1].
    /// Injected in the configuration so that the same component can publish raw counts
    /// during analysis and saturated scores during a live session.
    /// </summary>
    public interface IIndexNormalizer
    {
        string Name { get; }

        double Normalize(double rawValue);
    }

    public class IdentityNormalizer : IIndexNormalizer
    {
        public string Name => "Identity";

        public double Normalize(double rawValue) => rawValue;
    }

    /// <summary>
    /// 1 - exp(-alpha * x): saturating normalization used for every count based index.
    /// Alpha is calibrated so that a reference value (typically the P95 observed on a corpus)
    /// maps to a reference score (typically 0.95). This is the legacy NormalizeExp / ComputeAlpha.
    /// </summary>
    public class ExponentialSaturationNormalizer : IIndexNormalizer
    {
        public ExponentialSaturationNormalizer(double alpha)
        {
            this.Alpha = alpha;
        }

        public double Alpha { get; }

        public string Name => "ExponentialSaturation";

        public double Normalize(double rawValue)
        {
            if (this.Alpha <= 0 || double.IsNaN(rawValue))
            {
                return 0;
            }

            double score = 1.0 - Math.Exp(-this.Alpha * rawValue);
            return score < 0 ? 0 : score;
        }

        /// <summary>
        /// Builds a normalizer from a reference value and the score it should reach.
        /// Example: FromReference(18, 0.95) for a P95 of 18 task events on the window.
        /// </summary>
        public static ExponentialSaturationNormalizer FromReference(double referenceValue, double referenceScore = 0.95)
            => new ExponentialSaturationNormalizer(ComputeAlpha(referenceValue, referenceScore));

        /// <summary>alpha = -ln(1 - sRef) / xRef (legacy ComputeAlpha).</summary>
        public static double ComputeAlpha(double referenceValue, double referenceScore)
        {
            if (referenceValue <= 0 || referenceScore <= 0 || referenceScore >= 1)
            {
                return 0;
            }

            return -Math.Log(1 - referenceScore) / referenceValue;
        }
    }

    /// <summary>
    /// 1 - x: turns an inequality measure (Gini) into an equality score.
    /// </summary>
    public class EqualityNormalizer : IIndexNormalizer
    {
        public string Name => "Equality";

        public double Normalize(double rawValue) => 1.0 - rawValue;
    }

    /// <summary>
    /// x / max, clamped to [0, 1]. Useful when a theoretical maximum exists
    /// (e.g. a duration over the window duration).
    /// </summary>
    public class RatioNormalizer : IIndexNormalizer
    {
        public RatioNormalizer(double maximum)
        {
            this.Maximum = maximum;
        }

        public double Maximum { get; }

        public string Name => "Ratio";

        public double Normalize(double rawValue)
        {
            if (this.Maximum <= double.Epsilon)
            {
                return 0;
            }

            double score = rawValue / this.Maximum;
            return score < 0 ? 0 : (score > 1 ? 1 : score);
        }
    }

    /// <summary>
    /// Statistics shared by the group level indicators.
    /// </summary>
    public static class GroupStatistics
    {
        /// <summary>
        /// Normalized Gini index of a distribution (0 = perfectly equal, 1 = fully concentrated).
        /// Normalization by the theoretical maximum (n-1)/n makes the value comparable
        /// between groups of different sizes, which the raw Gini is not.
        /// </summary>
        public static double GiniIndex(IReadOnlyList<double> values)
        {
            if (values == null || values.Count <= 1)
            {
                return 0;
            }

            var sorted = values.OrderBy(v => v).ToList();
            int n = sorted.Count;
            double total = sorted.Sum();
            if (total <= double.Epsilon)
            {
                return 0;
            }

            double cumulated = 0;
            for (int i = 0; i < n; i++)
            {
                cumulated += (i + 1) * sorted[i];
            }

            double gini = ((2.0 * cumulated) / (n * total)) - ((n + 1.0) / n);
            double maxGini = (double)(n - 1) / n;
            double normalized = maxGini > double.Epsilon ? gini / maxGini : 0;

            if (normalized > 1)
            {
                normalized = 1;
            }

            return normalized < 0 ? 0 : normalized;
        }

        /// <summary>
        /// Identifier of the participant with the highest value.
        /// Returns null when the maximum is shared, which the legacy CheckIDMaxValue
        /// signalled with -1.
        /// </summary>
        public static uint? ArgMax(IReadOnlyDictionary<uint, double> values, double tolerance = 1e-9)
        {
            uint? best = null;
            double bestValue = double.MinValue;
            bool tie = false;

            foreach (var entry in values)
            {
                if (entry.Value > bestValue + tolerance)
                {
                    bestValue = entry.Value;
                    best = entry.Key;
                    tie = false;
                }
                else if (Math.Abs(entry.Value - bestValue) <= tolerance)
                {
                    tie = true;
                }
            }

            return tie ? (uint?)null : best;
        }

        /// <summary>
        /// Same as ArgMax but restricted to a subset (used for the pair level "talking most").
        /// </summary>
        public static uint? ArgMax(IReadOnlyDictionary<uint, double> values, IEnumerable<uint> restriction, double tolerance = 1e-9)
        {
            var subset = new Dictionary<uint, double>();
            foreach (uint participantId in restriction)
            {
                if (values.TryGetValue(participantId, out double value))
                {
                    subset[participantId] = value;
                }
            }

            return ArgMax(subset, tolerance);
        }

        /// <summary>
        /// Encoding of an identity index as a double stream, compatible with the legacy
        /// convention: 0 when nobody stands out, participantId + 1 otherwise.
        /// </summary>
        public static double EncodeIdentity(uint? participantId) => participantId.HasValue ? participantId.Value + 1 : 0;

        public static double Mean(IEnumerable<double> values)
        {
            var list = values as IList<double> ?? values.ToList();
            return list.Count == 0 ? 0 : list.Average();
        }

        public static double Sum(IEnumerable<double> values) => values.Sum();
    }
}
