using Microsoft.Psi.Interop.Serialization;
using System.IO;

public class PsiFormatVector3
{
    public static Format<System.Numerics.Vector3> GetFormat()
    {
        return new Format<System.Numerics.Vector3>(WriteVector3, ReadVector3);
    }

    public static void WriteVector3(System.Numerics.Vector3 point3D, BinaryWriter writer)
    {
        writer.Write(point3D.X);
        writer.Write(point3D.Y);
        writer.Write(point3D.Z);
    }

    public static System.Numerics.Vector3 ReadVector3(BinaryReader reader)
    {
        return new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble());
    }
}
