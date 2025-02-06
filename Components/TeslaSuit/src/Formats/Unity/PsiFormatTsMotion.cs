using Microsoft.Psi.Interop.Serialization;
using System.IO;
using TsAPI.Types;

namespace SAAC.PsiFormats
{
    public class PsiFormatTsMotion
    {
        public static Format<Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4>> GetFormat()
        {
            return new Format<Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4>>(WriteTsMotion, ReadTsMotion);
        }

        public static void WriteTsMotion(Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4> data, BinaryWriter writer)
        {
            writer.Write(data.Count);
            foreach (var bone in data)
            {
                writer.Write((int)bone.Key);
                writer.Write(bone.Value.M11);
                writer.Write(bone.Value.M12);
                writer.Write(bone.Value.M13);
                writer.Write(bone.Value.M14);
                writer.Write(bone.Value.M21);
                writer.Write(bone.Value.M22);
                writer.Write(bone.Value.M23);
                writer.Write(bone.Value.M24);
                writer.Write(bone.Value.M31);
                writer.Write(bone.Value.M32);
                writer.Write(bone.Value.M33);
                writer.Write(bone.Value.M34);
                writer.Write(bone.Value.M41);
                writer.Write(bone.Value.M42);
                writer.Write(bone.Value.M43);
                writer.Write(bone.Value.M44);
            }
        }

        public static Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4> ReadTsMotion(BinaryReader reader)
        {
            Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4> data = new Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                TsHumanBoneIndex index = (TsHumanBoneIndex)reader.ReadInt32();
                System.Numerics.Matrix4x4 matrix = new System.Numerics.Matrix4x4();
                matrix.M11 = reader.ReadSingle();
                matrix.M12 = reader.ReadSingle();
                matrix.M13 = reader.ReadSingle();
                matrix.M14 = reader.ReadSingle();
                matrix.M21 = reader.ReadSingle();
                matrix.M22 = reader.ReadSingle();
                matrix.M23 = reader.ReadSingle();
                matrix.M24 = reader.ReadSingle();
                matrix.M31 = reader.ReadSingle();
                matrix.M32 = reader.ReadSingle();
                matrix.M33 = reader.ReadSingle();
                matrix.M34 = reader.ReadSingle();
                matrix.M41 = reader.ReadSingle();
                matrix.M42 = reader.ReadSingle();
                matrix.M43 = reader.ReadSingle();
                matrix.M44 = reader.ReadSingle();
                data.Add(index, matrix);
            }
            return data;
        }
    }
}