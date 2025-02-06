using Microsoft.Psi.Interop.Serialization;
using SAAC.TeslaSuit;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatHapticParams
    {
        public static Format<HapticParams> GetFormat()
        {
            return new Format<HapticParams>(WriteHapticParams, ReadHapticParams);
        }

        public static void WriteHapticParams(HapticParams data, BinaryWriter writer)
        {
            writer.Write(data.Frequency);
            writer.Write(data.Amplitude);
            writer.Write(data.PulseWidth);
            writer.Write(data.Duration);
        }

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