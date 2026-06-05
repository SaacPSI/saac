// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bhaptics.PsiFormats
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides a \psi binary serialization format for <see cref="SAAC.Bhaptic.Helpers.HapticPlay"/>.
    /// Used to transmit pattern-based haptic play events over TCP streams and store them in a dataset.
    /// </summary>
    public class PsiFormatHapticPlay
    {
        /// <summary>
        /// Returns a <see cref="Format{T}"/> instance that serializes and deserializes <see cref="SAAC.Bhaptic.Helpers.HapticPlay"/> messages.
        /// </summary>
        /// <returns></returns>
        public static Format<SAAC.Bhaptic.Helpers.HapticPlay> GetFormat()
        {
            return new Format<SAAC.Bhaptic.Helpers.HapticPlay>(WriteHapticPlay, ReadHapticPlay);
        }

        /// <summary>
        /// Serializes a <see cref="SAAC.Bhaptic.Helpers.HapticPlay"/> message to binary.
        /// Fields are written in order: eventId, requestId, startMillis, intensity, duration, angleX, offsetY, count.
        /// </summary>
        private static void WriteHapticPlay(SAAC.Bhaptic.Helpers.HapticPlay message, BinaryWriter writer)
        {
            writer.Write(message.EventId);
            writer.Write(message.RequestId);
            writer.Write(message.StartMillis);
            writer.Write(message.Intensity);
            writer.Write(message.Duration);
            writer.Write(message.AngleX);
            writer.Write(message.OffsetY);
            writer.Write(message.Count);
        }

        /// <summary>
        /// Deserializes a <see cref="SAAC.Bhaptic.Helpers.HapticPlay"/> message from binary.
        /// Fields are read in the same order as <see cref="WriteHapticPlay"/>.
        /// </summary>
        /// <returns>A new <see cref="SAAC.Bhaptic.Helpers.HapticPlay"/> instance with fields populated from the binary data.</returns>
        private static SAAC.Bhaptic.Helpers.HapticPlay ReadHapticPlay(BinaryReader reader)
        {
            string eventId = reader.ReadString();
            int requestId = reader.ReadInt32();
            int startMillis = reader.ReadInt32();
            float intensity = reader.ReadSingle();
            float duration = reader.ReadSingle();
            float angleX = reader.ReadSingle();
            float offsetY = reader.ReadSingle();
            int count = reader.ReadInt32();
            return new SAAC.Bhaptic.Helpers.HapticPlay(eventId, requestId, startMillis, intensity, duration, angleX, offsetY, count);
        }
    }
}
