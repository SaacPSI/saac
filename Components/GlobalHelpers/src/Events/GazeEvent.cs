// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.GlobalHelpers
{
    /// <summary>
    /// Represents a gaze event with position and gaze status.
    /// </summary>
    public class GazeEvent : IDs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GazeEvent"/> class.
        /// </summary>
        /// <param name="type">The type of event.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="objectID">The object identifier.</param>
        /// <param name="position">The 3D position of the gaze.</param>
        /// <param name="isGazed">Whether the object is currently being gazed at.</param>
        public GazeEvent(EEventType type, string userId, string objectID, System.Numerics.Vector3 position, bool isGazed)
            : base(userId, objectID)
        {
            this.Type = type;
            this.Position = position;
            this.IsGazed = isGazed;
        }

        /// <summary>
        /// Gets the event type.
        /// </summary>
        public EEventType Type { get; private set; }

        /// <summary>
        /// Gets the 3D position of the gaze.
        /// </summary>
        public System.Numerics.Vector3 Position { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the object is currently being gazed at.
        /// </summary>
        public bool IsGazed { get; private set; }
    }
}
