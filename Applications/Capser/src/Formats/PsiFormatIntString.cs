using Microsoft.Psi.Interop.Serialization;
using SAAC.PipelineServices;
using System.IO;

namespace Casper.Formats
{
    public class PsiFormatIntString : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<(int,string)>(WriteIntString, ReadIntSring);
        }

        public void WriteIntString((int, string) data, BinaryWriter writer)
        {
            writer.Write(data.Item1);
            writer.Write(data.Item2);
        }

        public (int, string) ReadIntSring(BinaryReader reader)
        {
            return new (reader.ReadInt32(), reader.ReadString());
        }
    }
}