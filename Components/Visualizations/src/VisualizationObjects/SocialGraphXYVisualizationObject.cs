using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
using Microsoft.Psi.Visualization.Helpers;
using Microsoft.Psi.Visualization.VisualizationObjects;
using Microsoft.Psi.Visualization.Views.Visuals2D;

namespace SAAC.Visualizations
{
    /// <summary>
    /// Sociogramme XY : snapshot multi-nœuds/arêtes rendu dans un XY panel.
    /// </summary>
    [VisualizationObject("SocialGraph 2D")]
    public class SocialGraphXYVisualizationObject : XYValueVisualizationObject<IndividualPairCharacteristics>
    {
        // ==== Réglages nœuds ====
        private bool showLabels = true;
        private double nodeMinRadius = 10;
        private double nodeMaxRadius = 20;

        // ==== Réglages arêtes (génériques) ====
        private double edgeMinThickness = 1;
        private double edgeMaxThickness = 6;

        // ==== Affichages par variable (commutateurs) ====
        // Nodes
        private bool showNodesSpeakingTime = true;

        // Edges
        private bool showEdgesGaze = true;
        private bool showEdgesSynchrony = true;
        private bool showEdgesJVA = true;
        private bool showEdgesSpeechEquality = true;

        // Segments/capsules JVA additionnels éventuels
        private bool showJVASegment = true;

        // ==== Mise à l’échelle monde↔écran (XY panel) ====
        // 1 mètre = 100 px par défaut (0.01 m/px)
        private double metersPerPixel = 0.008;

        // ==== “Distance compressée & deadband” (utilisé par la vue) ====
        private double displayMinDistanceMeters = 0.60;
        private double distanceGainK = 0.90;
        private double deadbandMeters = 0.04;

        // -------------------- Propriétés publiques --------------------

        [DataMember]
        public bool ShowLabels
        {
            get => this.showLabels;
            set => this.Set(nameof(this.ShowLabels), ref this.showLabels, value);
        }

        [DataMember]
        public double NodeMinRadius
        {
            get => this.nodeMinRadius;
            set => this.Set(nameof(this.NodeMinRadius), ref this.nodeMinRadius, value);
        }

        [DataMember]
        public double NodeMaxRadius
        {
            get => this.nodeMaxRadius;
            set => this.Set(nameof(this.NodeMaxRadius), ref this.nodeMaxRadius, value);
        }

        [DataMember]
        public double EdgeMinThickness
        {
            get => this.edgeMinThickness;
            set => this.Set(nameof(this.EdgeMinThickness), ref this.edgeMinThickness, value);
        }

        [DataMember]
        public double EdgeMaxThickness
        {
            get => this.edgeMaxThickness;
            set => this.Set(nameof(this.EdgeMaxThickness), ref this.edgeMaxThickness, value);
        }

        // ---- Commutateurs d’affichage (Nodes) ----

        /// <summary>Afficher la couche "node size ~ SpeakingTime".</summary>
        [DataMember]
        public bool ShowNodesSpeakingTime
        {
            get => this.showNodesSpeakingTime;
            set => this.Set(nameof(this.ShowNodesSpeakingTime), ref this.showNodesSpeakingTime, value);
        }

        // ---- Commutateurs d’affichage (Edges) ----

        /// <summary>Afficher l’arête "Gaze on peers".</summary>
        [DataMember]
        public bool ShowEdgesGaze
        {
            get => this.showEdgesGaze;
            set => this.Set(nameof(this.ShowEdgesGaze), ref this.showEdgesGaze, value);
        }

        /// <summary>Afficher l’arête "Synchrony".</summary>
        [DataMember]
        public bool ShowEdgesSynchrony
        {
            get => this.showEdgesSynchrony;
            set => this.Set(nameof(this.ShowEdgesSynchrony), ref this.showEdgesSynchrony, value);
        }

        /// <summary>Afficher l’arête "JVA".</summary>
        [DataMember]
        public bool ShowEdgesJVA
        {
            get => this.showEdgesJVA;
            set => this.Set(nameof(this.ShowEdgesJVA), ref this.showEdgesJVA, value);
        }

        /// <summary>Afficher l’arête "Speech equality".</summary>
        [DataMember]
        public bool ShowEdgesSpeechEquality
        {
            get => this.showEdgesSpeechEquality;
            set => this.Set(nameof(this.ShowEdgesSpeechEquality), ref this.showEdgesSpeechEquality, value);
        }

        // ---- Monde ↔ écran ----

        /// <summary>Échelle monde↔écran. Exemple : 0.01 → 1 m = 100 px.</summary>
        [DataMember]
        public double MetersPerPixel
        {
            get => this.metersPerPixel;
            set => this.Set(nameof(this.MetersPerPixel), ref this.metersPerPixel, value);
        }

        /// <summary>Distance résiduelle d’affichage pour éviter la superposition.</summary>
        [DataMember]
        public double DisplayMinDistanceMeters
        {
            get => this.displayMinDistanceMeters;
            set => this.Set(nameof(this.DisplayMinDistanceMeters), ref this.displayMinDistanceMeters, value);
        }

        /// <summary>Gain de distance (&lt;=1) pour espacer visuellement.</summary>
        [DataMember]
        public double DistanceGainK
        {
            get => this.distanceGainK;
            set => this.Set(nameof(this.DistanceGainK), ref this.distanceGainK, Clamp(value, 0.1, 1.0));
        }

        /// <summary>Seuil d’insensibilité pour lisser le jitter (m).</summary>
        [DataMember]
        public double DeadbandMeters
        {
            get => this.deadbandMeters;
            set => this.Set(nameof(this.DeadbandMeters), ref this.deadbandMeters, value);
        }

        // -------------------- API utilisée par la vue --------------------

        /// <summary>Rayon (px) du nœud, par défaut en fonction de SpeakingTime. Si la couche est désactivée, retourne le rayon min.</summary>
        public double GetNodeRadius(PersonNode n)
        {
            if (n == null) return this.nodeMinRadius;
            double v = Clamp(n.SpeakingTime, 0.0, 1.0);
            if (!this.showNodesSpeakingTime)
                return this.nodeMinRadius;
            return this.nodeMinRadius + (this.nodeMaxRadius - this.nodeMinRadius) * v;
        }

        /// <summary>
        /// Distance "compressée" pour l’affichage : min + gain * (d - min).
        /// </summary>
        public double CompressDistanceMeters(double dRealMeters)
            => this.displayMinDistanceMeters + this.distanceGainK * System.Math.Max(0, dRealMeters - this.displayMinDistanceMeters);

        // ---- Méthodes utilitaires pour pilotage d’affichage dans la vue ----
        public bool ShouldShowNodeSpeakingTime() => this.showNodesSpeakingTime;

        public bool ShouldShowEdgeGaze() => this.showEdgesGaze;
        public bool ShouldShowEdgeSynchrony() => this.showEdgesSynchrony;
        public bool ShouldShowEdgeJVA() => this.showEdgesJVA;
        public bool ShouldShowEdgeSpeechEquality() => this.showEdgesSpeechEquality;

        // -------------------- Utils --------------------

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate
            => XamlHelper.CreateTemplate(this.GetType(), typeof(SocialGraphXYVisualizationObjectViews));
    }
}
