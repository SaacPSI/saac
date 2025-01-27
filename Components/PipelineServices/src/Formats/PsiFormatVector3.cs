using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PipelineServices
{
    public class PsiFormatVector3 : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<System.Numerics.Vector3>(WriteVector3, ReadVector3);
        }

        public void WriteVector3(System.Numerics.Vector3 point3D, BinaryWriter writer)
        {
            writer.Write(point3D.X);
            writer.Write(point3D.Y);
            writer.Write(point3D.Z);
        }

        public System.Numerics.Vector3 ReadVector3(BinaryReader reader)
        {
            return new System.Numerics.Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}