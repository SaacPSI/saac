// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    /// <summary>
    /// Internal class representing group information with timestamp.
    /// </summary>
    internal class GroupInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupInfo"/> class.
        /// </summary>
        /// <param name="id">The group identifier.</param>
        /// <param name="timestamp">The timestamp of the group.</param>
        /// <param name="list">The list of body IDs in the group.</param>
        public GroupInfo(uint id, DateTime timestamp, List<uint> list)
        {
            this.Id = id;
            this.Timestamp = timestamp;
            this.Bodies = list;
        }

        /// <summary>
        /// Gets the group identifier.
        /// </summary>
        public uint Id { get; private set; }

        /// <summary>
        /// Gets the timestamp of the group.
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Gets the list of body IDs in the group.
        /// </summary>
        public List<uint> Bodies { get; private set; }
    }
}
