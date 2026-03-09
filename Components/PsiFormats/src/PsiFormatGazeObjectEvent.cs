using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.PsiFormats
{
    public class PsiFormatGazeObjectEvent
    {
        public static Format<PsiGazeObjectEvent> GetFormat()
        {
            return new Format<PsiGazeObjectEvent>(WriteGazeObjectEvent, ReadGazeObjectEvent);
        }

        public static void WriteGazeObjectEvent(PsiGazeObjectEvent value, BinaryWriter writer)
        {
            writer.Write(value.UserId);
            writer.Write(value.ObjectId);
            writer.Write(value.IsGazing);
            writer.Write(value.ObjectType ?? string.Empty);
        }

        public static PsiGazeObjectEvent ReadGazeObjectEvent(BinaryReader reader)
        {
            int userId = reader.ReadInt32();
            int objectId = reader.ReadInt32();
            bool isGazing = reader.ReadBoolean();
            string objectType = reader.ReadString();

            return new PsiGazeObjectEvent(userId, objectId, isGazing, objectType);
        }
    }
}

