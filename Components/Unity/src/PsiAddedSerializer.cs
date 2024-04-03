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

public class IntSerializer : PsiASerializer<int>
{
    public override void Serialize(BufferWriter writer, int instance, SerializationContext context)
    {
        writer.Write(instance);
    }

    public override void Deserialize(BufferReader reader, ref int target, SerializationContext context)
    {
        target = reader.Read();
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
        target = new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble());
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
        target = new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()), new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()));
    }
}

public class PsiMessageBufferSerializer : PsiASerializer<Message<BufferReader>>
{
    public override void Serialize(BufferWriter writer, Message<BufferReader> instance, SerializationContext context)
    {
        writer.Write(instance.OriginatingTime.ticks);
        writer.Write(instance.CreationTime.ticks);
        writer.Write(instance.SourceId);
        writer.Write(instance.SequenceId);
        writer.Write(instance.Data.CurrentPosition);
        writer.Write(instance.Data.Length);
        writer.Write(instance.Data.Bytes);
    }

    public override void Deserialize(BufferReader reader, ref Message<BufferReader> target, SerializationContext context)
    {
        DateTime OriginatingTime = new System.DateTime((long)reader.ReadUInt64());
        DateTime CreationTime = new System.DateTime((long)reader.ReadUInt64());
        int SourceId = reader.ReadInt32();
        int SequenceId = reader.ReadInt32();

        BufferReader bufferReader = new BufferReader();
        bufferReader.CurrentPosition = reader.ReadInt32();
        bufferReader.Length = reader.ReadInt32();
        bufferReader.Bytes = reader.ReadBytes(bufferReader.Length);

        target.Data = Message.Create<BufferReader>(bufferReader, OriginatingTime, CreationTime, SourceId, SequenceId);
    }
}