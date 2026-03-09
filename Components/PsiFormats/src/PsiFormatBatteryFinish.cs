using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatBatteryFinish
    {
        public static Format<PsiBatteryFinish> GetFormat()
        {
            return new Format<PsiBatteryFinish>(WriteBatteryFinish, ReadBatteryFinish);
        }

        public static void WriteBatteryFinish(PsiBatteryFinish value, BinaryWriter writer)
        {
            // Booleans
            writer.Write(value.NegativeBorn);
            writer.Write(value.PositiveBorn);
            writer.Write(value.FrontBorn);
            writer.Write(value.BackBorn);
            writer.Write(value.OnlyTwoBorns);

            // Ints
            writer.Write(value.CompletedSpaces);
            writer.Write(value.TotalSpaces);

            // Arrays: length then elements
            if (value.GivenVoltages == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(value.GivenVoltages.Length);
                foreach (int v in value.GivenVoltages)
                {
                    writer.Write(v);
                }
            }

            if (value.VoltagesRequired == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(value.VoltagesRequired.Length);
                foreach (int v in value.VoltagesRequired)
                {
                    writer.Write(v);
                }
            }

            writer.Write(value.MatchVoltages);
            writer.Write(value.Regulated);
        }

        public static PsiBatteryFinish ReadBatteryFinish(BinaryReader reader)
        {
            // Booleans
            bool negativeBorn = reader.ReadBoolean();
            bool positiveBorn = reader.ReadBoolean();
            bool frontBorn = reader.ReadBoolean();
            bool backBorn = reader.ReadBoolean();
            bool onlyTwoBorns = reader.ReadBoolean();

            // Ints
            int completedSpaces = reader.ReadInt32();
            int totalSpaces = reader.ReadInt32();

            // Arrays
            int givenLength = reader.ReadInt32();
            int[] givenVoltages = new int[givenLength];
            for (int i = 0; i < givenLength; i++)
            {
                givenVoltages[i] = reader.ReadInt32();
            }

            int requiredLength = reader.ReadInt32();
            int[] voltagesRequired = new int[requiredLength];
            for (int i = 0; i < requiredLength; i++)
            {
                voltagesRequired[i] = reader.ReadInt32();
            }

            int matchVoltages = reader.ReadInt32();
            bool regulated = reader.ReadBoolean();

            return new PsiBatteryFinish(
                negativeBorn,
                positiveBorn,
                frontBorn,
                backBorn,
                onlyTwoBorns,
                completedSpaces,
                totalSpaces,
                givenVoltages,
                voltagesRequired,
                matchVoltages,
                regulated);
        }
    }
}


