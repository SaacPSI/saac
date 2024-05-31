using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.RendezVousPipelineServices
{
    public class PsiFormatCoordinateSystem : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<MathNet.Spatial.Euclidean.CoordinateSystem>(WriteCoordinateSystem, ReadCoordinateSystem);
        }

        public void WriteCoordinateSystem(MathNet.Spatial.Euclidean.CoordinateSystem system, BinaryWriter writer)
        {
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    writer.Write(system.At(i, j));
        }

        public MathNet.Spatial.Euclidean.CoordinateSystem ReadCoordinateSystem(BinaryReader reader)
        {
            MathNet.Spatial.Euclidean.CoordinateSystem system = new MathNet.Spatial.Euclidean.CoordinateSystem();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    system.At(i, j, reader.ReadDouble());
            return system;
        }
    }
}