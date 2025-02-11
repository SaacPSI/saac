using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatTupleOfVector
    {
        public static Format<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>> GetFormat()
        {
            return new Format<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>(WriteTupleOfVector, ReadTupleOfVector);
        }

        public static void WriteTupleOfVector(Tuple<System.Numerics.Vector3, System.Numerics.Vector3> point3D, BinaryWriter writer)
        {
            writer.Write(point3D.Item1.X);
            writer.Write(point3D.Item1.Y);
            writer.Write(point3D.Item1.Z);
            writer.Write(point3D.Item2.X);
            writer.Write(point3D.Item2.Y);
            writer.Write(point3D.Item2.Z);
        }

        public static Tuple<System.Numerics.Vector3, System.Numerics.Vector3> ReadTupleOfVector(BinaryReader reader)
        {
            return new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(new System.Numerics.Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                            new System.Numerics.Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        }
    }
}