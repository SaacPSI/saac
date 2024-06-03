using Microsoft.Psi;
using Microsoft.Psi.Common;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Serialization;
using System;


public abstract class PsiASerializer<T> : ISerializer<T>
{
    public bool? IsClearRequired => false;
    public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
    {
        return targetSchema ?? TypeSchema.FromType(typeof(T), this.GetType().AssemblyQualifiedName, serializers.RuntimeInfo.SerializationSystemVersion);
    }
    public void Clone(T instance, ref T target, SerializationContext context){}
    public abstract void Serialize(BufferWriter writer, T instance, SerializationContext context);
    public abstract void Deserialize(BufferReader reader, ref T target, SerializationContext context);
    public void PrepareDeserializationTarget(BufferReader reader, ref T target, SerializationContext context){}
    public void PrepareCloningTarget(T instance, ref T target, SerializationContext context)
    {
        target = instance;
    }
    public void Clear(ref T target, SerializationContext context){}
}

public class BoolSerializer : PsiASerializer<bool>
{
    public override void Serialize(BufferWriter writer, bool instance, SerializationContext context){}
    public override void Deserialize(BufferReader reader, ref bool target, SerializationContext context){}
}

public class CharSerializer : PsiASerializer<char>
{
    public override void Serialize(BufferWriter writer, char instance, SerializationContext context){}
    public override void Deserialize(BufferReader reader, ref char target, SerializationContext context){}
}

public class Vector3Serializer : PsiASerializer<System.Numerics.Vector3>
{
    public override void Serialize(BufferWriter writer, System.Numerics.Vector3 instance, SerializationContext context){}
    public override void Deserialize(BufferReader reader, ref System.Numerics.Vector3 target, SerializationContext context){}
}

public class TupleOfVector3Serializer : PsiASerializer<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>
{
    public override void Serialize(BufferWriter writer, Tuple<System.Numerics.Vector3, System.Numerics.Vector3> instance, SerializationContext context){}
    public override void Deserialize(BufferReader reader, ref Tuple<System.Numerics.Vector3, System.Numerics.Vector3> target, SerializationContext context){}
}

public class PsiMessageBufferSerializer : PsiASerializer<Message<BufferReader>>
{
    public override void Serialize(BufferWriter writer, Message<BufferReader> instance, SerializationContext context){}
    public override void Deserialize(BufferReader reader, ref Message<BufferReader> target, SerializationContext context){}
}

public class ImageSerializer : PsiASerializer<Image>
{
    public override void Serialize(BufferWriter writer, Image instance, SerializationContext context){}
    public override void Deserialize(BufferReader reader, ref Image target, SerializationContext context){}
}

public class BytesSerializer : PsiASerializer<byte[]>
{
    public override void Serialize(BufferWriter writer, byte[] instance, SerializationContext context){}
    public override void Deserialize(BufferReader reader, ref byte[] target, SerializationContext context){}
}

public class Matrix4x4Serializer : PsiASerializer<System.Numerics.Matrix4x4>
{
    public override void Serialize(BufferWriter writer, System.Numerics.Matrix4x4 instance, SerializationContext context) { }
    public override void Deserialize(BufferReader reader, ref System.Numerics.Matrix4x4 target, SerializationContext context) { }
}