using MathNet.Spatial.Euclidean;
using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.RendezVousPipelineServices
{
    public class PsiFormatRay : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<MathNet.Spatial.Euclidean.Ray3D>(WriteRay3D, ReadRay3D);
        }

        public void WriteRay3D(Ray3D ray3D, BinaryWriter writer)
        {
            writer.Write(ray3D.ThroughPoint.X);
            writer.Write(ray3D.ThroughPoint.Y);
            writer.Write(ray3D.ThroughPoint.Z);
            writer.Write(ray3D.Direction.X);
            writer.Write(ray3D.Direction.Y);
            writer.Write(ray3D.Direction.Z);
        }

        public Ray3D ReadRay3D(BinaryReader reader)
        {
            return new Ray3D(new Point3D(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble()), UnitVector3D.Create(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble()));
        }
    }
}
