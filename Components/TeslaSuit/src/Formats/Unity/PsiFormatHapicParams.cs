// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;
    using SAAC.TeslaSuit;

    /// <summary>
    /// Provides Psi format serialization for haptic parameters.
    /// </summary>
    public class PsiFormatHapticParams
    {
        /// <summary>
        /// Gets the format configuration for haptic parameters.
        /// </summary>
        /// <returns>The format configuration.</returns>
        public static Format<HapticParams> GetFormat()
        {
            return new Format<HapticParams>(WriteHapticParams, ReadHapticParams);
        }

        /// <summary>
        /// Writes haptic parameters to a binary writer.
        /// </summary>
        /// <param name="data">The haptic parameters to write.</param>
        /// <param name="writer">The binary writer.</param>
        public static void WriteHapticParams(HapticParams data, BinaryWriter writer)
        {
            writer.Write(data.Frequency);
            writer.Write(data.Amplitude);
            writer.Write(data.PulseWidth);
            writer.Write(data.Duration);
        }

        /// <summary>
        /// Reads haptic parameters from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <returns>The haptic parameters.</returns>
        public static HapticParams ReadHapticParams(BinaryReader reader)
        {
            int frequency = reader.ReadInt32();
            int amplitude = reader.ReadInt32();
            int pulseWidth = reader.ReadInt32();
            long duration = reader.ReadInt64();
            return new HapticParams(frequency, amplitude, pulseWidth, duration);
        }
    }
}
