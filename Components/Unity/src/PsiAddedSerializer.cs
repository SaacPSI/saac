using Microsoft.Psi;
using Microsoft.Psi.Common;
using Microsoft.Psi.Serialization;
using System;

public class BoolSerializer : PsiASerializer<bool>
{
    public override void Serialize(BufferWriter writer, bool instance, SerializationContext context)
    {
        writer.Write(instance);
    }

    public override void Deserialize(BufferReader reader, ref bool target, SerializationContext context)
    {
        target = reader.ReadBool();
    }
}

public class CharSerializer : PsiASerializer<char>
{
    public override void Serialize(BufferWriter writer, char instance, SerializationContext context)
    {
        writer.Write(instance);
    }

    public override void Deserialize(BufferReader reader, ref char target, SerializationContext context)
    {
        target = reader.ReadChar();
    }
}

public class Vector3Serializer : PsiASerializer<System.Numerics.Vector3>
{
    public override void Serialize(BufferWriter writer, System.Numerics.Vector3 instance, SerializationContext context)
    {
        writer.Write((double)instance.X);
        writer.Write((double)instance.Y);
        writer.Write((double)instance.Z);
    }

    public override void Deserialize(BufferReader reader, ref System.Numerics.Vector3 target, SerializationContext context)
    {
        float x = (float)reader.ReadDouble();
        float y = (float)reader.ReadDouble();
        float z = (float)reader.ReadDouble();
        target = new System.Numerics.Vector3(x, y, z);
    }
}

public class TupleOfVector3Serializer : PsiASerializer<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>
{
    public override void Serialize(BufferWriter writer, Tuple<System.Numerics.Vector3, System.Numerics.Vector3> instance, SerializationContext context)
    {
        writer.Write((double)instance.Item1.X);
        writer.Write((double)instance.Item1.Y);
        writer.Write((double)instance.Item1.Z);
        writer.Write((double)instance.Item2.X);
        writer.Write((double)instance.Item2.Y);
        writer.Write((double)instance.Item2.Z);
    }

    public override void Deserialize(BufferReader reader, ref Tuple<System.Numerics.Vector3, System.Numerics.Vector3> target, SerializationContext context)
    {
        float x = (float)reader.ReadDouble();
        float y = (float)reader.ReadDouble();
        float z = (float)reader.ReadDouble();
        float a = (float)reader.ReadDouble();
        float t = (float)reader.ReadDouble();
        float g = (float)reader.ReadDouble();
        target = new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(new System.Numerics.Vector3(x, y, z), new System.Numerics.Vector3(a, t, g));
    }
}

public class PsiMessageBufferSerializer : PsiASerializer<Message<BufferReader>>
{
    public override void Serialize(BufferWriter writer, Message<BufferReader> instance, SerializationContext context)
    {
        //writer.Write(instance.);
        //writer.Write(instance.Item1.Y);
        //writer.Write(instance.Item1.Z);
        //writer.Write(instance.Item2.X);
        //writer.Write(instance.Item2.Y);
        //writer.Write(instance.Item2.Z);
    }

    public override void Deserialize(BufferReader reader, ref Message<BufferReader> target, SerializationContext context)
    {
        //float x = (float)reader.ReadDouble();
        //float y = (float)reader.ReadDouble();
        //float z = (float)reader.ReadDouble();
        //float a = (float)reader.ReadDouble();
        //float t = (float)reader.ReadDouble();
        //float g = (float)reader.ReadDouble();
        //target = new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(new System.Numerics.Vector3(x, y, z), new System.Numerics.Vector3(a, t, g));
    }
}