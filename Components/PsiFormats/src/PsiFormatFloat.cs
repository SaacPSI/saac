// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides serialization format for float type.
    /// </summary>
    public class PsiFormatFloat
    {
        /// <summary>
        /// Gets the format for serializing and deserializing float values.
        /// </summary>
        /// <returns>A Format instance for float serialization.</returns>
        public static Format<float> GetFormat()
        {
            return new Format<float>(WriteFloat, ReadFloat);
        }

        /// <summary>
        /// Writes an float value to a binary writer.
        /// </summary>
        /// <param name="real">The float value to write.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WriteFloat(float real, BinaryWriter writer)
        {
            writer.Write(real);
        }

        /// <summary>
        /// Reads an float value from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized float value.</returns>
        public static float ReadFloat(BinaryReader reader)
        {
            return reader.ReadSingle();
        }
    }
}
