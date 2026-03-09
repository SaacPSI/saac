using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatBoolVector3
    {
        public static Format<(bool, System.Numerics.Vector3)> GetFormat()
        {
            return new Format<(bool, System.Numerics.Vector3)>(WriteBoolVector3, ReadBoolVector3);
        }

        public static void WriteBoolVector3((bool, System.Numerics.Vector3) value, BinaryWriter writer)
        {
            writer.Write(value.Item1);
            writer.Write(value.Item2.X);
            writer.Write(value.Item2.Y);
            writer.Write(value.Item2.Z);
        }

        public static (bool, System.Numerics.Vector3) ReadBoolVector3(BinaryReader reader)
        {
            bool item1 = reader.ReadBoolean();
            System.Numerics.Vector3 item2 = new System.Numerics.Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            return (item1, item2);
        }
    }
}
