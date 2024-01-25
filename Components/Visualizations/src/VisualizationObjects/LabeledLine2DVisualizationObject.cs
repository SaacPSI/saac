using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;
using Microsoft.Psi.Visualization.Helpers;
using Microsoft.Psi.Visualization.VisualizationObjects;

namespace SAAC.Visualizations
{
    /// <summary>
    /// Implements a scatter rectangle visualization object.
    /// </summary>
    [VisualizationObject("Labeled Line2D")]
    public class LabeledLine2DVisualizationObject : XYValueVisualizationObject<Tuple<System.Drawing.Pen, string, string>>
    {
        /// <summary>
        /// The color of the line to draw.
        /// </summary>
        private Color color = Colors.DodgerBlue;

        /// <summary>
        /// Stroke thickness to draw data stream with.
        /// </summary>
        private double lineWidth = 1;

        /// <summary>
        /// Whether to show the label.
        /// </summary>
        private bool showLabel = true;

        /// <summary>
        /// Gets or sets the line color.
        /// </summary>
        [DataMember]
        [DisplayName("Color")]
        [Description("The line color.")]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }

        /// <summary>
        /// Gets or sets the line width.
        /// </summary>
        [DataMember]
        [DisplayName("Line Width")]
        [Description("The width of the line in pixels.")]
        public double LineWidth
        {
            get { return this.lineWidth; }
            set { this.Set(nameof(this.LineWidth), ref this.lineWidth, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the label.
        /// </summary>
        [DataMember]
        [DisplayName("Show Label")]
        [Description("Indicates whether to show the label.")]
        public bool ShowLabel
        {
            get { return this.showLabel; }
            set { this.Set(nameof(this.ShowLabel), ref this.showLabel, value); }
        }

        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(LabeledLine2DVisualizationObject));
    }
}
