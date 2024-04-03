using Microsoft.Psi.AzureKinect;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Psi.Visualization.DataTypes;
using Microsoft.Psi.Visualization.VisualizationObjects;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Win3D = System.Windows.Media.Media3D;

namespace SAAC.Visualizations
{
    /// <summary>
    /// Implements a visualization object for simplified bodies.
    /// </summary>
    [VisualizationObject("Simplified Body")]
    public class SimplifiedBodyVisualizationObject : ModelVisual3DValueVisualizationObject<SAAC.Bodies.SimplifiedBody>
    {
        private static readonly Dictionary<(JointId ChildJoint, JointId ParentJoint), bool> AzureKinectBodyGraph = AzureKinectBody.Bones.ToDictionary(j => j, j => true);

        private double billboardHeightCm = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimplifiedBodyVisualizationObject"/> class.
        /// </summary>
        public SimplifiedBodyVisualizationObject()
        {
            this.Skeleton = new AugmentedSkeletonVisualizationObject<JointId>(
                nodeVisibilityFunc:
                    jointType =>
                    {
                        var jointState = this.CurrentData.Joints[jointType].Item1;
                        var isTracked = jointState == JointConfidenceLevel.High || jointState == JointConfidenceLevel.Medium;
                        return (jointState != JointConfidenceLevel.None || this.Skeleton.DisplayConfidence) && (isTracked || this.Skeleton.InferredJointsOpacity > 0);
                    },
                nodeFillFunc:
                    jointType =>
                    {
                        var jointState = this.CurrentData.Joints[jointType].Item1;
                        var isTracked = jointState == JointConfidenceLevel.High || jointState == JointConfidenceLevel.Medium;
                        if (this.Skeleton.DisplayConfidence)
                        {
                            switch(jointState) 
                            {
                                case JointConfidenceLevel.High:
                                case JointConfidenceLevel.Medium:
                                    return new SolidColorBrush(Color.FromRgb(0, 255, 0));
                                case JointConfidenceLevel.Low:
                                    return new SolidColorBrush(Color.FromRgb(255, 255, 0));
                                case JointConfidenceLevel.None:
                                    return new SolidColorBrush(Color.FromRgb(255, 0, 0));
                            }
                            return new SolidColorBrush(this.Skeleton.NodeColor);
                        }
                        else
                        {
                            return isTracked ?
                                new SolidColorBrush(this.Skeleton.NodeColor) :
                                new SolidColorBrush(
                                    Color.FromArgb(
                                        (byte)(Math.Max(0, Math.Min(100, this.Skeleton.InferredJointsOpacity)) * 2.55),
                                        this.Skeleton.NodeColor.R,
                                        this.Skeleton.NodeColor.G,
                                        this.Skeleton.NodeColor.B));
                        }
                    },
                edgeVisibilityFunc:
                    bone =>
                    {
                        var parentState = this.CurrentData.Joints[bone.Item1].Item1;
                        var childState = this.CurrentData.Joints[bone.Item2].Item1;
                        var parentIsTracked = parentState == JointConfidenceLevel.High || parentState == JointConfidenceLevel.Medium;
                        var childIsTracked = childState == JointConfidenceLevel.High || childState == JointConfidenceLevel.Medium;
                        var isTracked = parentIsTracked && childIsTracked;
                        return parentState != JointConfidenceLevel.None && childState != JointConfidenceLevel.None && (isTracked || this.Skeleton.InferredJointsOpacity > 0);
                    },
                edgeFillFunc:
                    bone =>
                    {
                        var parentState = this.CurrentData.Joints[bone.Item1].Item1;
                        var childState = this.CurrentData.Joints[bone.Item2].Item1;
                        var parentIsTracked = parentState == JointConfidenceLevel.High || parentState == JointConfidenceLevel.Medium;
                        var childIsTracked = childState == JointConfidenceLevel.High || childState == JointConfidenceLevel.Medium;
                        var isTracked = parentIsTracked && childIsTracked;
                        return isTracked ?
                            new SolidColorBrush(this.Skeleton.NodeColor) :
                            new SolidColorBrush(
                                Color.FromArgb(
                                    (byte)(Math.Max(0, Math.Min(100, this.Skeleton.InferredJointsOpacity)) * 2.55),
                                    this.Skeleton.NodeColor.R,
                                    this.Skeleton.NodeColor.G,
                                    this.Skeleton.NodeColor.B));
                    });

            this.Skeleton.RegisterChildPropertyChangedNotifications(this, nameof(this.Skeleton));

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
        [DisplayName("Skeleton")]
        [Description("The body's skeleton properties.")]
        public AugmentedSkeletonVisualizationObject<JointId> Skeleton { get; set; }

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
            this.UpdateSkeleton();
            this.UpdateBillboard();
        }

        private void UpdateSkeleton()
        {
            if (this.CurrentData != null)
            {
                var points = this.CurrentData.Joints.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Item2.ToPoint3D());
                var graph = new Graph<JointId, MathNet.Spatial.Euclidean.Point3D, bool>(points, AzureKinectBodyGraph);
                this.Skeleton.SetCurrentValue(this.SynthesizeMessage(graph));
            }
        }

        private void UpdateBillboard()
        {
            if (this.CurrentData != null)
            {
                var origin = this.CurrentData.Joints[JointId.Pelvis].Item2;
                var pos = new Win3D.Point3D(origin.X, origin.Y, origin.Z + (this.BillboardHeightCm / 100.0));
                this.Billboard.SetCurrentValue(this.SynthesizeMessage(Tuple.Create(pos, $"Body{this.CurrentData.Id}")));
            }
        }

        private void UpdateVisibility()
        {
            bool childrenVisible = this.Visible && this.CurrentData != default;

            this.UpdateChildVisibility(this.Skeleton.ModelVisual3D, childrenVisible);
            this.UpdateChildVisibility(this.Billboard.ModelVisual3D, childrenVisible);
        }
    }
}
