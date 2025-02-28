using Microsoft.Psi.Interop.Serialization;
using SAAC.PipelineServices;
using System.IO;

namespace Casper.Formats
{
    internal class PsiFormatTV: IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<(int, int, int, string)>(Write, Read);
        }

        public void Write((int, int, int, string) data, BinaryWriter writer)
        {
            writer.Write(data.Item1);
            writer.Write(data.Item2);
            writer.Write(data.Item3);
            writer.Write(data.Item4);
        }

        public (int, int, int, string) Read(BinaryReader reader)
        {
            return new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadString());
        }
    }
}
