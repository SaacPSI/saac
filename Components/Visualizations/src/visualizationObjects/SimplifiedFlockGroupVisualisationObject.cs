// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Visualizations
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using Win3D = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a visualization object for simplified flock group.
    /// </summary>
    [VisualizationObject("Simplified Flock Group")]
    public class SimplifiedFlockGroupVisualizationObject : ModelVisual3DValueVisualizationObject<SAAC.Groups.SimplifiedFlockGroup>
    {
        private double billboardHeightCm = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimplifiedFlockGroupVisualizationObject"/> class.
        /// </summary>
        public SimplifiedFlockGroupVisualizationObject()
        {
            this.GroupVolume = new Rect3DVisualizationObject();
            this.GroupVolume.RegisterChildPropertyChangedNotifications(this, nameof(this.GroupVolume));

            this.GroupDirection = new Ray3DVisualizationObject() { Visible = false };
            this.GroupDirection.RegisterChildPropertyChangedNotifications(this, nameof(this.GroupDirection));

            this.Billboard = new BillboardTextVisualizationObject() { Visible = false };
            this.Billboard.RegisterChildPropertyChangedNotifications(this, nameof(this.Billboard));

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
        /// Gets the billboard visualization object.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [PropertyOrder(2)]
        [DisplayName("Billboard")]
        [Description("The billboard properties.")]
        public BillboardTextVisualizationObject Billboard { get; private set; }

        /// <summary>
        /// Gets the group volume 3D rect parameters.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [PropertyOrder(3)]
        [DisplayName("Volume")]
        [Description("The group's volume properties.")]
        public Rect3DVisualizationObject GroupVolume { get; private set; }

        /// <summary>
        /// Gets the group direction 3D ray parameters.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [PropertyOrder(4)]
        [DisplayName("Direction")]
        [Description("The group's direction properties.")]
        public Ray3DVisualizationObject GroupDirection { get; private set; }

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
            this.UpdateVolume();
            this.UpdateDirection();
            this.UpdateBillboard();
        }

        private void UpdateVolume()
        {
            if (this.CurrentData != null)
            {
                Rect3D? rect = new Rect3D(this.CurrentData.Area.Center.X, this.CurrentData.Area.Center.Y, 0.0,
                                        this.CurrentData.Area.Circumference, this.CurrentData.Area.Circumference, 100.0);
                this.GroupVolume.SetCurrentValue(this.SynthesizeMessage(rect));
            }
        }

        private void UpdateDirection()
        {
            if (this.CurrentData != null)
            {
                MathNet.Spatial.Euclidean.Ray3D? ray = new MathNet.Spatial.Euclidean.Ray3D(
                    new MathNet.Spatial.Euclidean.Point3D(this.CurrentData.Area.Center.X, this.CurrentData.Area.Center.Y, 0.0),
                    new MathNet.Spatial.Euclidean.Vector3D(this.CurrentData.Direction.X, this.CurrentData.Direction.Y, 0.0));
                this.GroupDirection.SetCurrentValue(this.SynthesizeMessage(ray));
            }
        }

        private void UpdateBillboard()
        {
            if (this.CurrentData != null)
            {
                var origin = this.CurrentData.Area.Center;
                var pos = new Win3D.Point3D(origin.X, origin.Y, this.BillboardHeightCm);
                this.Billboard.SetCurrentValue(this.SynthesizeMessage(Tuple.Create(pos, $"Group {this.CurrentData.Id}")));
            }
        }

        private void UpdateVisibility()
        {
            bool childrenVisible = this.Visible && this.CurrentData != default;

            this.UpdateChildVisibility(this.GroupVolume.ModelVisual3D, childrenVisible);
            this.UpdateChildVisibility(this.GroupDirection.ModelVisual3D, childrenVisible);
            this.UpdateChildVisibility(this.Billboard.ModelVisual3D, childrenVisible);
        }
    }
}
