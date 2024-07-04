using Microsoft.Psi.Interop.Serialization;
using System.IO;


public class PsiFormatCommandString
{
    public static Format<(PsiPipelineManager.Command, string)> GetFormat()
    {
        return new Format<(PsiPipelineManager.Command, string)>(WriteIntString, ReadIntSring);
    }

    public static void WriteIntString((PsiPipelineManager.Command, string) data, BinaryWriter writer)
    {
        writer.Write((int)data.Item1);
        writer.Write(data.Item2);
    }

    public static (PsiPipelineManager.Command, string) ReadIntSring(BinaryReader reader)
    {
        return new ((PsiPipelineManager.Command)reader.ReadInt32(), reader.ReadString());
    }
}
