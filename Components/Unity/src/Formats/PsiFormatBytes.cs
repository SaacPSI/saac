using Microsoft.Psi.Imaging;
using Microsoft.Psi.Interop.Serialization;
using System;
using System.IO;

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
