using System;
using UnityEngine;

public class PsiPositionOrientationExporter : PsiExporter<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>
{
    private DateTime Timestamp = DateTime.UtcNow;
    private UnityEngine.Vector3 PreviousPosition = Vector3.down;
    private UnityEngine.Vector3 PreviousOrientation = Vector3.down;

    void Update()
    {
        var now = GetCurrentTime();
        var position = gameObject.transform.position;
        var orientation = gameObject.transform.eulerAngles;
        if (CanSend() && Timestamp != now && position != PreviousPosition && PreviousOrientation != orientation)
        {
            Out.Post(new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(new System.Numerics.Vector3(position.x, position.y, position.z), new System.Numerics.Vector3(orientation.x, orientation.y, orientation.z)), now);
            Timestamp = now;
            PreviousPosition = position;
            PreviousOrientation = orientation;
        }
    }

#if PLATFORM_ANDROID
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>> GetSerializer()
    {
        return PsiFormatPositionAndOrientation.GetFormat();
    }
#endif
}