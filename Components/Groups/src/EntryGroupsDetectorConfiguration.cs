// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    /// <summary>
    /// Configuration for the entry groups detector.
    /// </summary>
    public class EntryGroupsDetectorConfiguration
    {
        /// <summary>
        /// Gets or sets the time duration a group must remain stable before being considered a formed entry group.
        /// </summary>
        public TimeSpan GroupFormationDelay { get; set; } = new TimeSpan(0, 0, 2);
    }
}
