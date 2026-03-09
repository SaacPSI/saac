using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatIntBoolString
    {
        public static Format<(int, bool, string)> GetFormat()
        {
            return new Format<(int, bool, string)>(WriteIntBoolString, ReadIntBoolString);
        }

        public static void WriteIntBoolString((int, bool, string) value, BinaryWriter writer)
        {
            writer.Write(value.Item1);
            writer.Write(value.Item2);
            writer.Write(value.Item3);
        }

        public static (int, bool, string) ReadIntBoolString(BinaryReader reader)
        {
            int item1 = reader.ReadInt32();
            bool item2 = reader.ReadBoolean();
            string item3 = reader.ReadString();
            return (item1, item2, item3);
        }
    }
}
