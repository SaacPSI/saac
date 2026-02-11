// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

// From OpenSense : https://github.com/intelligent-human-perception-laboratory/OpenSense
// Nuget for HashCode => Microsoft.Bcl.HashCode
namespace SAAC.Helpers
{
    using System.Numerics;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents gaze direction vectors for left and right eyes.
    /// </summary>
    public class GazeVector : IEquatable<GazeVector>
    {
        /// <summary>
        /// The gaze vector for the left eye.
        /// </summary>
        public readonly Vector3 Left;

        /// <summary>
        /// The gaze vector for the right eye.
        /// </summary>
        public readonly Vector3 Right;

        /// <summary>
        /// Initializes a new instance of the <see cref="GazeVector"/> class.
        /// </summary>
        /// <param name="left">The gaze vector for the left eye.</param>
        /// <param name="right">The gaze vector for the right eye.</param>
        [JsonConstructor]
        public GazeVector(Vector3 left, Vector3 right)
        {
            this.Left = left;
            this.Right = right;
        }

        #region IEquatable

        /// <summary>
        /// Determines whether the specified GazeVector is equal to the current GazeVector.
        /// </summary>
        /// <param name="other">The GazeVector to compare with the current instance.</param>
        /// <returns>True if the specified GazeVector is equal to the current instance; otherwise, false.</returns>
        public bool Equals(GazeVector other) =>
            this.Left.Equals(other.Left)
            && this.Right.Equals(other.Right);

        /// <summary>
        /// Determines whether the specified object is equal to the current GazeVector.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>True if the specified object is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object obj) => obj is GazeVector other ? this.Equals(other) : false;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => HashCode.Combine(
            this.Left,
            this.Right);

        /// <summary>
        /// Determines whether two specified GazeVector instances are equal.
        /// </summary>
        /// <param name="a">The first GazeVector to compare.</param>
        /// <param name="b">The second GazeVector to compare.</param>
        /// <returns>True if a and b are equal; otherwise, false.</returns>
        public static bool operator ==(GazeVector a, GazeVector b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified GazeVector instances are not equal.
        /// </summary>
        /// <param name="a">The first GazeVector to compare.</param>
        /// <param name="b">The second GazeVector to compare.</param>
        /// <returns>True if a and b are not equal; otherwise, false.</returns>
        public static bool operator !=(GazeVector a, GazeVector b) => !(a == b);
        #endregion
    }
}
