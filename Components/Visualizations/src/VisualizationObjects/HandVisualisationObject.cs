// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Visualizations
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.DataTypes;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using SAAC.GlobalHelpers;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using Win3D = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a visualization object for skeleton hands.
    /// </summary>
    [VisualizationObject("Hand")]
    public class HandVisualisationObject : ModelVisual3DValueVisualizationObject<Hand>
    {
        private static readonly Dictionary<(Hand.EHandJointID ChildJoint, Hand.EHandJointID ParentJoint), bool> HandGraph = GlobalHelpers.Hand.Bones.ToDictionary(j => j, j => true);

        private double billboardHeightCm = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandVisualisationObject"/> class.
        /// </summary>
        public HandVisualisationObject()
        {
            this.HandSkeleton = new SkeletonVisualizationObject<Hand.EHandJointID>(
                nodeVisibilityFunc:
                    jointType =>
                    {
                        return this.CurrentData.HandJoints.ContainsKey(jointType);
                    },
                nodeFillFunc:
                    jointType =>
                    {
                        return new SolidColorBrush(this.HandSkeleton.NodeColor);
                    },
                edgeVisibilityFunc:
                    bone =>
                    {
                        return this.CurrentData.HandJoints.ContainsKey(bone.Item1) && this.CurrentData.HandJoints.ContainsKey(bone.Item2);
                    },
                edgeFillFunc:
                    bone =>
                    {
                        return new SolidColorBrush(this.HandSkeleton.NodeColor);
                    });

            this.HandSkeleton.RegisterChildPropertyChangedNotifications(this, nameof(this.HandSkeleton));

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
        /// Gets or sets the billboard visualization object for the hand.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [PropertyOrder(2)]
        [DisplayName("Billboard")]
        [Description("The billboard properties.")]
        public BillboardTextVisualizationObject Billboard { get; set; }

        /// <summary>
        /// Gets or sets the skeleton visualization object for the hand.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [PropertyOrder(3)]
        [DisplayName("Skeleton")]
        [Description("The body's skeleton properties.")]
        public SkeletonVisualizationObject<Hand.EHandJointID> HandSkeleton { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether reversing Y and Z axes.
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
            this.UpdateSkeleton();
            this.UpdateBillboard();
        }

        private void UpdateSkeleton()
        {
            if (this.CurrentData != null)
            {
                Dictionary<Hand.EHandJointID, Point3D> points = new Dictionary<Hand.EHandJointID, Point3D>();
                foreach (var joint in this.CurrentData.HandJoints)
                {
                    points.Add(joint.Key, new Point3D(joint.Value.X, this.ReverseYZ ? joint.Value.Z : joint.Value.Y, this.ReverseYZ ? joint.Value.Y : joint.Value.Z));
                }

                var graph = new Graph<Hand.EHandJointID, Point3D, bool>(points, HandGraph);
                this.HandSkeleton.SetCurrentValue(this.SynthesizeMessage(graph));
            }
        }

        private void UpdateBillboard()
        {
            if (this.CurrentData != null)
            {
                var origin = this.CurrentData.RootPosition;
                var pos = new Win3D.Point3D(origin.X, origin.Y, origin.Z + (this.BillboardHeightCm / 100.0));
                this.Billboard.SetCurrentValue(this.SynthesizeMessage(Tuple.Create(pos, $"{this.CurrentData.Type} Hand")));
            }
        }

        private void UpdateVisibility()
        {
            bool childrenVisible = this.Visible && this.CurrentData != default;

            this.UpdateChildVisibility(this.HandSkeleton.ModelVisual3D, childrenVisible);
            this.UpdateChildVisibility(this.Billboard.ModelVisual3D, childrenVisible);
        }
    }
}
