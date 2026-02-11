// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides serialization format for bool type.
    /// </summary>
    public class PsiFormatBoolean
    {
        /// <summary>
        /// Gets the format for serializing and deserializing boolean values.
        /// </summary>
        /// <returns>A Format instance for boolean serialization.</returns>
        public static Format<bool> GetFormat()
        {
            return new Format<bool>(WriteBoolean, ReadBoolean);
        }

        /// <summary>
        /// Writes a boolean value to a binary writer.
        /// </summary>
        /// <param name="boolean">The boolean value to write.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WriteBoolean(bool boolean, BinaryWriter writer)
        {
            writer.Write(boolean);
        }

        /// <summary>
        /// Reads a boolean value from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized boolean value.</returns>
        public static bool ReadBoolean(BinaryReader reader)
        {
            return reader.ReadBoolean();
        }
    }
}
