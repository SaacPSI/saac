using System;
using UnityEngine;
using SAAC.PsiFormats;

public class PsiExporterPosition
    : PsiExporter<System.Numerics.Vector3>
{
    private UnityEngine.Vector3 PreviousPosition = Vector3.down;
    
    void Update()
    {
        var position = gameObject.transform.position;
        if (CanSend() && position != PreviousPosition)
        {
            Out.Post(new System.Numerics.Vector3(position.x, position.y, position.z), GetCurrentTime());
            PreviousPosition = position;
        }
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<System.Numerics.Vector3> GetSerializer()
    { 
        return PsiFormatVector3.GetFormat();
    }
#endif
}