using Microsoft.Psi.Interop.Serialization;
using SAAC.PipelineServices;
using System.IO;

namespace Casper.Formats
{
    internal class PsiFormatBoolVector3: IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<(bool, System.Numerics.Vector3)>(Write, Read);
        }

        public void Write((bool, System.Numerics.Vector3) data, BinaryWriter writer)
        {
            writer.Write(data.Item1);
            writer.Write((double)data.Item2.X);
            writer.Write((double)data.Item2.Y);
            writer.Write((double)data.Item2.Z);
        }

        public (bool, System.Numerics.Vector3) Read(BinaryReader reader)
        {
            return new(reader.ReadBoolean(), new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()));
        }
    }
}
