using Microsoft.Psi.Interop.Serialization;
using SAAC.PipelineServices;
using System.IO;


namespace Casper.Formats
{
    internal class PsiFormatBat: IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<PsiBatterie>(WriteBoolean, ReadBoolean);
        }

        public void WriteBoolean(PsiBatterie data, BinaryWriter writer)
        {
            writer.Write(data.id);
            writer.Write(data.tension);
            writer.Write(data.places);
            writer.Write(data.regulated);
            for (int i = 0; i < data.places; i++)
                writer.Write(data.modules[i]);
            writer.Write(data.state);
            writer.Write((double)data.dist);
        }

        public PsiBatterie ReadBoolean(BinaryReader reader)
        {
            int id = reader.ReadInt32();
            int tension = reader.ReadInt32();
            int places = reader.ReadInt32();
            int[] module = new int[places];
            bool regulated = reader.ReadBoolean();
            for (int i = 0; i < places; i++)
            {
                module[i] = reader.ReadInt32();
            }
            string state = reader.ReadString();
            float dist = (float)reader.ReadDouble();

            return new PsiBatterie(id, tension, places, regulated, module, state, dist);
        }
    }
}
