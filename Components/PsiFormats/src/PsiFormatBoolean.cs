using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatBoolean
    {
        public static Format<bool> GetFormat()
        {
            return new Format<bool>(WriteBoolean, ReadBoolean);
        }

        public static void WriteBoolean(bool boolean, BinaryWriter writer)
        {
            writer.Write(boolean);
        }

        public static bool ReadBoolean(BinaryReader reader)
        {
            return reader.ReadBoolean();
        }
    }
}
