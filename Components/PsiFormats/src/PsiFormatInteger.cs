// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides serialization format for int type.
    /// </summary>
    public class PsiFormatInteger
    {
        /// <summary>
        /// Gets the format for serializing and deserializing integer values.
        /// </summary>
        /// <returns>A Format instance for integer serialization.</returns>
        public static Format<int> GetFormat()
        {
            return new Format<int>(WriteInteger, ReadInteger);
        }

        /// <summary>
        /// Writes an integer value to a binary writer.
        /// </summary>
        /// <param name="integer">The integer value to write.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WriteInteger(int integer, BinaryWriter writer)
        {
            writer.Write(integer);
        }

        /// <summary>
        /// Reads an integer value from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized integer value.</returns>
        public static int ReadInteger(BinaryReader reader)
        {
            return reader.Read();
        }
    }
}
