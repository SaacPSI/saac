using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC
{
    namespace PsiFormats
    {
        public class PsiFormatCommand
        {
            public static Format<(PipelineServices.RendezVousPipeline.Command, string)> GetFormat()
            {
                return new Format<(PipelineServices.RendezVousPipeline.Command, string)>(WriteIntString, ReadIntSring);
            }

            public static void WriteIntString((PipelineServices.RendezVousPipeline.Command, string) data, BinaryWriter writer)
            {
                writer.Write((int)data.Item1);
                writer.Write(data.Item2);
            }

            public static (PipelineServices.RendezVousPipeline.Command, string) ReadIntSring(BinaryReader reader)
            {
                return new((PipelineServices.RendezVousPipeline.Command)reader.ReadInt32(), reader.ReadString());
            }
        }
    }
}

