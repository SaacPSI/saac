using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.RendezVousPipelineServices
{
    public class PsiFormatCommand : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<(RendezVousPipeline.Command, string)>(WriteIntString, ReadIntSring);
        }

        public void WriteIntString((RendezVousPipeline.Command, string) data, BinaryWriter writer)
        {
            writer.Write((int)data.Item1);
            writer.Write(data.Item2);
        }

        public (RendezVousPipeline.Command, string) ReadIntSring(BinaryReader reader)
        {
            return new ((RendezVousPipeline.Command)reader.ReadInt32(), reader.ReadString());
        }
    }
}