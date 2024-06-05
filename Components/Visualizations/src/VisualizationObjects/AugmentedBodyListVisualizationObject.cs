using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Psi.AzureKinect;
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
    [VisualizationObject("Augmented Bodies")]
    public class AugmentedBodyListVisualizationObject : ModelVisual3DListVisualizationObject<AugmentedBodyVisualizationObject, AzureKinectBody>
    {
    }
}