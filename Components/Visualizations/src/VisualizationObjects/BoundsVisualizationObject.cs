// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Visualizations
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements a visualization object for axis-aligned bounding boxes (label + center + size).
    /// All received bounds are accumulated and kept visible regardless of cursor position.
    /// Expects a stream of (string label, Vector3 center, Vector3 size).
    /// </summary>
    [VisualizationObject("Bounds")]
    public class BoundsVisualizationObject : ModelVisual3DValueVisualizationObject<(string, System.Numerics.Vector3, System.Numerics.Vector3)>
    {
        private readonly LinesVisual3D linesVisual = new LinesVisual3D();
        private readonly Dictionary<string, BillboardTextVisualizationObject> billboards = new Dictionary<string, BillboardTextVisualizationObject>();
        private readonly Dictionary<string, (System.Numerics.Vector3 Center, System.Numerics.Vector3 Size)> accumulatedBounds = new Dictionary<string, (System.Numerics.Vector3, System.Numerics.Vector3)>();

        private Color color = Colors.Cyan;
        private double thickness = 2.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundsVisualizationObject"/> class.
        /// </summary>
        public BoundsVisualizationObject()
            : base()
        {
            Debug.WriteLine("[BoundsViz] Constructor called");
            this.UpdateLineProperties();
            Debug.WriteLine("[BoundsViz] Constructor done");
        }

        /// <summary>
        /// Gets or sets the wireframe color.
        /// </summary>
        [DataMember]
        [PropertyOrder(1)]
        [Description("The color of the box wireframes.")]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }

        /// <summary>
        /// Gets or sets the edge thickness in screen pixels.
        /// </summary>
        [DataMember]
        [PropertyOrder(2)]
        [DisplayName("Thickness (px)")]
        [Description("The thickness of the box edges in screen pixels.")]
        public double Thickness
        {
            get { return this.thickness; }
            set { this.Set(nameof(this.Thickness), ref this.thickness, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the label above each box.
        /// </summary>
        [DataMember]
        [PropertyOrder(3)]
        [DisplayName("Show Labels")]
        [Description("Show the object name above each box.")]
        public bool ShowLabels { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to reverse Y and Z axes (Unity to PSI coordinate convention).
        /// </summary>
        [DataMember]
        [PropertyOrder(4)]
        [DisplayName("ReverseYZ")]
        [Description("Reverse Y & Z axes.")]
        public bool ReverseYZ { get; set; }

        /// <inheritdoc/>
        public override void UpdateVisual3D()
        {
            Debug.WriteLine($"[BoundsViz] UpdateVisual3D — Item1={this.CurrentData.Item1 ?? "<null>"}, accumulated={this.accumulatedBounds.Count}, Visible={this.Visible}");

            if (this.CurrentData.Item1 != null)
            {
                var (label, center, size) = this.CurrentData;

                bool isNew = !this.accumulatedBounds.ContainsKey(label);
                this.accumulatedBounds[label] = (center, size);

                if (isNew)
                {
                    var bb = new BillboardTextVisualizationObject() { Visible = true };
                    this.billboards[label] = bb;
                    Debug.WriteLine($"[BoundsViz] New bound accumulated: '{label}' — total={this.accumulatedBounds.Count}");
                }

                this.RebuildAllBoxes();
                this.UpdateAllBillboards();
            }

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Color) || propertyName == nameof(this.Thickness))
            {
                this.UpdateLineProperties();
            }
            else if (propertyName == nameof(this.Visible) || propertyName == nameof(this.ShowLabels))
            {
                this.UpdateVisibility();
            }
        }

        private Point3D ToPoint3D(System.Numerics.Vector3 v)
            => new Point3D(v.X, this.ReverseYZ ? v.Z : v.Y, this.ReverseYZ ? v.Y : v.Z);

        private void RebuildAllBoxes()
        {
            var points = new Point3DCollection(this.accumulatedBounds.Count * 24);
            foreach (var entry in this.accumulatedBounds.Values)
            {
                this.AppendBoxEdges(points, entry.Center, entry.Size);
            }

            this.linesVisual.Points = points;
            Debug.WriteLine($"[BoundsViz] RebuildAllBoxes — {this.accumulatedBounds.Count} boxes, {points.Count} points");
        }

        private void AppendBoxEdges(Point3DCollection points, System.Numerics.Vector3 center, System.Numerics.Vector3 size)
        {
            var half = size / 2f;

            var c = new System.Numerics.Vector3[]
            {
                center + new System.Numerics.Vector3(-half.X, -half.Y, -half.Z), // 0
                center + new System.Numerics.Vector3(+half.X, -half.Y, -half.Z), // 1
                center + new System.Numerics.Vector3(+half.X, +half.Y, -half.Z), // 2
                center + new System.Numerics.Vector3(-half.X, +half.Y, -half.Z), // 3
                center + new System.Numerics.Vector3(-half.X, -half.Y, +half.Z), // 4
                center + new System.Numerics.Vector3(+half.X, -half.Y, +half.Z), // 5
                center + new System.Numerics.Vector3(+half.X, +half.Y, +half.Z), // 6
                center + new System.Numerics.Vector3(-half.X, +half.Y, +half.Z), // 7
            };

            var edges = new (int A, int B)[]
            {
                (0, 1), (1, 2), (2, 3), (3, 0),
                (4, 5), (5, 6), (6, 7), (7, 4),
                (0, 4), (1, 5), (2, 6), (3, 7),
            };

            foreach (var (a, b) in edges)
            {
                points.Add(this.ToPoint3D(c[a]));
                points.Add(this.ToPoint3D(c[b]));
            }
        }

        private void UpdateAllBillboards()
        {
            foreach (var entry in this.accumulatedBounds)
            {
                string label = entry.Key;
                var center = entry.Value.Center;
                var size = entry.Value.Size;
                if (this.billboards.TryGetValue(label, out var bb))
                {
                    var half = size / 2f;
                    var topCenter = center + new System.Numerics.Vector3(0f, this.ReverseYZ ? half.Y : 0f, this.ReverseYZ ? 0f : half.Z);
                    bb.SetCurrentValue(this.SynthesizeMessage(Tuple.Create(this.ToPoint3D(topCenter), label)));
                }
            }
        }

        private void UpdateVisibility()
        {
            bool hasData = this.Visible && this.accumulatedBounds.Count > 0;
            this.UpdateChildVisibility(this.linesVisual, hasData);
            foreach (var entry in this.billboards)
            {
                this.UpdateChildVisibility(entry.Value.ModelVisual3D, hasData && this.ShowLabels);
            }

            Debug.WriteLine($"[BoundsViz] UpdateVisibility — hasData={hasData}, boxes={this.accumulatedBounds.Count}, billboards={this.billboards.Count}");
        }

        private void UpdateLineProperties()
        {
            this.linesVisual.Color = this.color;
            this.linesVisual.Thickness = this.thickness;
        }
    }
}
