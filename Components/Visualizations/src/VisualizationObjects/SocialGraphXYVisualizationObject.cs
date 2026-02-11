// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Visualizations
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using SAAC.Groups;

    /// <summary>
    /// XY Sociogram: multi-node/edge snapshot rendered in an XY panel.
    /// </summary>
    [VisualizationObject("SocialGraph 2D")]
    public class SocialGraphXYVisualizationObject : XYValueVisualizationObject<IndividualPairCharacteristics>
    {
        private bool showLabels = true;
        private double nodeMinRadius = 10;
        private double nodeMaxRadius = 20;
        private double edgeMinThickness = 1;
        private double edgeMaxThickness = 6;
        private bool showNodesSpeakingTime = true;
        private bool showEdgesGaze = true;
        private bool showEdgesSynchrony = true;
        private bool showEdgesJVA = true;
        private bool showEdgesSpeechEquality = true;
        private bool showJVASegment = true;
        private double metersPerPixel = 0.008;
        private double displayMinDistanceMeters = 0.60;
        private double distanceGainK = 0.90;
        private double deadbandMeters = 0.04;

        /// <summary>
        /// Gets or sets a value indicating whether labels are shown on the visualization.
        /// </summary>
        [DataMember]
        public bool ShowLabels
        {
            get => this.showLabels;
            set => this.Set(nameof(this.ShowLabels), ref this.showLabels, value);
        }

        /// <summary>
        /// Gets or sets the minimum radius for nodes in pixels.
        /// </summary>
        [DataMember]
        public double NodeMinRadius
        {
            get => this.nodeMinRadius;
            set => this.Set(nameof(this.NodeMinRadius), ref this.nodeMinRadius, value);
        }

        /// <summary>
        /// Gets or sets the maximum radius for nodes in pixels.
        /// </summary>
        [DataMember]
        public double NodeMaxRadius
        {
            get => this.nodeMaxRadius;
            set => this.Set(nameof(this.NodeMaxRadius), ref this.nodeMaxRadius, value);
        }

        /// <summary>
        /// Gets or sets the minimum thickness for edges in pixels.
        /// </summary>
        [DataMember]
        public double EdgeMinThickness
        {
            get => this.edgeMinThickness;
            set => this.Set(nameof(this.EdgeMinThickness), ref this.edgeMinThickness, value);
        }

        /// <summary>
        /// Gets or sets the maximum thickness for edges in pixels.
        /// </summary>
        [DataMember]
        public double EdgeMaxThickness
        {
            get => this.edgeMaxThickness;
            set => this.Set(nameof(this.EdgeMaxThickness), ref this.edgeMaxThickness, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether node size reflects speaking time.
        /// </summary>
        [DataMember]
        public bool ShowNodesSpeakingTime
        {
            get => this.showNodesSpeakingTime;
            set => this.Set(nameof(this.ShowNodesSpeakingTime), ref this.showNodesSpeakingTime, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether gaze-on-peers edges are displayed.
        /// </summary>
        [DataMember]
        public bool ShowEdgesGaze
        {
            get => this.showEdgesGaze;
            set => this.Set(nameof(this.ShowEdgesGaze), ref this.showEdgesGaze, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether synchrony edges are displayed.
        /// </summary>
        [DataMember]
        public bool ShowEdgesSynchrony
        {
            get => this.showEdgesSynchrony;
            set => this.Set(nameof(this.ShowEdgesSynchrony), ref this.showEdgesSynchrony, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether JVA (Joint Visual Attention) edges are displayed.
        /// </summary>
        [DataMember]
        public bool ShowEdgesJVA
        {
            get => this.showEdgesJVA;
            set => this.Set(nameof(this.ShowEdgesJVA), ref this.showEdgesJVA, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether speech equality edges are displayed.
        /// </summary>
        [DataMember]
        public bool ShowEdgesSpeechEquality
        {
            get => this.showEdgesSpeechEquality;
            set => this.Set(nameof(this.ShowEdgesSpeechEquality), ref this.showEdgesSpeechEquality, value);
        }

        /// <summary>
        /// Gets or sets the world-to-screen scale in meters per pixel. Example: 0.01 means 1 meter = 100 pixels.
        /// </summary>
        [DataMember]
        public double MetersPerPixel
        {
            get => this.metersPerPixel;
            set => this.Set(nameof(this.MetersPerPixel), ref this.metersPerPixel, value);
        }

        /// <summary>
        /// Gets or sets the minimum display distance in meters to avoid overlap.
        /// </summary>
        [DataMember]
        public double DisplayMinDistanceMeters
        {
            get => this.displayMinDistanceMeters;
            set => this.Set(nameof(this.DisplayMinDistanceMeters), ref this.displayMinDistanceMeters, value);
        }

        /// <summary>
        /// Gets or sets the distance gain factor (less than or equal to 1) for visual spacing.
        /// </summary>
        [DataMember]
        public double DistanceGainK
        {
            get => this.distanceGainK;
            set => this.Set(nameof(this.DistanceGainK), ref this.distanceGainK, Clamp(value, 0.1, 1.0));
        }

        /// <summary>
        /// Gets or sets the deadband threshold in meters to smooth jitter.
        /// </summary>
        [DataMember]
        public double DeadbandMeters
        {
            get => this.deadbandMeters;
            set => this.Set(nameof(this.DeadbandMeters), ref this.deadbandMeters, value);
        }

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate
            => XamlHelper.CreateTemplate(this.GetType(), typeof(SocialGraphXYVisualizationObjectViews));

        /// <summary>
        /// Gets the node radius in pixels based on speaking time. Returns minimum radius if the layer is disabled.
        /// </summary>
        /// <param name="n">The person node.</param>
        /// <returns>The calculated radius in pixels.</returns>
        public double GetNodeRadius(PersonNode n)
        {
            if (n == null)
            {
                return this.nodeMinRadius;
            }

            double v = Clamp(n.SpeakingTime, 0.0, 1.0);
            if (!this.showNodesSpeakingTime)
            {
                return this.nodeMinRadius;
            }

            return this.nodeMinRadius + ((this.nodeMaxRadius - this.nodeMinRadius) * v);
        }

        /// <summary>
        /// Compresses the distance for display: min + gain * (d - min).
        /// </summary>
        /// <param name="dRealMeters">The real distance in meters.</param>
        /// <returns>The compressed display distance in meters.</returns>
        public double CompressDistanceMeters(double dRealMeters)
            => this.displayMinDistanceMeters + (this.distanceGainK * System.Math.Max(0, dRealMeters - this.displayMinDistanceMeters));

        /// <summary>
        /// Determines whether to show node speaking time visualization.
        /// </summary>
        /// <returns>True if speaking time should be shown; otherwise, false.</returns>
        public bool ShouldShowNodeSpeakingTime() => this.showNodesSpeakingTime;

        /// <summary>
        /// Determines whether to show gaze edge visualization.
        /// </summary>
        /// <returns>True if gaze edges should be shown; otherwise, false.</returns>
        public bool ShouldShowEdgeGaze() => this.showEdgesGaze;

        /// <summary>
        /// Determines whether to show synchrony edge visualization.
        /// </summary>
        /// <returns>True if synchrony edges should be shown; otherwise, false.</returns>
        public bool ShouldShowEdgeSynchrony() => this.showEdgesSynchrony;

        /// <summary>
        /// Determines whether to show JVA edge visualization.
        /// </summary>
        /// <returns>True if JVA edges should be shown; otherwise, false.</returns>
        public bool ShouldShowEdgeJVA() => this.showEdgesJVA;

        /// <summary>
        /// Determines whether to show speech equality edge visualization.
        /// </summary>
        /// <returns>True if speech equality edges should be shown; otherwise, false.</returns>
        public bool ShouldShowEdgeSpeechEquality() => this.showEdgesSpeechEquality;

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
