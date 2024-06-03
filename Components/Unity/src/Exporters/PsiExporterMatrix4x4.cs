using System;
using System.Numerics;
using UnityEngine;

public class PsiExporterMatrix4x4: PsiExporter<System.Numerics.Matrix4x4>
{
    private System.Numerics.Matrix4x4 PreviousMatrix4x4;

    void Update()
    {
        System.Numerics.Matrix4x4 newMatrix = new System.Numerics.Matrix4x4(transform.worldToLocalMatrix[0, 0], transform.worldToLocalMatrix[0, 1], transform.worldToLocalMatrix[0, 2], transform.worldToLocalMatrix[0, 3],
                                                                            transform.worldToLocalMatrix[1, 0], transform.worldToLocalMatrix[1, 1], transform.worldToLocalMatrix[1, 2], transform.worldToLocalMatrix[1, 3],
                                                                            transform.worldToLocalMatrix[2, 0], transform.worldToLocalMatrix[2, 1], transform.worldToLocalMatrix[2, 2], transform.worldToLocalMatrix[2, 3],
                                                                            transform.worldToLocalMatrix[3, 0], transform.worldToLocalMatrix[3, 1], transform.worldToLocalMatrix[3, 2], transform.worldToLocalMatrix[3, 3]);
        if (CanSend() && PreviousMatrix4x4 != newMatrix)
        {
            Out.Post(newMatrix, GetCurrentTime());
            PreviousMatrix4x4 = newMatrix;
        }
    }

#if PLATFORM_ANDROID
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<System.Numerics.Matrix4x4> GetSerializer()
    {
        return PsiFormatMatrix4x4.GetFormat();
    }
#endif
}