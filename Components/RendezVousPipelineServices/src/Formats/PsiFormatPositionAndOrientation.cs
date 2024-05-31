using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.RendezVousPipelineServices
{
    public class PsiFormatPositionAndOrientation : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>(WritePositionOrientation, ReadPositionOrientation);
        }

        public void WritePositionOrientation(Tuple<System.Numerics.Vector3, System.Numerics.Vector3> point3D, BinaryWriter writer)
        {
            writer.Write((double)point3D.Item1.X);
            writer.Write((double)point3D.Item1.Y);
            writer.Write((double)point3D.Item1.Z);
            writer.Write((double)point3D.Item2.X);
            writer.Write((double)point3D.Item2.Y);
            writer.Write((double)point3D.Item2.Z);
        }

        public Tuple<System.Numerics.Vector3, System.Numerics.Vector3> ReadPositionOrientation(BinaryReader reader)
        {
            return new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()),
                            new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()));
        }
    }
}
