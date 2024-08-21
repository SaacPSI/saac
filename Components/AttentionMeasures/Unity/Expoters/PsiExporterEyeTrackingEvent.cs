using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PsiExporterEyeTrackingEvent : PsiExporter<EyeTrackingEvent>
{
    public void BeginningExperiment()
    {
        
        Out.Post(new EyeTrackingEvent(EyeTrackingEvent.EventType.BeginningExperiment), GetCurrentTime());
    }    
    
    public void EndingExperiment()
    {
        
        Out.Post(new EyeTrackingEvent(EyeTrackingEvent.EventType.EndingExperiment), GetCurrentTime());
    }

#if PSI_TCP_SOURCE
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<EyeTrackingEvent> GetSerializer()
    {
        return PsiFormatEyeTrackingEvent.GetFormat();
    }
#endif
}
