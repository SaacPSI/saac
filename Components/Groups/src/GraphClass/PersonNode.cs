// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using System.Collections.Generic;

    /// <summary>
    /// Node of the sociogram: what is measured on one participant. The metrics are kept by
    /// name (<see cref="GraphMetricNames"/>), so that a node holds the indices that are
    /// computed, whichever they are, and nothing for the others.
    /// </summary>
    public class PersonNode
    {
        /// <summary>
        /// Gets or sets the identifier of the participant, which is the label of the node:
        /// "1" for the first participant, "2" for the second one, and so on.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the metrics of the participant, by name. A metric that is not
        /// computed is absent.
        /// </summary>
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Gets or sets the physical activity level (<see cref="GraphMetricNames.Movement"/>);
        /// 0 when it is not computed.
        /// </summary>
        public double MovementScore
        {
            get => this.Get(GraphMetricNames.Movement);
            set => this.Set(GraphMetricNames.Movement, value);
        }

        /// <summary>
        /// Gets or sets the share of the window spent speaking, in [0, 1]
        /// (<see cref="GraphMetricNames.VerbalParticipation"/>); 0 when it is not computed.
        /// </summary>
        public double SpeakingTime
        {
            get => this.Get(GraphMetricNames.VerbalParticipation);
            set => this.Set(GraphMetricNames.VerbalParticipation, value);
        }

        /// <summary>
        /// Gets or sets the participation in the task
        /// (<see cref="GraphMetricNames.TaskParticipation"/>); 0 when it is not computed.
        /// </summary>
        public double TaskParticipation
        {
            get => this.Get(GraphMetricNames.TaskParticipation);
            set => this.Set(GraphMetricNames.TaskParticipation, value);
        }

        /// <summary>
        /// The place of the participant in the room, seen from above, in metres.
        /// </summary>
        /// <param name="mean">True for the mean place over the window of the other metrics, false for the place at the instant.</param>
        /// <param name="x">First horizontal coordinate, drawn from left to right.</param>
        /// <param name="y">Second horizontal coordinate, drawn from the bottom to the top.</param>
        /// <returns>False when the graph does not hold that place.</returns>
        public bool TryGetPlace(bool mean, out double x, out double y)
        {
            string nameX = mean ? GraphMetricNames.MeanPositionX : GraphMetricNames.PositionX;
            string nameY = mean ? GraphMetricNames.MeanPositionY : GraphMetricNames.PositionY;
            x = this.Get(nameX);
            y = this.Get(nameY);
            return this.Has(nameX) && this.Has(nameY);
        }

        /// <summary>Says whether a metric is computed for this participant.</summary>
        /// <param name="metric">Name of the metric.</param>
        /// <returns>True when the node holds a value for it.</returns>
        public bool Has(string metric) => GraphMetrics.Has(this.Metrics, metric);

        /// <summary>Reads a metric.</summary>
        /// <param name="metric">Name of the metric.</param>
        /// <param name="absent">What to return when the metric is not computed.</param>
        /// <returns>The value, or <paramref name="absent"/>.</returns>
        public double Get(string metric, double absent = 0) => GraphMetrics.Get(this.Metrics, metric, absent);

        /// <summary>Gives a value to a metric.</summary>
        /// <param name="metric">Name of the metric.</param>
        /// <param name="value">The value.</param>
        public void Set(string metric, double value) => (this.Metrics ??= new Dictionary<string, double>())[metric] = value;
    }
}
