using Microsoft.Psi.Interop.Serialization;
using System;
using System.IO;

public class PsiFormatPositionOrientation
{
    public static Format<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>> GetFormat()
    {
        return new Format<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>(WritePositionOrientation, ReadPositionOrientation);
    }

    public static void WritePositionOrientation(Tuple<System.Numerics.Vector3, System.Numerics.Vector3> point3D, BinaryWriter writer)
    {
        writer.Write((double)point3D.Item1.X);
        writer.Write((double)point3D.Item1.Y);
        writer.Write((double)point3D.Item1.Z);
        writer.Write((double)point3D.Item2.X);
        writer.Write((double)point3D.Item2.Y);
        writer.Write((double)point3D.Item2.Z);
    }

    public static Tuple<System.Numerics.Vector3, System.Numerics.Vector3> ReadPositionOrientation(BinaryReader reader)
    {
        return new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()), 
                        new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()));
    }
}