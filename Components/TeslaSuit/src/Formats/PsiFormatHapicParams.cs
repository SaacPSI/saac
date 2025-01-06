using Microsoft.Psi.Interop.Serialization;
using SAAC.PipelineServices;
using System.IO;

namespace SAAC.TeslaSuit
{
    public class PsiFormatHapticParams : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<HapticParams>(WriteHapticParams, ReadHapticParams);
        }

        public void WriteHapticParams(HapticParams data, BinaryWriter writer)
        {
            writer.Write(data.Frequency);
            writer.Write(data.Amplitude);
            writer.Write(data.PulseWidth);
            writer.Write(data.Duration);
        }

        public HapticParams ReadHapticParams(BinaryReader reader)
        {
            int frequency = reader.ReadInt32();
            int amplitude = reader.ReadInt32();
            int pulseWidth = reader.ReadInt32();
            long duration = reader.ReadInt64();
            return new HapticParams(frequency, amplitude, pulseWidth, duration);
        }
    }
}