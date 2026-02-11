// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using nuitrack;

    /// <summary>
    /// Configuration for the simple bodies position extraction component.
    /// </summary>
    public class SimpleBodiesPositionExtractionConfiguration
    {
        /// <summary>
        /// Gets or sets the Nuitrack joint used as the global position for the algorithm.
        /// </summary>
        public JointType NuitrackJointAsPosition { get; set; } = JointType.Torso;

        /// <summary>
        /// Gets or sets the Azure Kinect or Simplified skeleton joint used as the global position.
        /// </summary>
        public Microsoft.Azure.Kinect.BodyTracking.JointId GeneralJointAsPosition { get; set; } = Microsoft.Azure.Kinect.BodyTracking.JointId.Pelvis;
    }
}
