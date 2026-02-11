// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using Win3D = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements an augmented coordinate system visualization object.
    /// </summary>
    public class AugmentedCoordinateSystemVisualizationObject : CoordinateSystemVisualizationObject
    {
        private double billboardHeightCm = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="AugmentedCoordinateSystemVisualizationObject"/> class.
        /// </summary>
        public AugmentedCoordinateSystemVisualizationObject()
            : base()
        {
            this.Billboard = new BillboardTextVisualizationObject() { Visible = false };
            this.Billboard.RegisterChildPropertyChangedNotifications(this, nameof(this.Billboard));
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
        /// Gets or sets a value indicating whether the visualtion reverse axes.
        /// </summary>
        [DataMember]
        [PropertyOrder(3)]
        [DisplayName("ReverseYZ")]
        [Description("Reverse Y & Z axes.")]
        public bool ReverseYZ { get; set; }

        /// <inheritdoc/>
        public override void UpdateVisual3D()
        {
            base.UpdateVisual3D();
            this.UpdateBillboard();
        }

        private void UpdateBillboard()
        {
            this.UpdateChildVisibility(this.Billboard.ModelVisual3D, this.Billboard.Visible);
            if (this.CurrentData != null && this.Billboard.Visible)
            {
                var origin = this.CurrentData.Origin;
                var pos = new Win3D.Point3D(origin.X, this.ReverseYZ ? origin.Z : origin.Y, (this.ReverseYZ ? origin.Y : origin.Z) + (this.BillboardHeightCm / 100.0));
                this.Billboard.SetCurrentValue(this.SynthesizeMessage(Tuple.Create(pos, $"{this.SourceStreamName}")));
            }
        }
    }
}
