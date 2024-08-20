using Microsoft.Psi.Interop.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class PsiFormatEyeTracking
{
    public static Format<Dictionary<ETData, IEyeTracking>> GetFormat()
    {
        return new Format<Dictionary<ETData, IEyeTracking>>(WriteEyeTracking, ReadEyeTracking);
    }

    public static void WriteEyeTracking(Dictionary<ETData, IEyeTracking> eyeTracking, BinaryWriter writer)
    {
        writer.Write(eyeTracking.Count);
        foreach(var item in eyeTracking)
        {
            writer.Write((int)item.Key);
            item.Value.Write(writer);
        }
    }

    public static Dictionary<ETData, IEyeTracking> ReadEyeTracking(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        Dictionary<ETData, IEyeTracking> dictionary = new Dictionary<ETData, IEyeTracking>(count);
        EyeTrackingTemplate template = new EyeTrackingTemplate();
        foreach (var item in template.content)
        {
            dictionary.Add((ETData)reader.ReadInt32(), item.Value.Read(reader));
        }
        return dictionary;
    }
}
