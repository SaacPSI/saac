// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Visualizations
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements an augmented skeleton visualization object with confidence display.
    /// </summary>
    /// <typeparam name="TJoint">The joint type.</typeparam>
    public class AugmentedSkeletonVisualizationObject<TJoint> : SkeletonVisualizationObject<TJoint>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AugmentedSkeletonVisualizationObject{TJoint}"/> class.
        /// </summary>
        /// <param name="nodeVisibilityFunc">The node visibility function.</param>
        /// <param name="nodeFillFunc">The node fill function.</param>
        /// <param name="edgeVisibilityFunc">The edge visibility function.</param>
        /// <param name="edgeFillFunc">The edge fill function.</param>
        public AugmentedSkeletonVisualizationObject(Func<TJoint, bool> nodeVisibilityFunc = null, Func<TJoint, Brush> nodeFillFunc = null, Func<(TJoint, TJoint), bool> edgeVisibilityFunc = null, Func<(TJoint, TJoint), Brush> edgeFillFunc = null)
            : base(nodeVisibilityFunc, nodeFillFunc, edgeVisibilityFunc, edgeFillFunc)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets the basic display of confidence.
        /// </summary>
        [DataMember]
        [PropertyOrder(5)]
        [DisplayName("Display confidence")]
        [Description("Display confidence level to joint.")]
        public bool DisplayConfidence { get; set; } = true;
    }
}
