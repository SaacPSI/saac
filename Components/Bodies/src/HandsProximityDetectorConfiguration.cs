// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using Microsoft.Azure.Kinect.BodyTracking;

    /// <summary>
    /// Configuration for the hands proximity detector component.
    /// </summary>
    public class HandsProximityDetectorConfiguration
    {
        /// <summary>
        /// Gets or sets the minimum confidence level of joints used in the algorithm.
        /// </summary>
        public JointConfidenceLevel MinimumConfidenceLevel { get; set; } = JointConfidenceLevel.Low;

        /// <summary>
        /// Gets or sets the minimum distance threshold between hands in meters.
        /// </summary>
        public double MinimumDistanceThreshold { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets a value indicating whether only specified pairs are checked or all possible pairs are tested.
        /// </summary>
        public bool IsPairToCheckGiven { get; set; } = false;
    }
}
