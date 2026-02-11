// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using Microsoft.Azure.Kinect.BodyTracking;

    /// <summary>
    /// Configuration for the body postures detector component.
    /// </summary>
    public class BodyPosturesDetectorConfiguration
    {
        /// <summary>
        /// Gets or sets the minimum confidence level required for joint data.
        /// </summary>
        public JointConfidenceLevel MinimumConfidenceLevel { get; set; } = JointConfidenceLevel.Medium;

        /// <summary>
        /// Gets or sets the minimum distance threshold in meters.
        /// </summary>
        public double MinimumDistanceThreshold { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets the minimum angle in degrees to consider a body as sitting.
        /// </summary>
        public double MinimumSittingDegrees { get; set; } = 90.0;

        /// <summary>
        /// Gets or sets the maximum angle in degrees to consider a body as standing.
        /// </summary>
        public double MaximumStandingDegrees { get; set; } = 15.0;

        /// <summary>
        /// Gets or sets the maximum angle in degrees to consider a body as pointing.
        /// </summary>
        public double MaximumPointingDegrees { get; set; } = 25.0;
    }
}
