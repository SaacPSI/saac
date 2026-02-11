// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides serialization format for string type.
    /// </summary>
    public class PsiFormatString
    {
        /// <summary>
        /// Gets the format for serializing and deserializing string values.
        /// </summary>
        /// <returns>A Format instance for string serialization.</returns>
        public static Format<string> GetFormat()
        {
            return new Format<string>(WriteString, ReadSring);
        }

        /// <summary>
        /// Writes a string to a binary writer.
        /// </summary>
        /// <param name="data">The string to write.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WriteString(string data, BinaryWriter writer)
        {
            writer.Write(data);
        }

        /// <summary>
        /// Reads a string from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized string.</returns>
        public static string ReadSring(BinaryReader reader)
        {
            return reader.ReadString();
        }
    }
}
