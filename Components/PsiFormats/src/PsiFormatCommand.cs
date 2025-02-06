using Microsoft.Psi.Interop.Serialization;

namespace SAAC
{
    namespace PsiFormats
    {
        public class PsiFormatCommand
        {
            public static Format<(int, string)> GetFormat()
            {
                return new Format<(int, string)>(WriteIntString, ReadIntSring);
            }

            public static void WriteIntString((int, string) data, BinaryWriter writer)
            {
                writer.Write(data.Item1);
                writer.Write(data.Item2);
            }

            public static (int, string) ReadIntSring(BinaryReader reader)
            {
                return new(reader.ReadInt32(), reader.ReadString());
            }
        }
    }
}

