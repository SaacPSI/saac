using Microsoft.Psi.Interop.Serialization;
using System;
using System.IO;

public class PsiFormatString
{
    public static Format<string> GetFormat()
    {
        return new Format<string>(WriteString, ReadSring);
    }

    public static void WriteString(string data, BinaryWriter writer)
    {
        writer.Write(data);
    }

    public static string ReadSring(BinaryReader reader)
    {
        return reader.ReadString();
    }
} 