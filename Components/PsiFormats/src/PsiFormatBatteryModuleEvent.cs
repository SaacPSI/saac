using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatBatteryModuleEvent
    {
        public static Format<PsiBatteryModuleEvent> GetFormat()
        {
            return new Format<PsiBatteryModuleEvent>(WriteBatteryModuleEvent, ReadBatteryModuleEvent);
        }

        public static void WriteBatteryModuleEvent(PsiBatteryModuleEvent value, BinaryWriter writer)
        {
            writer.Write(value.BatteryId);
            writer.Write(value.Power);
            writer.Write(value.Places);
            writer.Write(value.Regulated);
            writer.Write(value.ModuleId);
            writer.Write(value.ModuleType ?? string.Empty);
            writer.Write(value.ModulePower);
            writer.Write(value.PlaceIndex);
            writer.Write(value.ModuleStatus ?? string.Empty);
        }

        public static PsiBatteryModuleEvent ReadBatteryModuleEvent(BinaryReader reader)
        {
            int batteryId = reader.ReadInt32();
            int power = reader.ReadInt32();
            int places = reader.ReadInt32();
            bool regulated = reader.ReadBoolean();
            int moduleId = reader.ReadInt32();
            string moduleType = reader.ReadString();
            int modulePower = reader.ReadInt32();
            int placeIndex = reader.ReadInt32();
            string moduleStatus = reader.ReadString();

            return new PsiBatteryModuleEvent(
                batteryId,
                power,
                places,
                regulated,
                moduleId,
                moduleType,
                modulePower,
                placeIndex,
                moduleStatus);
        }
    }
}

