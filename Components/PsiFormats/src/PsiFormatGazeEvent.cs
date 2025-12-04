using Microsoft.Psi.Interop.Serialization;
using SAAC.GlobalHelpers;

namespace SAAC.PsiFormats
{
    public class PsiFormatGazeEvent
    {
        public static Format<GazeEvent> GetFormat()
        {
            return new Format<GazeEvent>(WriteGazeEvent, ReadGazeEvent);
        }

        public static void WriteGazeEvent(GazeEvent gazeEvent, BinaryWriter writer)
        {
            writer.Write(gazeEvent.UserID);
            writer.Write(gazeEvent.ObjectID);
            writer.Write((int)gazeEvent.Type);
            writer.Write(gazeEvent.IsGazed);
        }

        public static GazeEvent ReadGazeEvent(BinaryReader reader)
        {
            string gazerid = reader.ReadString();
            string objectid = reader.ReadString();
            int type = reader.ReadInt32();
            bool status = reader.ReadBoolean();

            return new GazeEvent((EEventType)type, gazerid, objectid, status);
        }
    }
}