// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    /// <summary>
    /// Configuration for the instant groups detector.
    /// </summary>
    public class InstantGroupsDetectorConfiguration
    {
        /// <summary>
        /// Gets or sets the distance threshold between skeletons to constitute a group.
        /// </summary>
        public double DistanceThreshold { get; set; } = 0.8;
    }
}
