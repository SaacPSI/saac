// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

// From OpenSense : https://github.com/intelligent-human-perception-laboratory/OpenSense
// Nuget for HashCode => Microsoft.Bcl.HashCode
namespace SAAC.Helpers
{
    using System.Collections.Immutable;

    /// <summary>
    /// Represents facial action units data.
    /// </summary>
    public class Face : IEquatable<Face>
    {
        /// <summary>
        /// Dictionary of action units by name.
        /// </summary>
        public readonly IReadOnlyDictionary<string, ActionUnit> ActionUnits;

        /// <summary>
        /// Initializes a new instance of the <see cref="Face"/> class.
        /// </summary>
        /// <param name="actionUnits">Dictionary of action units keyed by their names.</param>
        public Face(IDictionary<string, ActionUnit> actionUnits)
        {
            this.ActionUnits = actionUnits.ToImmutableSortedDictionary();
        }

        #region IEquatable

        /// <summary>
        /// Determines whether the specified Face is equal to the current Face.
        /// </summary>
        /// <param name="other">The Face to compare with the current instance.</param>
        /// <returns>True if the specified Face is equal to the current instance; otherwise, false.</returns>
        public bool Equals(Face other) =>
            this.ActionUnits.SequenceEqual(other.ActionUnits);

        /// <summary>
        /// Determines whether the specified object is equal to the current Face.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>True if the specified object is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object obj) => obj is Face other ? this.Equals(other) : false;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => HashCode.Combine(
            this.ActionUnits);

        /// <summary>
        /// Determines whether two specified Face instances are equal.
        /// </summary>
        /// <param name="a">The first Face to compare.</param>
        /// <param name="b">The second Face to compare.</param>
        /// <returns>True if a and b are equal; otherwise, false.</returns>
        public static bool operator ==(Face a, Face b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified Face instances are not equal.
        /// </summary>
        /// <param name="a">The first Face to compare.</param>
        /// <param name="b">The second Face to compare.</param>
        /// <returns>True if a and b are not equal; otherwise, false.</returns>
        public static bool operator !=(Face a, Face b) => !(a == b);
        #endregion
    }
}
