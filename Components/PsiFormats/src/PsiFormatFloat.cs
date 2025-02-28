using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatFloat
    {
        public static Format<float> GetFormat()
        {
            return new Format<float>(WriteFloat, ReadFloat);
        }

        public static void WriteFloat(float value, BinaryWriter writer)
        {
            writer.Write(value);
        }

        public static float ReadFloat(BinaryReader reader)
        {
            return reader.ReadSingle();
        }
    }
}
