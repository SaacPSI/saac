// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Visualizations
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using SAAC.GlobalHelpers;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements a visualization object for gaze events.
    /// </summary>
    [VisualizationObject("GazeEvent")]
    public class GazeEventVisualisationObject : ModelVisual3DValueVisualizationObject<GazeEvent>
    {
        private double billboardHeightCm = 100;
        private SphereVisual3D sphereVisual;
        private Color color = Colors.White;
        private double radiusCm = 2;
        private double opacity = 100;
        private int sphereDiv = 7;

        /// <summary>
        /// Initializes a new instance of the <see cref="GazeEventVisualisationObject"/> class.
        /// </summary>
        public GazeEventVisualisationObject()
            : base()
        {
            this.sphereVisual = new SphereVisual3D();
            this.Billboard = new BillboardTextVisualizationObject() { Visible = false };
            this.Billboard.RegisterChildPropertyChangedNotifications(this, nameof(this.Billboard));
            this.UpdatePointProperties();
            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the height at which to draw the billboard (cm).
        /// </summary>
        [DataMember]
        [PropertyOrder(1)]
        [DisplayName("Billboard Height (cm)")]
        [Description("Height at which to draw the billboard (cm).")]
        public double BillboardHeightCm
        {
            get { return this.billboardHeightCm; }
            set { this.Set(nameof(this.BillboardHeightCm), ref this.billboardHeightCm, value); }
        }

        /// <summary>
        /// Gets or sets the billboard visualization object for the hand.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [PropertyOrder(2)]
        [DisplayName("Billboard")]
        [Description("The billboard properties.")]
        public BillboardTextVisualizationObject Billboard { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        [DataMember]
        [PropertyOrder(3)]
        [Description("The color of the point(s).")]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }

        /// <summary>
        /// Gets or sets the radius of the point(s) in centimeters.
        /// </summary>
        [DataMember]
        [PropertyOrder(4)]
        [DisplayName("Radius (cm)")]
        [Description("The radius of the point(s) in centimeters.")]
        public double RadiusCm
        {
            get { return this.radiusCm; }
            set { this.Set(nameof(this.RadiusCm), ref this.radiusCm, value); }
        }

        /// <summary>
        /// Gets or sets the opacity of the point(s).
        /// </summary>
        [DataMember]
        [PropertyOrder(5)]
        [Description("The opacity of the point(s).")]
        public double Opacity
        {
            get { return this.opacity; }
            set { this.Set(nameof(this.Opacity), ref this.opacity, value); }
        }

        /// <summary>
        /// Gets or sets the number of divisions to use when rendering the sphere.
        /// </summary>
        [DataMember]
        [PropertyOrder(6)]
        [Description("Number of divisions to use when rendering each point as a sphere (minimum value is 3).")]
        public int SphereDivisions
        {
            get { return this.sphereDiv; }
            set { this.Set(nameof(this.SphereDivisions), ref this.sphereDiv, value < 3 ? 3 : value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether reversing Y and Z axes.
        /// </summary>
        [DataMember]
        [PropertyOrder(7)]
        [DisplayName("ReverseYZ")]
        [Description("Reverse Y & Z axes.")]
        public bool ReverseYZ { get; set; }

        /// <inheritdoc/>
        public override void UpdateVisual3D()
        {
            if (this.CurrentData != null)
            {
                this.UpdateVisuals();
            }

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Color) ||
               propertyName == nameof(this.Opacity) ||
               propertyName == nameof(this.RadiusCm) ||
               propertyName == nameof(this.SphereDivisions))
            {
                this.UpdatePointProperties();
            }

            if (propertyName == nameof(this.BillboardHeightCm))
            {
                this.UpdateBillboard();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisuals()
        {
            this.UpdateVisibility();
            this.UpdatePosition();
            this.UpdateBillboard();
        }

        private void UpdatePosition()
        {
            if (this.CurrentData != null)
            {
                this.sphereVisual.Transform = new TranslateTransform3D(this.CurrentData.Position.X, this.ReverseYZ ? this.CurrentData.Position.Z : this.CurrentData.Position.Y, this.ReverseYZ ? this.CurrentData.Position.Y : this.CurrentData.Position.Z);
            }
        }

        private void UpdateBillboard()
        {
            if (this.CurrentData != null)
            {
                var origin = this.CurrentData.Position;
                var pos = new Point3D(origin.X, origin.Y, origin.Z + (this.BillboardHeightCm / 100.0));
                this.Billboard.SetCurrentValue(this.SynthesizeMessage(Tuple.Create(pos, $"User {this.CurrentData.UserID} look at {this.CurrentData.ObjectID}")));
            }
        }

        private void UpdateVisibility()
        {
            bool childrenVisible = this.Visible && this.CurrentData != default && this.CurrentData.IsGazed;

            this.UpdateChildVisibility(this.sphereVisual, childrenVisible);
            this.UpdateChildVisibility(this.Billboard.ModelVisual3D, childrenVisible);
        }

        private void UpdatePointProperties()
        {
            double opacity = Math.Max(0, Math.Min(100, this.Opacity));
            var alphaColor = Color.FromArgb(
                (byte)(opacity * 2.55),
                this.Color.R,
                this.Color.G,
                this.Color.B);

            this.sphereVisual.Fill = new SolidColorBrush(alphaColor);
            this.sphereVisual.Radius = this.RadiusCm * 0.01;
            this.sphereVisual.PhiDiv = this.sphereDiv;
            this.sphereVisual.ThetaDiv = this.sphereDiv;
        }
    }
}

