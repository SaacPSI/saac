// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bhaptics.PsiFormats
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Provides a \psi binary serialization format for <see cref="SAAC.Bhaptic.Helpers.MotorPlay"/>.
    /// Used to transmit direct motor-level haptic events over TCP streams and store them in a dataset.
    /// The <c>motors</c> array is serialized as a length-prefixed sequence of <c>int</c> values.
    /// </summary>
    public class PsiFormatMotorPlay
    {
        /// <summary>
        /// Returns a <see cref="Format{T}"/> instance that serializes and deserializes <see cref="SAAC.Bhaptic.Helpers.MotorPlay"/> messages.
        /// </summary>
        /// <returns>A <see cref="Format{T}"/> instance for <see cref="SAAC.Bhaptic.Helpers.MotorPlay"/>.</returns>
        public static Format<SAAC.Bhaptic.Helpers.MotorPlay> GetFormat()
        {
            return new Format<SAAC.Bhaptic.Helpers.MotorPlay>(WriteMotorPlay, ReadMotorPlay);
        }

        /// <summary>
        /// Serializes a <see cref="SAAC.Bhaptic.Helpers.MotorPlay"/> message to binary.
        /// Fields are written in order: position, requestId, motors (length then each value), durationMillis.
        /// </summary>
        private static void WriteMotorPlay(SAAC.Bhaptic.Helpers.MotorPlay message, BinaryWriter writer)
        {
            writer.Write(message.Position);
            writer.Write(message.RequestId);
            writer.Write(message.Motors.Length);
            foreach (var motor in message.Motors)
            {
                writer.Write(motor);
            }

            writer.Write(message.DurationMillis);
        }

        /// <summary>
        /// Deserializes a <see cref="SAAC.Bhaptic.Helpers.MotorPlay"/> message from binary.
        /// Fields are read in the same order as <see cref="WriteMotorPlay"/>.
        /// </summary>
        /// <returns>A deserialized <see cref="SAAC.Bhaptic.Helpers.MotorPlay"/> instance.</returns>
        private static SAAC.Bhaptic.Helpers.MotorPlay ReadMotorPlay(BinaryReader reader)
        {
            int position = reader.ReadInt32();
            int requestId = reader.ReadInt32();
            int motorsLength = reader.ReadInt32();
            int[] motors = new int[motorsLength];
            for (int i = 0; i < motorsLength; i++)
            {
                motors[i] = reader.ReadInt32();
            }
            int durationMillis = reader.ReadInt32();
            return new SAAC.Bhaptic.Helpers.MotorPlay(position, requestId, motors, durationMillis);
        }
    }
}
