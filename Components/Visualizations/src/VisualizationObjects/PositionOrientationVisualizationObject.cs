using Microsoft.Psi.Visualization.VisualizationObjects;
using System.ComponentModel;
using System.Runtime.Serialization;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Win3D = System.Windows.Media.Media3D;
using System.Numerics;
using MathNet.Spatial.Euclidean;

namespace SAAC.Visualizations
{
    /// <summary>
    /// Implements a visualization object for simplified bodies.
    /// </summary>
    [VisualizationObject("Position Orientation")]
    public class PositionOrientationVisualizationObject : ModelVisual3DValueVisualizationObject<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>
    {

        private double billboardHeightCm = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionOrientationVisualizationObject"/> class.
        /// </summary>
        public PositionOrientationVisualizationObject()
        {
            System = new CoordinateSystemVisualizationObject();
            this.System.RegisterChildPropertyChangedNotifications(this, nameof(this.System));

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
        /// Gets the billboard visualization object for the body.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [PropertyOrder(2)]
        [DisplayName("Billboard")]
        [Description("The billboard properties.")]
        public BillboardTextVisualizationObject Billboard { get; set; }

        /// <summary>
        /// Gets the skeleton visualization object for the body.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [PropertyOrder(3)]
        [DisplayName("CoordinateSystem")]
        [Description("The representation object.")]
        public CoordinateSystemVisualizationObject System { get; set; }


        /// <summary>
        /// Gets the skeleton visualization object for the body.
        /// </summary>
        [DataMember]
        [PropertyOrder(4)]
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
            this.UpdateSystem();
            this.UpdateBillboard();
        }

        private void UpdateSystem()
        {
            if (this.CurrentData != null)
            {
                MathNet.Spatial.Euclidean.Point3D origin = new MathNet.Spatial.Euclidean.Point3D(this.CurrentData.Item1.X, ReverseYZ ? this.CurrentData.Item1.Z : this.CurrentData.Item1.Y, ReverseYZ ? this.CurrentData.Item1.Y : this.CurrentData.Item1.Z);
                MathNet.Spatial.Euclidean.CoordinateSystem rot = MathNet.Spatial.Euclidean.CoordinateSystem.Rotation(MathNet.Spatial.Units.Angle.FromDegrees(this.CurrentData.Item2.X), MathNet.Spatial.Units.Angle.FromDegrees(ReverseYZ ? this.CurrentData.Item2.Z : this.CurrentData.Item2.Y), MathNet.Spatial.Units.Angle.FromDegrees(ReverseYZ ? this.CurrentData.Item2.Y : this.CurrentData.Item2.Z));
                MathNet.Spatial.Euclidean.CoordinateSystem newValue = new MathNet.Spatial.Euclidean.CoordinateSystem(origin, rot.XAxis, rot.YAxis, rot.ZAxis);
                this.System.SetCurrentValue(this.SynthesizeMessage(newValue));
            }
        }

        private void UpdateBillboard()
        {
            if (this.CurrentData != null)
            {
                var origin = this.CurrentData.Item1;
                var pos = new Win3D.Point3D(origin.X, ReverseYZ ? origin.Z : origin.Y, (ReverseYZ ? origin.Y : origin.Z) + (this.BillboardHeightCm / 100.0));
                this.Billboard.SetCurrentValue(this.SynthesizeMessage(Tuple.Create(pos, $"{SourceStreamName}")));
            }
        }

        private void UpdateVisibility()
        {
            bool childrenVisible = this.Visible && this.CurrentData != default;

            this.UpdateChildVisibility(this.System.ModelVisual3D, childrenVisible);
            this.UpdateChildVisibility(this.Billboard.ModelVisual3D, childrenVisible);
        }
    }
}
