using Microsoft.Psi.Interop.Serialization;

namespace SAAC.PsiFormats
{
    public class PsiFormatDateTime
    {
        public static Format<System.DateTime> GetFormat()
        {
            return new Format<System.DateTime>(WriteDateTime, ReadDateTime);
        }

        public static void WriteDateTime(System.DateTime dateTime, BinaryWriter writer)
        {
            writer.Write(dateTime.Ticks);
        }

        public static System.DateTime ReadDateTime(BinaryReader reader)
        {
            return new System.DateTime((long)reader.ReadUInt64());
        }
    }
}