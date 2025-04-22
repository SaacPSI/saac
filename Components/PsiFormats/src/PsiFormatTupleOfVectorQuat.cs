using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatTupleOfVectorQuat
    {
        public static Format<Tuple<System.Numerics.Vector3, System.Numerics.Quaternion>> GetFormat()
        {
            return new Format<Tuple<System.Numerics.Vector3, System.Numerics.Quaternion>>(WriteTupleOfVectorQuat, ReadTupleOfVectorQuat);
        }

        public static void WriteTupleOfVectorQuat(Tuple<System.Numerics.Vector3, System.Numerics.Quaternion> point3D, BinaryWriter writer)
        {
            writer.Write(point3D.Item1.X);
            writer.Write(point3D.Item1.Y);
            writer.Write(point3D.Item1.Z);
            writer.Write(point3D.Item2.X);
            writer.Write(point3D.Item2.Y);
            writer.Write(point3D.Item2.Z);
            writer.Write(point3D.Item2.W);
        }

        public static Tuple<System.Numerics.Vector3, System.Numerics.Quaternion> ReadTupleOfVectorQuat(BinaryReader reader)
        {
            return new Tuple<System.Numerics.Vector3, System.Numerics.Quaternion>(new System.Numerics.Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                            new System.Numerics.Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        }
    }
}