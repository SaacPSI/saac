using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PipelineServices 
{ 
    public class PsiFormatBytes : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<byte[]>(WriteBytes, ReadBytes);
        }

        public void WriteBytes(byte[] image, BinaryWriter writer)
        {
            writer.Write(image.Length);
            writer.Write(image);
        }

        public byte[] ReadBytes(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            byte[] data = reader.ReadBytes(length);
            return data;
        }
    }
}
