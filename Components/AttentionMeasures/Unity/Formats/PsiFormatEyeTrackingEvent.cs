using Microsoft.Psi.Interop.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class PsiFormatEyeTrackingEvent
{
    public static Format<EyeTrackingEvent> GetFormat()
    {
        return new Format<EyeTrackingEvent>(WriteEyeTrackingEvent, ReadEyeTrackingEvent);
    }

    public static void WriteEyeTrackingEvent(EyeTrackingEvent eyeTrackingEvent, BinaryWriter writer)
    {
        writer.Write((int) eyeTrackingEvent.eventType);
    }

    public static EyeTrackingEvent ReadEyeTrackingEvent(BinaryReader reader)
    {
        return new EyeTrackingEvent((EyeTrackingEvent.EventType) reader.ReadInt32());
    }
}
