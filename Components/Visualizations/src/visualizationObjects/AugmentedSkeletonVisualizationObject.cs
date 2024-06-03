using Microsoft.Psi.Visualization.VisualizationObjects;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace SAAC.Visualizations
{
    public class AugmentedSkeletonVisualizationObject<TJoint> : SkeletonVisualizationObject<TJoint>
    {
        public AugmentedSkeletonVisualizationObject(Func<TJoint, bool> nodeVisibilityFunc = null, Func<TJoint, Brush> nodeFillFunc = null, Func<(TJoint, TJoint), bool> edgeVisibilityFunc = null, Func<(TJoint, TJoint), Brush> edgeFillFunc = null)
            : base(nodeVisibilityFunc, nodeFillFunc, edgeVisibilityFunc, edgeFillFunc)
        {
        }

        /// <summary>
        /// Gets or sets the basic display of confidence.
        /// </summary>
        [DataMember]
        [PropertyOrder(5)]
        [DisplayName("Display confidence")]
        [Description("Display confidence level to joint.")]
        public bool DisplayConfidence { get; set; } = true;
    }
}
