// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using Microsoft.Psi.Interop.Serialization;
    using SAAC.GlobalHelpers;

    /// <summary>
    /// Provides serialization format for SAAC.GlobalHelpers.GrabEvent type.
    /// </summary>
    public class PsiFormatGrabEvent
    {
        /// <summary>
        /// Gets the format for serializing and deserializing GrabEvent objects.
        /// </summary>
        /// <returns>A Format instance for GrabEvent serialization.</returns>
        public static Format<GrabEvent> GetFormat()
        {
            return new Format<GrabEvent>(WriteGrabEvent, ReadGrabEvent);
        }

        /// <summary>
        /// Writes a GrabEvent to a binary writer.
        /// </summary>
        /// <param name="grabEvent">The GrabEvent to write.</param>
        /// <param name="writer">The binary writer to write to.</param>
        public static void WriteGrabEvent(GrabEvent grabEvent, BinaryWriter writer)
        {
            writer.Write(grabEvent.UserID);
            writer.Write(grabEvent.ObjectID);
            writer.Write((int)grabEvent.Type);
            writer.Write(grabEvent.IsGrabbed);
        }

        /// <summary>
        /// Reads a GrabEvent from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <returns>The deserialized GrabEvent.</returns>
        public static GrabEvent ReadGrabEvent(BinaryReader reader)
        {
            string grabberid = reader.ReadString();
            string objectid = reader.ReadString();
            int type = reader.ReadInt32();
            bool status = reader.ReadBoolean();

            return new GrabEvent((EEventType)type, grabberid, objectid, status);
        }
    }
}
