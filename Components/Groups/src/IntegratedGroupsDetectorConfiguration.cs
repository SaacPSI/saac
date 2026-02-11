// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    /// <summary>
    /// Configuration for the integrated groups detector.
    /// </summary>
    public class IntegratedGroupsDetectorConfiguration
    {
        /// <summary>
        /// Gets or sets the weight increase factor when a body is found in a group.
        /// </summary>
        public double IncreaseWeightFactor { get; set; } = 3.0;

        /// <summary>
        /// Gets or sets the weight decrease factor when a body is not found in a group.
        /// </summary>
        public double DecreaseWeightFactor { get; set; } = 2.0;

        /// <summary>
        /// Gets or sets the intersection percentage threshold for merging groups.
        /// When two groups have an intersection percentage above this value, the smaller group is removed.
        /// </summary>
        public double IntersectionPercentage { get; set; } = 0.8;
    }
}
