// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Visualizations
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object for a dictionnary of simplified flock group.
    /// </summary>
    [VisualizationObject("Simplified Flock Group")]
    public class SimplifiedFlockGroupDictionnaryVisualisationObject : ModelVisual3DDictionaryVisualizationObject<SimplifiedFlockGroupVisualizationObject, uint, SAAC.Groups.SimplifiedFlockGroup>
    {
    }
}
