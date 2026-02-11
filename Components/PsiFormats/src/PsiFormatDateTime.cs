// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides serialization format for System.DateTime type.
    /// </summary>
    public class PsiFormatDateTime
    {
        /// <summary>
        /// Gets the format for serializing and deserializing DateTime objects.
        /// </summary>
        /// <returns>A Format instance for DateTime serialization.</returns>
        public static Format<System.DateTime> GetFormat()
        {
            return new Format<System.DateTime>(WriteDateTime, ReadDateTime);
        }

        /// <summary>
        /// Writes a DateTime to a binary writer.
        /// </summary>
        /// <param name="dateTime">The DateTime to write.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WriteDateTime(System.DateTime dateTime, BinaryWriter writer)
        {
            writer.Write(dateTime.Ticks);
        }

        /// <summary>
        /// Reads a DateTime from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized DateTime.</returns>
        public static System.DateTime ReadDateTime(BinaryReader reader)
        {
            return new System.DateTime((long)reader.ReadUInt64());
        }
    }
}
