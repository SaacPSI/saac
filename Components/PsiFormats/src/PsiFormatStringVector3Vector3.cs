using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatStringVector3Vector3
    {
        public static Format<(string, System.Numerics.Vector3, System.Numerics.Vector3)> GetFormat()
        {
            return new Format<(string, System.Numerics.Vector3, System.Numerics.Vector3)>(Write, Read);
        }

        public static void Write((string, System.Numerics.Vector3, System.Numerics.Vector3) data, BinaryWriter writer)
        {
            writer.Write(data.Item1);
            writer.Write(data.Item2.X);
            writer.Write(data.Item2.Y);
            writer.Write(data.Item2.Z);
            writer.Write(data.Item3.X);
            writer.Write(data.Item3.Y);
            writer.Write(data.Item3.Z);
        }

        public static (string, System.Numerics.Vector3, System.Numerics.Vector3) Read(BinaryReader reader)
        {
            return new(reader.ReadString(),
            new System.Numerics.Vector3((float)reader.ReadSingle(), (float)reader.ReadSingle(), (float)reader.ReadSingle()),
            new System.Numerics.Vector3((float)reader.ReadSingle(), (float)reader.ReadSingle(), (float)reader.ReadSingle()));
        }
    }
}
