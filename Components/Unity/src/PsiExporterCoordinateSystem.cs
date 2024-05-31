using System;
using UnityEngine;

public class PsiExporterPositionOrientation
    : PsiExporter<MathNet.Spatial.Euclidean.CoordinateSystem>
{
    private MathNet.Spatial.Euclidean.CoordinateSystem PreviousCoordinateSystem;

    void Update()
    {
        MathNet.Spatial.Euclidean.CoordinateSystem newSystem = new MathNet.Spatial.Euclidean.CoordinateSystem(transform);
        if (CanSend() && PreviousCoordinateSystem != newSystem)
        {
            Out.Post(newSystem, GetCurrentTime());
            PreviousCoordinateSystem = newSystem;
        }
    }

#if PLATFORM_ANDROID
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<MathNet.Spatial.Euclidean.CoordinateSystem> GetSerializer()
    {
        return PsiFormatCoordinateSystem.GetFormat();
    }
#endif
}