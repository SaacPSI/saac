// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides serialization format for System.Numerics.Matrix4x4 type.
    /// </summary>
    public class PsiFormatMatrix4x4
    {
        /// <summary>
        /// Gets the format for serializing and deserializing Matrix4x4 objects.
        /// </summary>
        /// <returns>A Format instance for Matrix4x4 serialization.</returns>
        public static Format<System.Numerics.Matrix4x4> GetFormat()
        {
            return new Format<System.Numerics.Matrix4x4>(WriteMatrix4x4, ReadMatrix4x4);
        }

        /// <summary>
        /// Writes a Matrix4x4 to a binary writer.
        /// </summary>
        /// <param name="matrix">The Matrix4x4 to write.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WriteMatrix4x4(System.Numerics.Matrix4x4 matrix, BinaryWriter writer)
        {
            writer.Write(matrix.M11);
            writer.Write(matrix.M12);
            writer.Write(matrix.M13);
            writer.Write(matrix.M14);
            writer.Write(matrix.M21);
            writer.Write(matrix.M22);
            writer.Write(matrix.M23);
            writer.Write(matrix.M24);
            writer.Write(matrix.M31);
            writer.Write(matrix.M32);
            writer.Write(matrix.M33);
            writer.Write(matrix.M34);
            writer.Write(matrix.M41);
            writer.Write(matrix.M42);
            writer.Write(matrix.M43);
            writer.Write(matrix.M44);
        }

        /// <summary>
        /// Reads a Matrix4x4 from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized Matrix4x4.</returns>
        public static System.Numerics.Matrix4x4 ReadMatrix4x4(BinaryReader reader)
        {
            System.Numerics.Matrix4x4 matrix = default(System.Numerics.Matrix4x4);
            matrix.M11 = reader.ReadSingle();
            matrix.M12 = reader.ReadSingle();
            matrix.M13 = reader.ReadSingle();
            matrix.M14 = reader.ReadSingle();
            matrix.M21 = reader.ReadSingle();
            matrix.M22 = reader.ReadSingle();
            matrix.M23 = reader.ReadSingle();
            matrix.M24 = reader.ReadSingle();
            matrix.M31 = reader.ReadSingle();
            matrix.M32 = reader.ReadSingle();
            matrix.M33 = reader.ReadSingle();
            matrix.M34 = reader.ReadSingle();
            matrix.M41 = reader.ReadSingle();
            matrix.M42 = reader.ReadSingle();
            matrix.M43 = reader.ReadSingle();
            matrix.M44 = reader.ReadSingle();
            return matrix;
        }
    }
}
