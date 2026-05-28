// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Visualizations
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object for a list of axis-aligned bounding boxes.
    /// Expects a stream of IEnumerable&lt;(string label, Vector3 center, Vector3 size)&gt;.
    /// </summary>
    [VisualizationObject("Bounds List")]
    public class BoundsListVisualizationObject : ModelVisual3DListVisualizationObject<BoundsVisualizationObject, (string, System.Numerics.Vector3, System.Numerics.Vector3)>
    {
    }
}
