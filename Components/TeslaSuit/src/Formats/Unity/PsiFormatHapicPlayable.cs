// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;
    using SAAC.TeslaSuit;

    /// <summary>
    /// Provides Psi format serialization for haptic playable data.
    /// </summary>
    public class PsiFormatHapticPlayable
    {
        /// <summary>
        /// Gets the format configuration for haptic playable data.
        /// </summary>
        /// <returns>The format configuration.</returns>
        public static Format<HapticPlayable> GetFormat()
        {
            return new Format<HapticPlayable>(WriteHapticPlayable, ReadHapticPlayable);
        }

        /// <summary>
        /// Writes haptic playable data to a binary writer.
        /// </summary>
        /// <param name="data">The haptic playable data to write.</param>
        /// <param name="writer">The binary writer.</param>
        public static void WriteHapticPlayable(HapticPlayable data, BinaryWriter writer)
        {
            writer.Write(data.Id);
            writer.Write(data.HapticParams.Frequency);
            writer.Write(data.HapticParams.Amplitude);
            writer.Write(data.HapticParams.PulseWidth);
            writer.Write(data.HapticParams.Duration);
        }

        /// <summary>
        /// Reads haptic playable data from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <returns>The haptic playable data.</returns>
        public static HapticPlayable ReadHapticPlayable(BinaryReader reader)
        {
            ulong id = reader.ReadUInt64();
            int frequency = reader.ReadInt32();
            int amplitude = reader.ReadInt32();
            int pulseWidth = reader.ReadInt32();
            long duration = reader.ReadInt64();
            return new HapticPlayable(id, frequency, amplitude, pulseWidth, duration);
        }
    }
}
