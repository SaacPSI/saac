using Microsoft.Psi.Interop.Serialization;
using SAAC.PipelineServices;
using System.IO;

namespace Casper.Formats
{
    internal class PsiFormatIntBoolString: IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<(int, bool, string)>(Write, Read);
        }

        public void Write((int, bool, string) data, BinaryWriter writer)
        {
            writer.Write(data.Item1);
            writer.Write(data.Item2);
            writer.Write(data.Item3);
        }

        public (int, bool, string) Read(BinaryReader reader)
        {
            return new(reader.ReadInt32(), reader.ReadBoolean(), reader.ReadString());
        }
    }
}
