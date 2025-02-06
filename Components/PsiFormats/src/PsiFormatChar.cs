using Microsoft.Psi.Interop.Serialization;

namespace SAAC.PsiFormats
{
    public class PsiFormatChar
    {
        public static Format<char> GetFormat()
        {
            return new Format<char>(WriteChar, ReadChar);
        }

        public static void WriteChar(char character, BinaryWriter writer)
        {
            writer.Write(character);
        }

        public static char ReadChar(BinaryReader reader)
        {
            return reader.ReadChar();
        }
    }
}
