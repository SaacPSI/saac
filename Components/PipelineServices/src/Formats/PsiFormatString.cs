using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PipelineServices
{
    public class PsiFormatString : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<string>(WriteString, ReadSring);
        }

        public void WriteString(string data, BinaryWriter writer)
        {
            writer.Write(data);
        }

        public string ReadSring(BinaryReader reader)
        {
            return reader.ReadString();
        }
    }
}
