using Microsoft.Psi.Interop.Serialization;
using System.IO;

public class PsiFormatMatrix4x4
{
    public static Format<System.Numerics.Matrix4x4> GetFormat()
    {
        return new Format<System.Numerics.Matrix4x4>(WriteMatrix4x4, ReadMatrix4x4);
    }

    public static void WriteMatrix4x4(System.Numerics.Matrix4x4 matrix, BinaryWriter writer)
    {
        writer.Write((double)matrix.M11);
        writer.Write((double)matrix.M12);
        writer.Write((double)matrix.M13);
        writer.Write((double)matrix.M14);
        writer.Write((double)matrix.M21);
        writer.Write((double)matrix.M22);
        writer.Write((double)matrix.M23);
        writer.Write((double)matrix.M24);
        writer.Write((double)matrix.M31);
        writer.Write((double)matrix.M32);
        writer.Write((double)matrix.M33);
        writer.Write((double)matrix.M34);
        writer.Write((double)matrix.M41);
        writer.Write((double)matrix.M42);
        writer.Write((double)matrix.M43);
        writer.Write((double)matrix.M44);
    }

    public static System.Numerics.Matrix4x4 ReadMatrix4x4(BinaryReader reader)
    {
        System.Numerics.Matrix4x4 matrix = new System.Numerics.Matrix4x4();
        matrix.M11 = (float)reader.ReadDouble();
        matrix.M12 = (float)reader.ReadDouble();
        matrix.M13 = (float)reader.ReadDouble();
        matrix.M14 = (float)reader.ReadDouble();
        matrix.M21 = (float)reader.ReadDouble();
        matrix.M22 = (float)reader.ReadDouble();
        matrix.M23 = (float)reader.ReadDouble();
        matrix.M24 = (float)reader.ReadDouble();
        matrix.M31 = (float)reader.ReadDouble();
        matrix.M32 = (float)reader.ReadDouble();
        matrix.M33 = (float)reader.ReadDouble();
        matrix.M34 = (float)reader.ReadDouble();
        matrix.M41 = (float)reader.ReadDouble();
        matrix.M42 = (float)reader.ReadDouble();
        matrix.M43 = (float)reader.ReadDouble();
        matrix.M44 = (float)reader.ReadDouble();
        return matrix;
    }
}