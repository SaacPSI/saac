using Microsoft.Psi.Interop.Serialization;

namespace SAAC.PsiFormats
{
    public class PsiFormatBytes
    {
        public static Format<byte[]> GetFormat()
        {
            return new Format<byte[]>(WriteBytes, ReadBytes);
        }

        public static void WriteBytes(byte[] image, BinaryWriter writer)
        {
            writer.Write(image.Length);
            writer.Write(image);
        }

        public static byte[] ReadBytes(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            byte[] data = reader.ReadBytes(length);
            return data;
        }
    }
}
