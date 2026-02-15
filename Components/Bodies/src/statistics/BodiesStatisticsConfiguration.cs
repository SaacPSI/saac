// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies.Statistics
{
    using Microsoft.Azure.Kinect.BodyTracking;

    /// <summary>
    /// Configuration for the bodies statistics component.
    /// </summary>
    public class BodiesStatisticsConfiguration
    {
        /// <summary>
        /// Gets or sets the minimum confidence level required for joints used in statistics.
        /// </summary>
        public JointConfidenceLevel ConfidenceLevel { get; set; } = JointConfidenceLevel.Medium;

        /// <summary>
        /// Gets or sets the file path for storing statistics in CSV format.
        /// </summary>
        public string StoringPath { get; set; } = "./Stats.csv";
    }
}
