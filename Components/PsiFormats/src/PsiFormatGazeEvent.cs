// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using Microsoft.Psi.Interop.Serialization;
    using SAAC.GlobalHelpers;

    /// <summary>
    /// Provides serialization format for SAAC.GlobalHelpers.GazeEvent type.
    /// </summary>
    public class PsiFormatGazeEvent
    {
        /// <summary>
        /// Gets the format for serializing and deserializing GazeEvent objects.
        /// </summary>
        /// <returns>A Format instance for GazeEvent serialization.</returns>
        public static Format<GazeEvent> GetFormat()
        {
            return new Format<GazeEvent>(WriteGazeEvent, ReadGazeEvent);
        }

        /// <summary>
        /// Writes a GazeEvent to a binary writer.
        /// </summary>
        /// <param name="gazeEvent">The GazeEvent to write.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WriteGazeEvent(GazeEvent gazeEvent, BinaryWriter writer)
        {
            writer.Write(gazeEvent.UserID);
            writer.Write(gazeEvent.ObjectID);
            writer.Write((int)gazeEvent.Type);
            writer.Write(gazeEvent.Position.X);
            writer.Write(gazeEvent.Position.Y);
            writer.Write(gazeEvent.Position.Z);
            writer.Write(gazeEvent.IsGazed);
        }

        /// <summary>
        /// Reads a GazeEvent from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized GazeEvent.</returns>
        public static GazeEvent ReadGazeEvent(BinaryReader reader)
        {
            string gazerid = reader.ReadString();
            string objectid = reader.ReadString();
            int type = reader.ReadInt32();
            System.Numerics.Vector3 position = new System.Numerics.Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            bool status = reader.ReadBoolean();
            return new GazeEvent((EEventType)type, gazerid, objectid, position, status);
        }
    }
}
