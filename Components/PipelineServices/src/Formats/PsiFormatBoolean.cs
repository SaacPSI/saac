using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PipelineServices
{
    public class PsiFormatBoolean : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<bool>(WriteBoolean, ReadBoolean);
        }

        public void WriteBoolean(bool boolean, BinaryWriter writer)
        {
            writer.Write(boolean);
        }

        public bool ReadBoolean(BinaryReader reader)
        {
            return reader.ReadBoolean();
        }
    }
}
