// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.BodyTracking;

    /// <summary>
    /// Configuration for the bodies selection component.
    /// </summary>
    public class BodiesSelectionConfiguration
    {
        /// <summary>
        /// Gets or sets the transformation from camera 2 to camera 1 coordinate system.
        /// </summary>
        public CoordinateSystem? Camera2ToCamera1Transformation { get; set; } = null;

        /// <summary>
        /// Gets or sets the joint used for correspondence between bodies.
        /// </summary>
        public JointId JointUsedForCorrespondence { get; set; } = JointId.Pelvis;

        /// <summary>
        /// Gets or sets the maximum acceptable distance for correspondence in meters.
        /// </summary>
        public double MaxDistance { get; set; } = 0.8;

        /// <summary>
        /// Gets or sets the minimum distance threshold that excludes body pairs from pairing.
        /// </summary>
        public double NotPairableDistanceThreshold { get; set; } = 8;
    }
}
