using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatBat
    {
        public static Format<PsiBatterie> GetFormat()
        {
            return new Format<PsiBatterie>(WriteBatterie, ReadBatterie);
        }

        public static void WriteBatterie(PsiBatterie value, BinaryWriter writer)
        {
            writer.Write(value.Id);
            writer.Write(value.Tension);
            writer.Write(value.Places);
            writer.Write(value.Regulated);
            // Write array: length first, then elements
            if (value.Modules == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(value.Modules.Length);
                foreach (int module in value.Modules)
                {
                    writer.Write(module);
                }
            }
            writer.Write(value.State ?? string.Empty);
            writer.Write(value.Dist);
        }

        public static PsiBatterie ReadBatterie(BinaryReader reader)
        {
            int id = reader.ReadInt32();
            int tension = reader.ReadInt32();
            int places = reader.ReadInt32();
            bool regulated = reader.ReadBoolean();
            // Read array: length first, then elements
            int modulesLength = reader.ReadInt32();
            int[] modules = new int[modulesLength];
            for (int i = 0; i < modulesLength; i++)
            {
                modules[i] = reader.ReadInt32();
            }
            string state = reader.ReadString();
            float dist = reader.ReadSingle();
            return new PsiBatterie(id, tension, places, regulated, modules, state, dist);
        }
    }
}

