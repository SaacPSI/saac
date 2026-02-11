// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

// From OpenSense : https://github.com/intelligent-human-perception-laboratory/OpenSense
// Nuget for HashCode => Microsoft.Bcl.HashCode
namespace SAAC.Helpers
{
    using System.Collections.Immutable;
    using System.Numerics;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents head pose data including position, rotation, and facial landmarks.
    /// </summary>
    public class Pose
    {
        /// <summary>
        /// Absolute head position to camera in millimeters.
        /// </summary>
        public readonly Vector3 Position;

        /// <summary>
        /// Absolute head rotation to camera in radians.
        /// </summary>
        public readonly Vector3 Angle;

        /// <summary>
        /// 2D facial landmarks.
        /// </summary>
        public readonly IReadOnlyList<Vector2> Landmarks;

        /// <summary>
        /// Visible 2D facial landmarks.
        /// </summary>
        public readonly IReadOnlyList<Vector2> VisiableLandmarks;

        /// <summary>
        /// 3D facial landmarks.
        /// </summary>
        public readonly IReadOnlyList<Vector3> Landmarks3D;

        /// <summary>
        /// Indicator lines for visualization.
        /// </summary>
        public readonly IReadOnlyList<ValueTuple<Vector2, Vector2>> IndicatorLines;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pose"/> class.
        /// </summary>
        /// <param name="position">The head position in millimeters.</param>
        /// <param name="angle">The head rotation angles in radians.</param>
        /// <param name="landmarks">The 2D facial landmarks.</param>
        /// <param name="visiableLandmarks">The visible 2D facial landmarks.</param>
        /// <param name="landmarks3D">The 3D facial landmarks.</param>
        /// <param name="indicatorLines">The indicator lines for visualization.</param>
        [JsonConstructor]
        public Pose(
            Vector3 position,
            Vector3 angle,
            ImmutableArray<Vector2> landmarks,
            ImmutableArray<Vector2> visiableLandmarks,
            ImmutableArray<Vector3> landmarks3D,
            ImmutableArray<ValueTuple<Vector2, Vector2>> indicatorLines)
        {
            this.IndicatorLines = indicatorLines;
            this.Landmarks = landmarks;
            this.VisiableLandmarks = visiableLandmarks;
            this.Landmarks3D = landmarks3D;
            this.Position = position;
            this.Angle = angle;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pose"/> class from raw data.
        /// </summary>
        /// <param name="data">Raw pose data containing position (x, y, z) and angles (rx, ry, rz).</param>
        /// <param name="landmarks">The 2D facial landmarks.</param>
        /// <param name="visiableLandmarks">The visible 2D facial landmarks.</param>
        /// <param name="landmarks3D">The 3D facial landmarks.</param>
        /// <param name="indicatorLines">The indicator lines for visualization.</param>
        public Pose(
            IList<float> data,
            IEnumerable<Vector2> landmarks,
            IEnumerable<Vector2> visiableLandmarks,
            IEnumerable<Vector3> landmarks3D,
            IEnumerable<ValueTuple<Vector2, Vector2>> indicatorLines)
            : this(
                new Vector3(data[0], data[1], data[2]),
                new Vector3(data[3], data[4], data[5]),
                landmarks.ToImmutableArray(),
                visiableLandmarks.ToImmutableArray(),
                landmarks3D.ToImmutableArray(),
                indicatorLines.ToImmutableArray())
        {
        }

        /// <summary>
        /// Gets the 3D position of the nose tip landmark.
        /// </summary>
        public Vector3 NoseTip3D => this.Landmarks3D[30];

        #region To accommodate old code

        /// <summary>
        /// Gets the pose component at the specified index.
        /// Indices 0-2 correspond to position (x, y, z), indices 3-5 correspond to angles (rx, ry, rz).
        /// </summary>
        /// <param name="index">The zero-based index of the component to get.</param>
        /// <returns>The pose component value as a double.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when index is not between 0 and 5.</exception>
        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return this.Position.X;
                    case 1:
                        return this.Position.Y;
                    case 2:
                        return this.Position.Z;
                    case 3:
                        return this.Angle.X;
                    case 4:
                        return this.Angle.Y;
                    case 5:
                        return this.Angle.Z;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }

        /// <summary>
        /// Gets the number of pose components (always 6: 3 for position, 3 for angles).
        /// </summary>
        public int Count => 6;

        /// <summary>
        /// Returns an enumerator that iterates through the pose components.
        /// </summary>
        /// <returns>An enumerator for the pose components.</returns>
        public IEnumerator<double> GetEnumerator()
        {
            for (var i = 0; i < this.Count; i++)
            {
                yield return this[i];
            }
        }

        #endregion

        #region IEquatable

        /// <summary>
        /// Determines whether the specified Pose is equal to the current Pose.
        /// </summary>
        /// <param name="other">The Pose to compare with the current instance.</param>
        /// <returns>True if the specified Pose is equal to the current instance; otherwise, false.</returns>
        public bool Equals(Pose other) =>
            this.Landmarks.SequenceEqual(other.Landmarks)
            && this.VisiableLandmarks.SequenceEqual(other.VisiableLandmarks)
            && this.Landmarks3D.SequenceEqual(other.Landmarks3D)
            && this.Position.Equals(other.Position)
            && this.Angle.Equals(other.Angle);

        /// <summary>
        /// Determines whether the specified object is equal to the current Pose.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>True if the specified object is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object obj) => obj is Pose other ? this.Equals(other) : false;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => HashCode.Combine(
            this.Landmarks,
            this.VisiableLandmarks,
            this.Landmarks3D,
            this.Position,
            this.Angle);

        /// <summary>
        /// Determines whether two specified Pose instances are equal.
        /// </summary>
        /// <param name="a">The first Pose to compare.</param>
        /// <param name="b">The second Pose to compare.</param>
        /// <returns>True if a and b are equal; otherwise, false.</returns>
        public static bool operator ==(Pose a, Pose b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified Pose instances are not equal.
        /// </summary>
        /// <param name="a">The first Pose to compare.</param>
        /// <param name="b">The second Pose to compare.</param>
        /// <returns>True if a and b are not equal; otherwise, false.</returns>
        public static bool operator !=(Pose a, Pose b) => !(a == b);
        #endregion
    }
}
