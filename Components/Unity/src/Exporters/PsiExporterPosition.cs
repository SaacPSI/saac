using System;
using UnityEngine;
using SAAC.PsiFormats;

public class PsiExporterPosition
    : PsiExporter<System.Numerics.Vector3>
{
    private UnityEngine.Vector3 PreviousPosition = Vector3.down;

    public Transform TransformToExport;
    public bool isLocal = false;

    private void Start()
    {
        if (TransformToExport == null)
            TransformToExport = this.transform;
        base.Start();
    }

    void Update()
    {
        var position;
        if (isLocal)
        {
            position = TransformToExport.localPosition;
        }
        else
        {
            position = TransformToExport.position;
        } 
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
