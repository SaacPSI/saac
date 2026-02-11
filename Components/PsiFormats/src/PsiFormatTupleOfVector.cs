// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides serialization format for Tuple of two System.Numerics.Vector3 objects.
    /// </summary>
    public class PsiFormatTupleOfVector
    {
        /// <summary>
        /// Gets the format for serializing and deserializing tuples of Vector3 objects.
        /// </summary>
        /// <returns>A Format instance for tuple of Vector3 serialization.</returns>
        public static Format<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>> GetFormat()
        {
            return new Format<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>(WriteTupleOfVector, ReadTupleOfVector);
        }

        /// <summary>
        /// Writes a tuple of two Vector3 objects to a binary writer.
        /// </summary>
        /// <param name="point3D">The tuple containing two Vector3 objects to write.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WriteTupleOfVector(Tuple<System.Numerics.Vector3, System.Numerics.Vector3> point3D, BinaryWriter writer)
        {
            writer.Write(point3D.Item1.X);
            writer.Write(point3D.Item1.Y);
            writer.Write(point3D.Item1.Z);
            writer.Write(point3D.Item2.X);
            writer.Write(point3D.Item2.Y);
            writer.Write(point3D.Item2.Z);
        }

        /// <summary>
        /// Reads a tuple of two Vector3 objects from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized tuple containing two Vector3 objects.</returns>
        public static Tuple<System.Numerics.Vector3, System.Numerics.Vector3> ReadTupleOfVector(BinaryReader reader)
        {
            return new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(
                new System.Numerics.Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                            new System.Numerics.Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        }
    }
}
