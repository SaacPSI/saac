// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.GlobalHelpers
{
    /// <summary>
    /// Represents a grab event with grab status.
    /// </summary>
    public class GrabEvent : IDs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrabEvent"/> class.
        /// </summary>
        /// <param name="type">The type of event.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="objectID">The object identifier.</param>
        /// <param name="isGrabbed">Whether the object is currently being grabbed.</param>
        public GrabEvent(EEventType type, string userId, string objectID, bool isGrabbed)
            : base(userId, objectID)
        {
            this.Type = type;
            this.IsGrabbed = isGrabbed;
        }

        /// <summary>
        /// Gets the event type.
        /// </summary>
        public EEventType Type { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the object is currently being grabbed.
        /// </summary>
        public bool IsGrabbed { get; private set; }
    }
}
