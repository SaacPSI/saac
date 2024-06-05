using Microsoft.Psi.Interop.Serialization;
using System.IO;

public class PsiFormatInteger
{
    public static Format<int> GetFormat()
    {
        return new Format<int>(WriteInteger, ReadInteger);
    }

    public static void WriteInteger(int integer, BinaryWriter writer)
    {
        writer.Write(integer);
    }

    public static int ReadInteger(BinaryReader reader)
    {
        return reader.Read();
    }
}