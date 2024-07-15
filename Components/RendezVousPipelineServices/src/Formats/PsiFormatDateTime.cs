using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.RendezVousPipelineServices
{
    public class PsiFormatDateTime : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<System.DateTime>(WriteDateTime, ReadDateTime);
        }

        public void WriteDateTime(System.DateTime dateTime, BinaryWriter writer)
        {
            writer.Write(dateTime.ToBinary());
        }

        public System.DateTime ReadDateTime(BinaryReader reader)
        {
            return System.DateTime.FromBinary(reader.ReadInt64());
        }
    }
}