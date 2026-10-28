// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Edge of the sociogram: what is measured on a pair of participants. The metrics are
    /// kept by name (<see cref="GraphMetricNames"/>). Those that have a direction, the looks
    /// of one participant at the other, are kept once per direction: from <see cref="First"/>
    /// to <see cref="Second"/> in <see cref="ForwardMetrics"/>, the other way in
    /// <see cref="BackwardMetrics"/>.
    /// </summary>
    public class PersonEdge
    {
        /// <summary>Gets or sets one participant of the pair.</summary>
        public string FromId_1 { get; set; } = string.Empty;

        /// <summary>Gets or sets the other participant of the pair.</summary>
        public string ToId_1 { get; set; } = string.Empty;

        /// <summary>Gets or sets the participant <see cref="ToId_1"/> names: the pair, read the other way.</summary>
        public string FromId_2 { get; set; } = string.Empty;

        /// <summary>Gets or sets the participant <see cref="FromId_1"/> names: the pair, read the other way.</summary>
        public string ToId_2 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the metrics of the pair, by name. A metric that is not computed is absent.
        /// </summary>
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();

        /// <summary>Gets or sets the metrics from <see cref="First"/> to <see cref="Second"/>, by name.</summary>
        public Dictionary<string, double> ForwardMetrics { get; set; } = new Dictionary<string, double>();

        /// <summary>Gets or sets the metrics from <see cref="Second"/> to <see cref="First"/>, by name.</summary>
        public Dictionary<string, double> BackwardMetrics { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Gets the participant of the pair that comes first: the lower number, so that the
        /// directions of an edge do not depend on the order its identifiers were given in.
        /// </summary>
        public string First => this.Ends().First;

        /// <summary>Gets the participant of the pair that comes second.</summary>
        public string Second => this.Ends().Second;

        /// <summary>
        /// Gets or sets the distance between the two participants, in metres
        /// (<see cref="GraphMetricNames.Distance"/>); 0 when it is not known. It places the
        /// nodes. The proximity score of the collaboration indices is the metric
        /// <see cref="GraphMetricNames.Proximity"/>.
        /// </summary>
        public double Proximity
        {
            get => this.Get(GraphMetricNames.Distance);
            set => this.Set(GraphMetricNames.Distance, value);
        }

        /// <summary>
        /// Gets or sets the synchrony of the movements, in [0, 1]
        /// (<see cref="GraphMetricNames.Synchrony"/>); 0 when it is not computed.
        /// </summary>
        public double Synchrony
        {
            get => this.Get(GraphMetricNames.Synchrony);
            set => this.Set(GraphMetricNames.Synchrony, value);
        }

        /// <summary>
        /// Gets or sets the looks of <see cref="First"/> at <see cref="Second"/>
        /// (<see cref="GraphMetricNames.GazeOnPeers"/>); 0 when they are not computed.
        /// </summary>
        public double GazeOnPeers12
        {
            get => GraphMetrics.Get(this.ForwardMetrics, GraphMetricNames.GazeOnPeers);
            set => (this.ForwardMetrics ??= new Dictionary<string, double>())[GraphMetricNames.GazeOnPeers] = value;
        }

        /// <summary>
        /// Gets or sets the looks of <see cref="Second"/> at <see cref="First"/>
        /// (<see cref="GraphMetricNames.GazeOnPeers"/>); 0 when they are not computed.
        /// </summary>
        public double GazeOnPeers21
        {
            get => GraphMetrics.Get(this.BackwardMetrics, GraphMetricNames.GazeOnPeers);
            set => (this.BackwardMetrics ??= new Dictionary<string, double>())[GraphMetricNames.GazeOnPeers] = value;
        }

        /// <summary>
        /// Gets or sets the joint visual attention of the pair
        /// (<see cref="GraphMetricNames.JointVisualAttention"/>); 0 when it is not computed.
        /// </summary>
        public double JVAEvent
        {
            get => this.Get(GraphMetricNames.JointVisualAttention);
            set => this.Set(GraphMetricNames.JointVisualAttention, value);
        }

        /// <summary>
        /// Gets or sets the mean duration of the joint visual attention episodes, normalised
        /// (<see cref="GraphMetricNames.JointVisualAttentionIntensity"/>); 0 when it is not computed.
        /// </summary>
        public double JVAIntensity
        {
            get => this.Get(GraphMetricNames.JointVisualAttentionIntensity);
            set => this.Set(GraphMetricNames.JointVisualAttentionIntensity, value);
        }

        /// <summary>
        /// Gets or sets the equality of the speaking times, -1 when nobody spoke
        /// (<see cref="GraphMetricNames.SpeechEquality"/>); 0 when it is not computed.
        /// </summary>
        public double SpeechEquality
        {
            get => this.Get(GraphMetricNames.SpeechEquality);
            set => this.Set(GraphMetricNames.SpeechEquality, value);
        }

        /// <summary>
        /// Gets or sets the equality of the participations in the task
        /// (<see cref="GraphMetricNames.TaskEquality"/>); 0 when it is not computed.
        /// </summary>
        public double TaskEquality
        {
            get => this.Get(GraphMetricNames.TaskEquality);
            set => this.Set(GraphMetricNames.TaskEquality, value);
        }

        /// <summary>The edge of two participants.</summary>
        /// <param name="a">One participant.</param>
        /// <param name="b">The other one.</param>
        /// <returns>An edge without metrics.</returns>
        public static PersonEdge Between(string a, string b)
        {
            bool inOrder = GraphMetrics.CompareIds(a, b) <= 0;
            string first = inOrder ? a : b;
            string second = inOrder ? b : a;
            return new PersonEdge { FromId_1 = second, ToId_1 = first, FromId_2 = first, ToId_2 = second };
        }

        /// <summary>Says whether the edge is the one of two participants.</summary>
        /// <param name="a">One participant.</param>
        /// <param name="b">The other one.</param>
        /// <returns>True when it is, in either order.</returns>
        public bool Links(string a, string b)
        {
            (string first, string second) = this.Ends();
            return (first == a && second == b) || (first == b && second == a);
        }

        /// <summary>Says whether a metric is computed for this pair.</summary>
        /// <param name="metric">Name of the metric.</param>
        /// <returns>True when the edge holds a value for it.</returns>
        public bool Has(string metric) => GraphMetrics.Has(this.Metrics, metric);

        /// <summary>Says whether a metric with a direction is computed for this pair, in either direction.</summary>
        /// <param name="metric">Name of the metric.</param>
        /// <returns>True when the edge holds a value for it.</returns>
        public bool HasDirected(string metric) => GraphMetrics.Has(this.ForwardMetrics, metric) || GraphMetrics.Has(this.BackwardMetrics, metric);

        /// <summary>Reads a metric.</summary>
        /// <param name="metric">Name of the metric.</param>
        /// <param name="absent">What to return when the metric is not computed.</param>
        /// <returns>The value, or <paramref name="absent"/>.</returns>
        public double Get(string metric, double absent = 0) => GraphMetrics.Get(this.Metrics, metric, absent);

        /// <summary>Gives a value to a metric.</summary>
        /// <param name="metric">Name of the metric.</param>
        /// <param name="value">The value.</param>
        public void Set(string metric, double value) => (this.Metrics ??= new Dictionary<string, double>())[metric] = value;

        private (string First, string Second) Ends()
        {
            List<string> ids = new[] { this.FromId_1, this.ToId_1, this.FromId_2, this.ToId_2 }
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();
            ids.Sort(GraphMetrics.CompareIds);
            return ids.Count >= 2 ? (ids[0], ids[1]) : (ids.Count == 1 ? (ids[0], ids[0]) : (string.Empty, string.Empty));
        }
    }
}
