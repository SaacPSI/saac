using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatIntString
    {
        public static Format<(int, string)> GetFormat()
        {
            return new Format<(int, string)>(WriteIntString, ReadIntString);
        }

        public static void WriteIntString((int, string) value, BinaryWriter writer)
        {
            writer.Write(value.Item1);
            writer.Write(value.Item2);
        }

        public static (int, string) ReadIntString(BinaryReader reader)
        {
            int item1 = reader.ReadInt32();
            string item2 = reader.ReadString();
            return (item1, item2);
        }
    }
}
