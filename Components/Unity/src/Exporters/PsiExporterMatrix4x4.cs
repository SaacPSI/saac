using System;
using System.Numerics;
using UnityEngine;
using SAAC.PsiFormats;

public class PsiExporterMatrix4x4: PsiExporter<System.Numerics.Matrix4x4>
{
    public Transform TransformToExport;
    private System.Numerics.Matrix4x4 PreviousMatrix4x4;

    private void Start()
    {
        if (TransformToExport == null)
            TransformToExport = this.transform;
        base.Start();
    }

    void Update()
    {
        System.Numerics.Matrix4x4 newMatrix = new System.Numerics.Matrix4x4(TransformToExport.worldToLocalMatrix[0, 0], TransformToExport.worldToLocalMatrix[1, 0], TransformToExport.worldToLocalMatrix[2, 0], TransformToExport.worldToLocalMatrix[3, 0],
                                                                            TransformToExport.worldToLocalMatrix[0, 1], TransformToExport.worldToLocalMatrix[1, 1], TransformToExport.worldToLocalMatrix[2, 1], TransformToExport.worldToLocalMatrix[3, 1],
                                                                            TransformToExport.worldToLocalMatrix[0, 2], TransformToExport.worldToLocalMatrix[1, 2], TransformToExport.worldToLocalMatrix[2, 2], TransformToExport.worldToLocalMatrix[3, 2],
                                                                            TransformToExport.worldToLocalMatrix[0, 3], TransformToExport.worldToLocalMatrix[1, 3], TransformToExport.worldToLocalMatrix[2, 3], TransformToExport.worldToLocalMatrix[3, 3]);
        if (CanSend() && PreviousMatrix4x4 != newMatrix)
        {
            Out.Post(newMatrix, Timestamp);
            PreviousMatrix4x4 = newMatrix;
        }
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<System.Numerics.Matrix4x4> GetSerializer()
    {
        return PsiFormatMatrix4x4.GetFormat();
    }
#endif
}