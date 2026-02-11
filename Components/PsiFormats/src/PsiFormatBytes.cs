// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides serialization format for byte array type.
    /// </summary>
    public class PsiFormatBytes
    {
        /// <summary>
        /// Gets the format for serializing and deserializing byte arrays.
        /// </summary>
        /// <returns>A Format instance for byte array serialization.</returns>
        public static Format<byte[]> GetFormat()
        {
            return new Format<byte[]>(WriteBytes, ReadBytes);
        }

        /// <summary>
        /// Writes a byte array to a binary writer.
        /// </summary>
        /// <param name="image">The byte array to write.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WriteBytes(byte[] image, BinaryWriter writer)
        {
            writer.Write(image.Length);
            writer.Write(image);
        }

        /// <summary>
        /// Reads a byte array from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized byte array.</returns>
        public static byte[] ReadBytes(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            byte[] data = reader.ReadBytes(length);
            return data;
        }
    }
}
