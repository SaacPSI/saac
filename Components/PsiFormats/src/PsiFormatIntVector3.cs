using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatIntVector3
    {
        public static Format<(int, System.Numerics.Vector3)> GetFormat()
        {
            return new Format<(int, System.Numerics.Vector3)>(WriteIntVector3, ReadIntVector3);
        }

        public static void WriteIntVector3((int, System.Numerics.Vector3) value, BinaryWriter writer)
        {
            writer.Write(value.Item1);
            writer.Write(value.Item2.X);
            writer.Write(value.Item2.Y);
            writer.Write(value.Item2.Z);
        }

        public static (int, System.Numerics.Vector3) ReadIntVector3(BinaryReader reader)
        {
            int item1 = reader.ReadInt32();
            System.Numerics.Vector3 item2 = new System.Numerics.Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            return (item1, item2);
        }
    }
}
