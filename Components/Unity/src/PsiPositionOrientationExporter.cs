using System;
using System.IO;
using Microsoft.Psi.Interop.Serialization;
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
#if HOLOLENS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer GetSerializer()
    {
        return new Format<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>(WritePositionOrientation, ReadPositionOrientation);
    }

    public void WritePositionOrientation(Tuple<System.Numerics.Vector3, System.Numerics.Vector3> point3D, BinaryWriter writer)
    {
        writer.Write(point3D.Item1.X);
        writer.Write(point3D.Item1.Y);
        writer.Write(point3D.Item1.Z);
        writer.Write(point3D.Item2.X);
        writer.Write(point3D.Item2.Y);
        writer.Write(point3D.Item2.Z);
    }

    public Tuple<System.Numerics.Vector3, System.Numerics.Vector3> ReadPositionOrientation(BinaryReader reader)
    {
        float x = (float)reader.ReadDouble();
        float y = (float)reader.ReadDouble();
        float z = (float)reader.ReadDouble();
        float a = (float)reader.ReadDouble();
        float t = (float)reader.ReadDouble();
        float g = (float)reader.ReadDouble();
        return new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(new System.Numerics.Vector3(x, y, z), new System.Numerics.Vector3(a, t, g));
    }
#endif
}