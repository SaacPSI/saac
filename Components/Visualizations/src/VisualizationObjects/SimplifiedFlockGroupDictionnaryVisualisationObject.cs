using Microsoft.Psi.Visualization.VisualizationObjects;

namespace SAAC.Visualizations
{
    /// <summary>
    /// Implements a visualization object for a dictionnary of simplified flock group.
    /// </summary>
    [VisualizationObject("Simplified Flock Group")]
    public class SimplifiedFlockGroupDictionnaryVisualisationObject : ModelVisual3DDictionaryVisualizationObject<SimplifiedFlockGroupVisualizationObject, uint, SAAC.Groups.SimplifiedFlockGroup>
    {
    }
}