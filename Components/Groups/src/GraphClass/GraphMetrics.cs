// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Reading of the metrics and of the identifiers of the sociogram, shared by its classes
    /// and by what draws it.
    /// </summary>
    public static class GraphMetrics
    {
        /// <summary>Says whether a set of metrics holds a usable value for a name.</summary>
        /// <param name="metrics">The metrics, possibly null.</param>
        /// <param name="metric">Name of the metric.</param>
        /// <returns>True when there is a value and it is a number.</returns>
        public static bool Has(Dictionary<string, double>? metrics, string metric)
            => metrics != null && metric != null && metrics.TryGetValue(metric, out double value) && !double.IsNaN(value) && !double.IsInfinity(value);

        /// <summary>Reads a metric.</summary>
        /// <param name="metrics">The metrics, possibly null.</param>
        /// <param name="metric">Name of the metric.</param>
        /// <param name="absent">What to return when there is no usable value.</param>
        /// <returns>The value, or <paramref name="absent"/>.</returns>
        public static double Get(Dictionary<string, double>? metrics, string metric, double absent = 0)
            => Has(metrics, metric) ? metrics![metric] : absent;

        /// <summary>
        /// The number an identifier stands for: 3 for "3". The participants of a session are
        /// numbered, and the group metrics that name one of them hold that number.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The number, or null when the identifier is not one.</returns>
        public static int? Number(string? id)
            => int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) ? number : (int?)null;

        /// <summary>
        /// Order of two identifiers: by number when both are numbers, so that "2" comes
        /// before "10", otherwise as text.
        /// </summary>
        /// <param name="a">First identifier.</param>
        /// <param name="b">Second identifier.</param>
        /// <returns>Negative when <paramref name="a"/> comes first, positive when it comes last, 0 when they are the same.</returns>
        public static int CompareIds(string? a, string? b)
        {
            int? first = Number(a);
            int? second = Number(b);
            if (first.HasValue && second.HasValue && first.Value != second.Value)
            {
                return first.Value.CompareTo(second.Value);
            }

            return string.CompareOrdinal(a ?? string.Empty, b ?? string.Empty);
        }

        /// <summary>Key of the pair two identifiers form, the same in both orders.</summary>
        /// <param name="a">One identifier.</param>
        /// <param name="b">The other one.</param>
        /// <returns>The key.</returns>
        public static string PairKey(string a, string b) => CompareIds(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";
    }
}
