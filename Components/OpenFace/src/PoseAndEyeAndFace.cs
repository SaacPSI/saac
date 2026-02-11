// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

// From OpenSense : https://github.com/intelligent-human-perception-laboratory/OpenSense
// Nuget for HashCode => Microsoft.Bcl.HashCode
namespace SAAC.Helpers
{
    /// <summary>
    /// Represents combined head pose, eye tracking, and facial action unit data.
    /// </summary>
    public class PoseAndEyeAndFace : IEquatable<PoseAndEyeAndFace>
    {
        /// <summary>
        /// The head pose data.
        /// </summary>
        public readonly Pose Pose;

        /// <summary>
        /// The eye tracking data.
        /// </summary>
        public readonly Eye Eye;

        /// <summary>
        /// The facial action units data.
        /// </summary>
        public readonly Face Face;

        /// <summary>
        /// Initializes a new instance of the <see cref="PoseAndEyeAndFace"/> class.
        /// </summary>
        /// <param name="headPose">The head pose data.</param>
        /// <param name="gaze">The eye tracking data.</param>
        /// <param name="face">The facial action units data.</param>
        public PoseAndEyeAndFace(Pose headPose, Eye gaze, Face face)
        {
            this.Pose = headPose;
            this.Eye = gaze;
            this.Face = face;
        }

        #region IEquatable

        /// <summary>
        /// Determines whether the specified PoseAndEyeAndFace is equal to the current PoseAndEyeAndFace.
        /// </summary>
        /// <param name="other">The PoseAndEyeAndFace to compare with the current instance.</param>
        /// <returns>True if the specified PoseAndEyeAndFace is equal to the current instance; otherwise, false.</returns>
        public bool Equals(PoseAndEyeAndFace other) =>
            this.Pose.Equals(other.Pose)
            && this.Eye.Equals(other.Eye)
            && this.Face.Equals(other.Face);

        /// <summary>
        /// Determines whether the specified object is equal to the current PoseAndEyeAndFace.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>True if the specified object is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object obj) => obj is PoseAndEyeAndFace other ? this.Equals(other) : false;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => HashCode.Combine(
            this.Pose,
            this.Eye,
            this.Face);

        /// <summary>
        /// Determines whether two specified PoseAndEyeAndFace instances are equal.
        /// </summary>
        /// <param name="a">The first PoseAndEyeAndFace to compare.</param>
        /// <param name="b">The second PoseAndEyeAndFace to compare.</param>
        /// <returns>True if a and b are equal; otherwise, false.</returns>
        public static bool operator ==(PoseAndEyeAndFace a, PoseAndEyeAndFace b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified PoseAndEyeAndFace instances are not equal.
        /// </summary>
        /// <param name="a">The first PoseAndEyeAndFace to compare.</param>
        /// <param name="b">The second PoseAndEyeAndFace to compare.</param>
        /// <returns>True if a and b are not equal; otherwise, false.</returns>
        public static bool operator !=(PoseAndEyeAndFace a, PoseAndEyeAndFace b) => !(a == b);
        #endregion
    }
}
