// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bhaptics.PsiFormats
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides a \psi binary serialization format for <see cref="SAAC.Bhaptic.Helpers.PauseResumeStop"/>.
    /// Used to transmit haptic playback state change commands (pause, resume, stop) over TCP streams
    /// and store them in a dataset.
    /// The <c>state</c> enum is serialized as an <c>int</c>.
    /// </summary>
    public class PsiFormatPauseResumeStop
    {
        /// <summary>
        /// Returns a <see cref="Format{T}"/> instance that serializes and deserializes <see cref="SAAC.Bhaptic.Helpers.PauseResumeStop"/> messages.
        /// </summary>
        /// <returns>A <see cref="Format{T}"/> instance for <see cref="SAAC.Bhaptic.Helpers.PauseResumeStop"/>.</returns>
        public static Format<SAAC.Bhaptic.Helpers.PauseResumeStop> GetFormat()
        {
            return new Format<SAAC.Bhaptic.Helpers.PauseResumeStop>(WritePauseResumeStop, ReadPauseResumeStop);
        }

        /// <summary>
        /// Serializes a <see cref="SAAC.Bhaptic.Helpers.PauseResumeStop"/> message to binary.
        /// Fields are written in order: eventId, state (as int).
        /// </summary>
        private static void WritePauseResumeStop(SAAC.Bhaptic.Helpers.PauseResumeStop message, BinaryWriter writer)
        {
            writer.Write(message.EventId);
            writer.Write((int)message.State);
        }

        /// <summary>
        /// Deserializes a <see cref="SAAC.Bhaptic.Helpers.PauseResumeStop"/> message from binary.
        /// Fields are read in the same order as <see cref="WritePauseResumeStop"/>.
        /// </summary>
        /// <returns>A deserialized <see cref="SAAC.Bhaptic.Helpers.PauseResumeStop"/> message.</returns>
        private static SAAC.Bhaptic.Helpers.PauseResumeStop ReadPauseResumeStop(BinaryReader reader)
        {
            string eventId = reader.ReadString();
            SAAC.Bhaptic.Helpers.PauseResumeStop.EPauseResumeStop state = (SAAC.Bhaptic.Helpers.PauseResumeStop.EPauseResumeStop)reader.ReadInt32();
            return new SAAC.Bhaptic.Helpers.PauseResumeStop(eventId, state);
        }
    }
}
