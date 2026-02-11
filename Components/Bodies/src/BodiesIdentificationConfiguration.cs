// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using Microsoft.Azure.Kinect.BodyTracking;

    /// <summary>
    /// Configuration for the bodies identification component.
    /// </summary>
    public class BodiesIdentificationConfiguration
    {
        /// <summary>
        /// Gets or sets the minimum acceptable confidence level for learning body characteristics.
        /// </summary>
        public JointConfidenceLevel MinimumConfidenceLevelForLearning { get; set; } = JointConfidenceLevel.Low;

        /// <summary>
        /// Gets or sets the bone list used.
        /// </summary>
        public List<(JointId ChildJoint, JointId ParentJoint)> BonesUsedForCorrespondence { get; set; } = new List<(JointId, JointId)>
        {
            (JointId.SpineNavel, JointId.Pelvis),
            (JointId.SpineChest, JointId.SpineNavel),
            (JointId.Neck, JointId.SpineChest),
            (JointId.ClavicleLeft, JointId.SpineChest),
            (JointId.ShoulderLeft, JointId.ClavicleLeft),
            (JointId.ElbowLeft, JointId.ShoulderLeft),
            (JointId.WristLeft, JointId.ElbowLeft),
            //(JointId.HandLeft, JointId.WristLeft),
            //(JointId.HandTipLeft, JointId.HandLeft),
            //(JointId.ThumbLeft, JointId.WristLeft),
            (JointId.ClavicleRight, JointId.SpineChest),
            (JointId.ShoulderRight, JointId.ClavicleRight),
            (JointId.ElbowRight, JointId.ShoulderRight),
            (JointId.WristRight, JointId.ElbowRight),
            //(JointId.HandRight, JointId.WristRight),
            //(JointId.HandTipRight, JointId.HandRight),
            //(JointId.ThumbRight, JointId.WristRight),
            (JointId.HipLeft, JointId.Pelvis),
            (JointId.KneeLeft, JointId.HipLeft),
            (JointId.AnkleLeft, JointId.KneeLeft),
            (JointId.FootLeft, JointId.AnkleLeft),
            (JointId.HipRight, JointId.Pelvis),
            (JointId.KneeRight, JointId.HipRight),
            (JointId.AnkleRight, JointId.KneeRight),
            (JointId.FootRight, JointId.AnkleRight),
            (JointId.Head, JointId.Neck),
            //(JointId.Nose, JointId.Head),
            //(JointId.EyeLeft, JointId.Head),
            //(JointId.EarLeft, JointId.Head),
            //(JointId.EyeRight, JointId.Head),
            //(JointId.EarRight, JointId.Head)
        };

        /// <summary>
        /// Gets or sets maximum acceptable duration for correpondance in millisecond
        /// </summary>
        public TimeSpan MaximumIdentificationTime { get; set; } = new TimeSpan(0, 0, 0, 0, 500);

        /// <summary>
        /// Gets or sets minimum time for trying the correspondance below that time we trust the Kinect identification algo
        /// </summary>
        public TimeSpan MinimumIdentificationTime { get; set; } = new TimeSpan(0, 1, 0);

        /// <summary>
        /// Gets or sets maximum acceptable duration for between old id pop again without identification in millisecond
        /// </summary>
        public TimeSpan MaximumLostTime { get; set; } = new TimeSpan(0, 5, 0);

        /// <summary>
        /// Gets or sets maximum acceptable deviation for correpondance in meter
        /// </summary>
        public double MaximumDeviationAllowed { get; set; } = 0.0025;

        /// <summary>
        /// Gets or sets maximum acceptable deviation for correpondance in meter
        /// </summary>
        public uint MinimumBonesForIdentification { get; set; } = 5;
    }
}
