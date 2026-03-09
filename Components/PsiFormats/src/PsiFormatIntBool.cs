using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatIntBool
    {
        public static Format<(int, bool)> GetFormat()
        {
            return new Format<(int, bool)>(WriteIntBool, ReadIntBool);
        }

        public static void WriteIntBool((int, bool) value, BinaryWriter writer)
        {
            writer.Write(value.Item1);
            writer.Write(value.Item2);
        }

        public static (int, bool) ReadIntBool(BinaryReader reader)
        {
            int item1 = reader.ReadInt32();
            bool item2 = reader.ReadBoolean();
            return (item1, item2);
        }
    }
}
