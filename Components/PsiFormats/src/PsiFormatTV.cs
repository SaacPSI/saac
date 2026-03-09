using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatTV
    {
        public static Format<PsiTV> GetFormat()
        {
            return new Format<PsiTV>(WriteTV, ReadTV);
        }

        public static void WriteTV(PsiTV value, BinaryWriter writer)
        {
            writer.Write(value.Id);
            writer.Write(value.Val1);
            writer.Write(value.Val2);
            writer.Write(value.Message);
        }

        public static PsiTV ReadTV(BinaryReader reader)
        {
            int id = reader.ReadInt32();
            int val1 = reader.ReadInt32();
            int val2 = reader.ReadInt32();
            string message = reader.ReadString();
            return new PsiTV(id, val1, val2, message);
        }
    }
}
