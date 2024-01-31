using System;
using System.IO;
using Microsoft.Psi.Interop.Serialization;
using UnityEngine;

public class PsiPositionExporter : PsiExporter<System.Numerics.Vector3>
{
    private DateTime Timestamp = DateTime.UtcNow;
    private UnityEngine.Vector3 PreviousPosition = Vector3.down;
    
    void Update()
    {
        var now = GetCurrentTime();
        var position = gameObject.transform.position;
        if (CanSend() && Timestamp != now && position != PreviousPosition)
        {
            Out.Post(new System.Numerics.Vector3(position.x, position.y, position.z), DateTime.UtcNow);
            Timestamp = now;
            PreviousPosition = position;
        }
    }

    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer GetSerializer()
    { 
        return new Format<System.Numerics.Vector3>(WritePosition3D, ReadPosition3D);
    }

    public void WritePosition3D(System.Numerics.Vector3 point3D, BinaryWriter writer)
    {
        writer.Write(point3D.X);
        writer.Write(point3D.Y);
        writer.Write(point3D.Z);
    }

    public System.Numerics.Vector3 ReadPosition3D(BinaryReader reader)
    {
        float x = (float)reader.ReadDouble();
        float y = (float)reader.ReadDouble();
        float z = (float)reader.ReadDouble();
        return new System.Numerics.Vector3(x, y, z);
    }
}