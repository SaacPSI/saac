using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatBatteryEvent
    {
        public static Format<PsiBatteryEvent> GetFormat()
        {
            return new Format<PsiBatteryEvent>(WriteBatteryEvent, ReadBatteryEvent);
        }

        public static void WriteBatteryEvent(PsiBatteryEvent value, BinaryWriter writer)
        {
            writer.Write(value.BatteryId);
            writer.Write(value.Power);
            writer.Write(value.Places);
            writer.Write(value.Regulated);
        }

        public static PsiBatteryEvent ReadBatteryEvent(BinaryReader reader)
        {
            int batteryId = reader.ReadInt32();
            int power = reader.ReadInt32();
            int places = reader.ReadInt32();
            bool regulated = reader.ReadBoolean();

            return new PsiBatteryEvent(batteryId, power, places, regulated);
        }
    }
}

