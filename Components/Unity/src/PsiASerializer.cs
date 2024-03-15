using Microsoft.Psi;
using Microsoft.Psi.Common;
using Microsoft.Psi.Serialization;

public abstract class PsiASerializer<T> : ISerializer<T>
{
    /// <inheritdoc />
    public bool? IsClearRequired => false;

    public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
    {
        return targetSchema ?? TypeSchema.FromType(typeof(T), this.GetType().AssemblyQualifiedName, serializers.RuntimeInfo.SerializationSystemVersion);
    }

    public void Clone(T instance, ref T target, SerializationContext context)
    {
        //target = instance.DeepClone();
    }

    public abstract void Serialize(BufferWriter writer, T instance, SerializationContext context);

    public abstract void Deserialize(BufferReader reader, ref T target, SerializationContext context);

    public void PrepareDeserializationTarget(BufferReader reader, ref T target, SerializationContext context)
    {
    }

    public void PrepareCloningTarget(T instance, ref T target, SerializationContext context)
    {
        target = instance;
    }

    public void Clear(ref T target, SerializationContext context)
    {
    }
}