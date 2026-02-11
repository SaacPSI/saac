// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    /// <summary>
    /// Configuration for the simplified flock groups detector.
    /// </summary>
    public class SimplifiedFlockGroupsDetectorConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum size of the bodies queues for calculation.
        /// </summary>
        public uint QueueMaxCount { get; set; } = 60;

        /// <summary>
        /// Gets or sets the distance weight for the model to constitute a group.
        /// </summary>
        public double DistanceWeight { get; set; } = 1;

        /// <summary>
        /// Gets or sets the velocity weight for the model to constitute a group.
        /// </summary>
        public double VelocityWeight { get; set; } = 0;

        /// <summary>
        /// Gets or sets the direction weight for the model to constitute a group.
        /// </summary>
        public double DirectionWeight { get; set; } = 0;

        /// <summary>
        /// Gets or sets the model value threshold to constitute a group.
        /// </summary>
        public double ModelThreshold { get; set; } = 0.8;
    }
}
