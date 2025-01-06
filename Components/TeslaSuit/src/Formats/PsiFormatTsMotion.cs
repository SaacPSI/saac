using Microsoft.Psi.Interop.Serialization;
using SAAC.PipelineServices;
using System.IO;
using TsAPI.Types;

namespace SAAC.TeslaSuit
{
    public class PsiFormatTsMotion : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4>>(WriteTsMotion, ReadTsMotion);
        }

        public void WriteTsMotion(Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4> data, BinaryWriter writer)
        {
            writer.Write(data.Count);
            foreach (var bone in data)
            {
                writer.Write((int)bone.Key);
                writer.Write((double)bone.Value.M11);
                writer.Write((double)bone.Value.M12);
                writer.Write((double)bone.Value.M13);
                writer.Write((double)bone.Value.M14);
                writer.Write((double)bone.Value.M21);
                writer.Write((double)bone.Value.M22);
                writer.Write((double)bone.Value.M23);
                writer.Write((double)bone.Value.M24);
                writer.Write((double)bone.Value.M31);
                writer.Write((double)bone.Value.M32);
                writer.Write((double)bone.Value.M33);
                writer.Write((double)bone.Value.M34);
                writer.Write((double)bone.Value.M41);
                writer.Write((double)bone.Value.M42);
                writer.Write((double)bone.Value.M43);
                writer.Write((double)bone.Value.M44);
            }
        }

        public Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4> ReadTsMotion(BinaryReader reader)
        {
            Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4> data = new Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                TsHumanBoneIndex index = (TsHumanBoneIndex)reader.ReadInt32();
                System.Numerics.Matrix4x4 matrix = new System.Numerics.Matrix4x4();
                matrix.M11 = (float)reader.ReadDouble();
                matrix.M12 = (float)reader.ReadDouble();
                matrix.M13 = (float)reader.ReadDouble();
                matrix.M14 = (float)reader.ReadDouble();
                matrix.M21 = (float)reader.ReadDouble();
                matrix.M22 = (float)reader.ReadDouble();
                matrix.M23 = (float)reader.ReadDouble();
                matrix.M24 = (float)reader.ReadDouble();
                matrix.M31 = (float)reader.ReadDouble();
                matrix.M32 = (float)reader.ReadDouble();
                matrix.M33 = (float)reader.ReadDouble();
                matrix.M34 = (float)reader.ReadDouble();
                matrix.M41 = (float)reader.ReadDouble();
                matrix.M42 = (float)reader.ReadDouble();
                matrix.M43 = (float)reader.ReadDouble();
                matrix.M44 = (float)reader.ReadDouble();
                data.Add(index, matrix);
            }
            return data;
        }
    }
}