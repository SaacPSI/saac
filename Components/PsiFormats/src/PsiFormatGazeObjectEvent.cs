using Microsoft.Psi.Interop.Serialization;
using SAAC.GlobalHelpers;

namespace SAAC.PsiFormats
{
    /// <summary>
    /// Provides serialization format for SAAC.GlobalHelpers.GrabEvent type.
    /// </summary>
    public class PsiFormatGazeObjectEvent
    {
        public static Format<ObjectGazeEvent> GetFormat()
        {
            return new Format<ObjectGazeEvent>(WriteGazeEvent, ReadGazeEvent);
        }

        public static void WriteGazeEvent(ObjectGazeEvent gazeEvent, BinaryWriter writer)
        {
            writer.Write(gazeEvent.userID);
            writer.Write(gazeEvent.objectID);
            writer.Write(gazeEvent.type);
            writer.Write(gazeEvent.status);
        }

        public static ObjectGazeEvent ReadGazeEvent(BinaryReader reader)
        {
            int gazerid = reader.ReadInt32();
            string objectid = reader.ReadString();
            string type = reader.ReadString();
            bool status = reader.ReadBoolean();

            return new ObjectGazeEvent(type, gazerid, objectid, status);
        }
    }
}
