using Microsoft.Psi.Visualization.VisualizationObjects;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace SAAC.Visualizations
{ 
    /// <summary>
    /// Implements a visualization object for a list of simplifed bodies.
    /// </summary>
    [VisualizationObject("Simplified Bodies")]
    public class SimplifiedBodyListVisualizationObject : ModelVisual3DListVisualizationObject<SimplifiedBodyVisualizationObject, SAAC.Bodies.SimplifiedBody>
    {
    }
}