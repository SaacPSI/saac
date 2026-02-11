// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides serialization format for char type.
    /// </summary>
    public class PsiFormatChar
    {
        /// <summary>
        /// Gets the format for serializing and deserializing char values.
        /// </summary>
        /// <returns>A Format instance for char serialization.</returns>
        public static Format<char> GetFormat()
        {
            return new Format<char>(WriteChar, ReadChar);
        }

        /// <summary>
        /// Writes a char value to a binary writer.
        /// </summary>
        /// <param name="character">The char value to write.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WriteChar(char character, BinaryWriter writer)
        {
            writer.Write(character);
        }

        /// <summary>
        /// Reads a char value from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized char value.</returns>
        public static char ReadChar(BinaryReader reader)
        {
            return reader.ReadChar();
        }
    }
}
