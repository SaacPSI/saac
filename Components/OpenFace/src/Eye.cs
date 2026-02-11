// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

// From OpenSense : https://github.com/intelligent-human-perception-laboratory/OpenSense
// Nuget for HashCode => Microsoft.Bcl.HashCode
namespace SAAC.Helpers
{
    using System.Collections.Immutable;
    using System.Numerics;

    /// <summary>
    /// Represents eye tracking data including gaze vectors, angles, and landmarks.
    /// </summary>
    public class Eye : IEquatable<Eye>
    {
        /// <summary>
        /// Normalized pupil vector to camera for both eyes.
        /// </summary>
        public readonly GazeVector GazeVector;

        /// <summary>
        /// Absolute gaze angle to camera in radians, mean of both eyes.
        /// </summary>
        public readonly Vector2 Angle;

        /// <summary>
        /// 2D landmarks of the eyes.
        /// </summary>
        public readonly IReadOnlyList<Vector2> Landmarks;

        /// <summary>
        /// 3D landmarks of the eyes.
        /// </summary>
        public readonly IReadOnlyList<Vector3> Landmarks3D;

        /// <summary>
        /// Visible 2D landmarks of the eyes.
        /// </summary>
        public readonly IReadOnlyList<Vector2> VisiableLandmarks;

        /// <summary>
        /// Indicator lines for visualization.
        /// </summary>
        public readonly IReadOnlyList<ValueTuple<Vector2, Vector2>> IndicatorLines;

        /// <summary>
        /// Initializes a new instance of the <see cref="Eye"/> class.
        /// </summary>
        /// <param name="gazeVector">The gaze vector for both eyes.</param>
        /// <param name="angle">The gaze angle in radians.</param>
        /// <param name="landmarks">The 2D eye landmarks.</param>
        /// <param name="visiableLandmarks">The visible 2D eye landmarks.</param>
        /// <param name="landmarks3D">The 3D eye landmarks.</param>
        /// <param name="indicatorLines">The indicator lines for visualization.</param>
        public Eye(
            GazeVector gazeVector,
            Vector2 angle,
            IEnumerable<Vector2> landmarks,
            IEnumerable<Vector2> visiableLandmarks,
            IEnumerable<Vector3> landmarks3D,
            IEnumerable<ValueTuple<Vector2, Vector2>> indicatorLines)
        {
            this.GazeVector = gazeVector;
            this.Angle = angle;
            this.Landmarks = landmarks.ToImmutableArray();
            this.Landmarks3D = landmarks3D.ToImmutableArray();
            this.VisiableLandmarks = visiableLandmarks.ToImmutableArray();
            this.IndicatorLines = indicatorLines.ToImmutableArray();
        }

        /// <summary>
        /// Gets the calculated pupil position as a gaze vector.
        /// Computes the average position of the pupil landmarks for each eye.
        /// </summary>
        public GazeVector PupilPosition
        {
            get
            {
                var leftLandmarks = this.Landmarks3D.Skip(0).Take(8).ToList();
                var leftSum = leftLandmarks.Aggregate((a, b) => a + b);
                var left = leftSum / leftLandmarks.Count;
                var rightLandmarks = this.Landmarks3D.Skip(28).Take(8).ToList();
                var rightSum = rightLandmarks.Aggregate((a, b) => a + b);
                var right = rightSum / rightLandmarks.Count;
                return new GazeVector(left, right);
            }
        }

        /// <summary>
        /// Gets the inner eye corner positions as a gaze vector.
        /// </summary>
        public GazeVector InnerEyeCornerPosition => new GazeVector(this.Landmarks3D[14], this.Landmarks3D[36]);

        #region IEquatable

        /// <summary>
        /// Determines whether the specified Eye is equal to the current Eye.
        /// </summary>
        /// <param name="other">The Eye to compare with the current instance.</param>
        /// <returns>True if the specified Eye is equal to the current instance; otherwise, false.</returns>
        public bool Equals(Eye other) =>
            this.Landmarks.SequenceEqual(other.Landmarks)
            && this.VisiableLandmarks.SequenceEqual(other.VisiableLandmarks)
            && this.Landmarks3D.SequenceEqual(other.Landmarks3D)
            && this.GazeVector.Equals(other.GazeVector)
            && this.Angle.Equals(other.Angle);

        /// <summary>
        /// Determines whether the specified object is equal to the current Eye.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>True if the specified object is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object obj) => obj is Eye other ? this.Equals(other) : false;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => HashCode.Combine(
            this.Landmarks,
            this.VisiableLandmarks,
            this.Landmarks3D,
            this.GazeVector,
            this.Angle);

        /// <summary>
        /// Determines whether two specified Eye instances are equal.
        /// </summary>
        /// <param name="a">The first Eye to compare.</param>
        /// <param name="b">The second Eye to compare.</param>
        /// <returns>True if a and b are equal; otherwise, false.</returns>
        public static bool operator ==(Eye a, Eye b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified Eye instances are not equal.
        /// </summary>
        /// <param name="a">The first Eye to compare.</param>
        /// <param name="b">The second Eye to compare.</param>
        /// <returns>True if a and b are not equal; otherwise, false.</returns>
        public static bool operator !=(Eye a, Eye b) => !(a == b);
        #endregion
    }
}
