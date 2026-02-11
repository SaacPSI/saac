// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

// From OpenSense : https://github.com/intelligent-human-perception-laboratory/OpenSense
// Nuget for HashCode => Microsoft.Bcl.HashCode
namespace SAAC.Helpers
{
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a facial action unit with intensity and presence values.
    /// </summary>
    public class ActionUnit : IEquatable<ActionUnit>
    {
        /// <summary>
        /// The intensity of the action unit.
        /// </summary>
        public readonly double Intensity;

        /// <summary>
        /// The presence probability of the action unit.
        /// </summary>
        public readonly double Presence;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionUnit"/> class.
        /// </summary>
        /// <param name="intensity">The intensity of the action unit.</param>
        /// <param name="presence">The presence probability of the action unit.</param>
        [JsonConstructor]
        public ActionUnit(double intensity, double presence)
        {
            this.Intensity = intensity;
            this.Presence = presence;
        }

        #region IEquatable

        /// <summary>
        /// Determines whether the specified ActionUnit is equal to the current ActionUnit.
        /// </summary>
        /// <param name="other">The ActionUnit to compare with the current instance.</param>
        /// <returns>True if the specified ActionUnit is equal to the current instance; otherwise, false.</returns>
        public bool Equals(ActionUnit other) =>
            this.Intensity.Equals(other.Intensity)
            && this.Presence.Equals(other.Presence);

        /// <summary>
        /// Determines whether the specified object is equal to the current ActionUnit.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>True if the specified object is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object obj) => obj is ActionUnit other ? this.Equals(other) : false;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => HashCode.Combine(
            this.Intensity,
            this.Presence);

        /// <summary>
        /// Determines whether two specified ActionUnit instances are equal.
        /// </summary>
        /// <param name="a">The first ActionUnit to compare.</param>
        /// <param name="b">The second ActionUnit to compare.</param>
        /// <returns>True if a and b are equal; otherwise, false.</returns>
        public static bool operator ==(ActionUnit a, ActionUnit b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified ActionUnit instances are not equal.
        /// </summary>
        /// <param name="a">The first ActionUnit to compare.</param>
        /// <param name="b">The second ActionUnit to compare.</param>
        /// <returns>True if a and b are not equal; otherwise, false.</returns>
        public static bool operator !=(ActionUnit a, ActionUnit b) => !(a == b);
        #endregion
    }
}
