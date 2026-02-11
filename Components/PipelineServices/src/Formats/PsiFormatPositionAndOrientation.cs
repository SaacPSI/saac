// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.IO;
using Microsoft.Psi.Interop.Serialization;

namespace SAAC.PsiFormats
{
    /// <summary>
    /// Provides serialization format for position and orientation as a tuple of two Vector3 objects.
    /// </summary>
    public class PsiFormatPositionAndOrientation
    {
        /// <summary>
        /// Gets the format for serializing and deserializing position and orientation tuples.
        /// </summary>
        /// <returns>A format instance for position and orientation serialization.</returns>
        public static Format<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>> GetFormat()
        {
            return new Format<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>(WritePositionOrientation, ReadPositionOrientation);
        }

        /// <summary>
        /// Writes a position and orientation tuple to a binary writer.
        /// </summary>
        /// <param name="point3D">The tuple containing position and orientation vectors.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WritePositionOrientation(Tuple<System.Numerics.Vector3, System.Numerics.Vector3> point3D, BinaryWriter writer)
        {
            writer.Write((double)point3D.Item1.X);
            writer.Write((double)point3D.Item1.Y);
            writer.Write((double)point3D.Item1.Z);
            writer.Write((double)point3D.Item2.X);
            writer.Write((double)point3D.Item2.Y);
            writer.Write((double)point3D.Item2.Z);
        }

        /// <summary>
        /// Reads a position and orientation tuple from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized tuple containing position and orientation vectors.</returns>
        public static Tuple<System.Numerics.Vector3, System.Numerics.Vector3> ReadPositionOrientation(BinaryReader reader)
        {
            return new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(
                new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()),
                            new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()));
        }
    }
}
