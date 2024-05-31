using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.RendezVousPipelineServices
{
    public class PsiFormatInt : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<int>(WriteInt, ReadInt);
        }

        public void WriteInt(int integer, BinaryWriter writer)
        {
            writer.Write(integer);
        }

        public int ReadInt(BinaryReader reader)
        {
            return reader.ReadInt32();
        }
    }
