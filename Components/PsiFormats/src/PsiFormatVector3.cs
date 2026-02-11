// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides serialization format for System.Numerics.Vector3 type.
    /// </summary>
    public class PsiFormatVector3
    {
        /// <summary>
        /// Gets the format for serializing and deserializing Vector3 objects.
        /// </summary>
        /// <returns>A Format instance for Vector3 serialization.</returns>
        public static Format<System.Numerics.Vector3> GetFormat()
        {
            return new Format<System.Numerics.Vector3>(WriteVector3, ReadVector3);
        }

        /// <summary>
        /// Writes a Vector3 to a binary writer.
        /// </summary>
        /// <param name="point3D">The Vector3 to write.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WriteVector3(System.Numerics.Vector3 point3D, BinaryWriter writer)
        {
            writer.Write(point3D.X);
            writer.Write(point3D.Y);
            writer.Write(point3D.Z);
        }

        /// <summary>
        /// Reads a Vector3 from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized Vector3.</returns>
        public static System.Numerics.Vector3 ReadVector3(BinaryReader reader)
        {
            return new System.Numerics.Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
