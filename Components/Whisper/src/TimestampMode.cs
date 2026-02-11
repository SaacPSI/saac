// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Whisper
{
    /// <summary>
    /// Timestamp mode options for speech recognition results.
    /// </summary>
    public enum TimestampMode
    {
        /// <summary>
        /// No timestamp mode set.
        /// </summary>
        NotSet = 0,

        /// <summary>
        /// Timestamp at the start of the segment.
        /// </summary>
        AtStart = 1,

        /// <summary>
        /// Timestamp at the end of the segment.
        /// </summary>
        AtEnd = 2,
    }
}
