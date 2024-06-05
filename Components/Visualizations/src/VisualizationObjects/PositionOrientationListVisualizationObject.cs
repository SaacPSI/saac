using Microsoft.Psi.Visualization.VisualizationObjects;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace SAAC.Visualizations
{
    /// <summary>
    /// Implements a visualization object for a list of Position Orientation vector3.
    /// </summary>
    [VisualizationObject("Position Orientation")]
    public class PositionOrientationListVisualizationObject : ModelVisual3DListVisualizationObject<PositionOrientationVisualizationObject, Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>
    {
    }
}