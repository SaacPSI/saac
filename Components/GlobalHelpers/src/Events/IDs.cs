// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.GlobalHelpers
{
    /// <summary>
    /// Base class for identifiers containing user and object IDs.
    /// </summary>
    public class IDs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IDs"/> class.
        /// </summary>
        /// <param name="userID">The user identifier.</param>
        /// <param name="objectID">The object identifier.</param>
        public IDs(string userID, string objectID)
        {
            this.UserID = userID;
            this.ObjectID = objectID;
        }

        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        public string UserID { get; private set; }

        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        public string ObjectID { get; private set; }
    }
}
