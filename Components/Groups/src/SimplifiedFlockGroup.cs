// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents a simplified flock group with spatial and movement properties.
    /// </summary>
    public class SimplifiedFlockGroup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimplifiedFlockGroup"/> class.
        /// </summary>
        /// <param name="id">The group identifier.</param>
        /// <param name="constituants">The list of body IDs in the group.</param>
        /// <param name="area">The circular area encompassing the group.</param>
        /// <param name="direction">The average direction of movement.</param>
        /// <param name="velocity">The average velocity of the group.</param>
        public SimplifiedFlockGroup(uint id, List<uint> constituants, Circle2D area, Vector2D direction, double velocity)
        {
            this.Id = id;
            this.Constituants = constituants;
            this.Area = area;
            this.Direction = direction;
            this.Velocity = velocity;
        }

        /// <summary>
        /// Gets the group identifier.
        /// </summary>
        public uint Id { get; private set; }

        /// <summary>
        /// Gets the list of body IDs that constitute the group.
        /// </summary>
        public List<uint> Constituants { get; private set; }

        /// <summary>
        /// Gets the circular area encompassing the group.
        /// </summary>
        public Circle2D Area { get; private set; }

        /// <summary>
        /// Gets the average direction of movement.
        /// </summary>
        public Vector2D Direction { get; private set; }

        /// <summary>
        /// Gets the average velocity of the group.
        /// </summary>
        public double Velocity { get; private set; }
    }
}
