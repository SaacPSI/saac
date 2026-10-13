// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using System.Collections.Generic;

    /// <summary>
    /// What is measured on the group: the participant who does the most of something, and
    /// the scores of the collaboration model. The metrics are kept by name
    /// (<see cref="GraphMetricNames"/>); one that is not computed is absent.
    /// </summary>
    public class PersonGroup
    {
        /// <summary>
        /// Gets or sets the metrics of the group, by name. A metric that is not computed is absent.
        /// </summary>
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Gets or sets the names of the metrics that are scores of the collaboration model, in
        /// the order they are listed. Empty for the default model
        /// (<see cref="GraphMetricNames.DefaultScores"/>); to be filled when the model has
        /// other dimensions.
        /// </summary>
        public List<string> ScoreNames { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the number of the participant who acts most on the task
        /// (<see cref="GraphMetricNames.TaskingMost"/>); 0 when nobody does or when it is not computed.
        /// </summary>
        public double TaskingMost
        {
            get => this.Get(GraphMetricNames.TaskingMost);
            set => this.Set(GraphMetricNames.TaskingMost, value);
        }

        /// <summary>
        /// Gets or sets the number of the participant who speaks most
        /// (<see cref="GraphMetricNames.TalkingMost"/>); 0 when nobody does or when it is not computed.
        /// </summary>
        public double TalkingMost
        {
            get => this.Get(GraphMetricNames.TalkingMost);
            set => this.Set(GraphMetricNames.TalkingMost, value);
        }

        /// <summary>
        /// Gets or sets the number of the participant the others look at most
        /// (<see cref="GraphMetricNames.WatchedMost"/>); 0 when nobody is or when it is not computed.
        /// </summary>
        public double WatchedMost
        {
            get => this.Get(GraphMetricNames.WatchedMost);
            set => this.Set(GraphMetricNames.WatchedMost, value);
        }

        /// <summary>
        /// Gets or sets the number of the participant who leads the visual attention
        /// (<see cref="GraphMetricNames.LeadVisualAttention"/>); 0 when nobody does or when it is not computed.
        /// </summary>
        public double LeadVA
        {
            get => this.Get(GraphMetricNames.LeadVisualAttention);
            set => this.Set(GraphMetricNames.LeadVisualAttention, value);
        }

        /// <summary>Gets or sets the dominance score (<see cref="GraphMetricNames.Dominance"/>); 0 when it is not computed.</summary>
        public double Dominance_score
        {
            get => this.Get(GraphMetricNames.Dominance);
            set => this.Set(GraphMetricNames.Dominance, value);
        }

        /// <summary>Gets or sets the joint attention score (<see cref="GraphMetricNames.JointAttention"/>); 0 when it is not computed.</summary>
        public double Ja_score
        {
            get => this.Get(GraphMetricNames.JointAttention);
            set => this.Set(GraphMetricNames.JointAttention, value);
        }

        /// <summary>Gets or sets the engagement score (<see cref="GraphMetricNames.Engagement"/>); 0 when it is not computed.</summary>
        public double Engagement_score
        {
            get => this.Get(GraphMetricNames.Engagement);
            set => this.Set(GraphMetricNames.Engagement, value);
        }

        /// <summary>Gets or sets the communication score (<see cref="GraphMetricNames.CommunicationProcessManagement"/>); 0 when it is not computed.</summary>
        public double Cpm_score
        {
            get => this.Get(GraphMetricNames.CommunicationProcessManagement);
            set => this.Set(GraphMetricNames.CommunicationProcessManagement, value);
        }

        /// <summary>Gets or sets the spatial score (<see cref="GraphMetricNames.SpatialBehaviour"/>); 0 when it is not computed.</summary>
        public double Spatial_score
        {
            get => this.Get(GraphMetricNames.SpatialBehaviour);
            set => this.Set(GraphMetricNames.SpatialBehaviour, value);
        }

        /// <summary>Gets or sets the mean of the dimension scores (<see cref="GraphMetricNames.SecondDimension"/>); 0 when it is not computed.</summary>
        public double Dimension2_score
        {
            get => this.Get(GraphMetricNames.SecondDimension);
            set => this.Set(GraphMetricNames.SecondDimension, value);
        }

        /// <summary>Gets or sets the collaboration score (<see cref="GraphMetricNames.CollaborationScore"/>); 0 when it is not computed.</summary>
        public double Collaboration_score
        {
            get => this.Get(GraphMetricNames.CollaborationScore);
            set => this.Set(GraphMetricNames.CollaborationScore, value);
        }

        /// <summary>Says whether a metric is computed for the group.</summary>
        /// <param name="metric">Name of the metric.</param>
        /// <returns>True when the group holds a value for it.</returns>
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
