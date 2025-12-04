using Microsoft.Psi.Interop.Serialization;
using SAAC.GlobalHelpers;

namespace SAAC.PsiFormats
{
    public class PsiFormatGrabEvent
    {
        public static Format<GrabEvent> GetFormat()
        {
            return new Format<GrabEvent>(WriteGrabEvent, ReadGrabEvent);
        }

        public static void WriteGrabEvent(GrabEvent grabEvent, BinaryWriter writer)
        {
            writer.Write(grabEvent.UserID);
            writer.Write(grabEvent.ObjectID);
            writer.Write((int)grabEvent.Type);
            writer.Write(grabEvent.IsGrabbed);
        }

        public static GrabEvent ReadGrabEvent(BinaryReader reader)
        {
            string grabberid = reader.ReadString();
            string objectid = reader.ReadString();
            int type = reader.ReadInt32();
            bool status = reader.ReadBoolean();

            return new GrabEvent((EEventType)type, grabberid, objectid, status);
        }
    }
}