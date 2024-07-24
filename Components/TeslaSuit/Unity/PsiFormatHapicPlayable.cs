using Microsoft.Psi.Interop.Serialization;
using System.IO;
using TsSDK;

public class PsiFormatHapticPlayable
{
    public static Format<HapticPlayable> GetFormat()
    {
        return new Format<HapticPlayable>(WriteHapticPlayable, ReadHapticPlayable);
    }

    public static void WriteHapticPlayable(HapticPlayable data, BinaryWriter writer)
    {
        writer.Write(data.Id);
        writer.Write(data.HapticParams.Frequency);
        writer.Write(data.HapticParams.Amplitude);
        writer.Write(data.HapticParams.PulseWidth);
        writer.Write(data.HapticParams.Duration);
    }

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