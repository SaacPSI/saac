using System;
using UnityEngine;
using SAAC.PsiFormats;

public class PsiExporterPositionOrientation : PsiExporter<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>
{
    private UnityEngine.Vector3 PreviousPosition = Vector3.zero;
    private UnityEngine.Vector3 PreviousOrientation = Vector3.zero;
    public bool isLocal = false;
    public Transform TransformToExport;

    private void Start()
    {
        if (TransformToExport == null)
            TransformToExport = this.transform;
        base.Start();
    }
    void Update()
    {
        var position = Vector3.zero;
        var orientation = Vector3.zero;
        if (isLocal)
        {
            position = TransformToExport.localPosition;
            orientation = TransformToExport.localEulerAngles;
        }
        else
        {
            position = TransformToExport.position;
            orientation = TransformToExport.eulerAngles;
        }

        if (CanSend() && PreviousPosition != position && PreviousOrientation != orientation)
        {
            Out.Post(new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(new System.Numerics.Vector3(position.x, position.y, position.z), new System.Numerics.Vector3(orientation.x, orientation.y, orientation.z)), Timestamp);
            PreviousPosition = position;
            PreviousOrientation = orientation;
        }
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>> GetSerializer()
    {
        return PsiFormatPositionAndOrientation.GetFormat();
    }
#endif
}