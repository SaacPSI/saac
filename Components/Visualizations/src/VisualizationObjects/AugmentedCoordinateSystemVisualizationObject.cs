using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi.Visualization.VisualizationObjects;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Win3D = System.Windows.Media.Media3D;

namespace SAAC.VisualizationObjects
{
    public class AugmentedCoordinateSystemVisualizationObject : CoordinateSystemVisualizationObject
    {
        private double billboardHeightCm = 100;

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
        /// Gets the billboard visualization object for the body.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [PropertyOrder(2)]
        [DisplayName("Billboard")]
        [Description("The billboard properties.")]
        public BillboardTextVisualizationObject Billboard { get; private set; }


        /// <summary>
        /// Gets the skeleton visualization object for the body.
        /// </summary>
        [DataMember]
        [PropertyOrder(3)]
        [DisplayName("ReverseYZ")]
        [Description("Reverse Y & Z axes.")]
        public bool ReverseYZ { get; set; }

        private void UpdateBillboard()
        {
            if (this.CurrentData != null)
            {
                var origin = this.CurrentData.Origin;
                var pos = new Win3D.Point3D(origin.X, ReverseYZ ? origin.Z : origin.Y, (ReverseYZ ? origin.Y : origin.Z) + (this.BillboardHeightCm / 100.0));
                this.Billboard.SetCurrentValue(this.SynthesizeMessage(Tuple.Create(pos, $"{SourceStreamName}")));
            }
        }
    }
}
