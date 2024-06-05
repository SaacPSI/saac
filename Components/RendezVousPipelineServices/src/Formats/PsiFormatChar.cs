using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.RendezVousPipelineServices
{
    public class PsiFormatChar : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<char>(WriteChar, ReadChar);
        }

        public void WriteChar(char character, BinaryWriter writer)
        {
            writer.Write(character);
        }

        public char ReadChar(BinaryReader reader)
        {
            return reader.ReadChar();
        }
    }
}
