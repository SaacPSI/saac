using System;
using UnityEngine;

public class PsiExporterPosition
    : PsiExporter<System.Numerics.Vector3>
{
    private DateTime Timestamp = DateTime.UtcNow;
    private UnityEngine.Vector3 PreviousPosition = Vector3.down;
    
    void Update()
    {
        var now = GetCurrentTime();
        var position = gameObject.transform.position;
        if (CanSend() && Timestamp != now && position != PreviousPosition)
        {
            Out.Post(new System.Numerics.Vector3(position.x, position.y, position.z), now);
            Timestamp = now;
            PreviousPosition = position;
        }
    }

#if PLATFORM_ANDROID
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<System.Numerics.Vector3> GetSerializer()
    { 
        return PsiFormatVector3.GetFormat();
    }
#endif
}