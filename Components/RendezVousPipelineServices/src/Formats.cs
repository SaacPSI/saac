using System;
using System.IO;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi.Interop.Serialization;

namespace SAAC.RendezVousPipelineServices
{
    public interface IPsiFormat
    {
        public abstract dynamic GetFormat();
    }

    public class PsiFormatBoolean : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<bool>(WriteBoolean, ReadBoolean);
        }

        public void WriteBoolean(bool boolean, BinaryWriter writer)
        {
            writer.Write(boolean);
        }

        public bool ReadBoolean(BinaryReader reader)
        {
            return reader.ReadBoolean();
        }
    }

    public class PsiFormaChar : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<char>(WriteChar, ReadChar);
        }

        public static void WriteChar(char character, BinaryWriter writer)
        {
            writer.Write(character);
        }

        public static char ReadChar(BinaryReader reader)
        {
            return reader.ReadChar();
        }
    }

    public class PsiFormatPositionAndOrientation : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>(WritePositionOrientation, ReadPositionOrientation);
        }

        public void WritePositionOrientation(Tuple<System.Numerics.Vector3, System.Numerics.Vector3> point3D, BinaryWriter writer)
        {
            writer.Write((double)point3D.Item1.X);
            writer.Write((double)point3D.Item1.Y);
            writer.Write((double)point3D.Item1.Z);
            writer.Write((double)point3D.Item2.X);
            writer.Write((double)point3D.Item2.Y);
            writer.Write((double)point3D.Item2.Z);
        }

        public Tuple<System.Numerics.Vector3, System.Numerics.Vector3> ReadPositionOrientation(BinaryReader reader)
        {
            return new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()),
                            new System.Numerics.Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble()));
        }
    }

    public class PsiFormatRay : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<MathNet.Spatial.Euclidean.Ray3D>(WriteRay3D, ReadRay3D);
        }

        public void WriteRay3D(Ray3D ray3D, BinaryWriter writer)
        {
            writer.Write(ray3D.ThroughPoint.X);
            writer.Write(ray3D.ThroughPoint.Y);
            writer.Write(ray3D.ThroughPoint.Z);
            writer.Write(ray3D.Direction.X);
            writer.Write(ray3D.Direction.Y);
            writer.Write(ray3D.Direction.Z);
        }

        public Ray3D ReadRay3D(BinaryReader reader)
        {
            return new Ray3D(new Point3D(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble()), UnitVector3D.Create(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble()));
        }
    }

    public class PsiFormatString : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<string>(WriteString, ReadSring);
        }

        public void WriteString(string data, BinaryWriter writer)
        {
            writer.Write(data);
        }

        public string ReadSring(BinaryReader reader)
        {
            return reader.ReadString();
        }
    }

    public class PsiFormatBytes : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<byte[]>(WriteBytes, ReadBytes);
        }

        public void WriteBytes(byte[] image, BinaryWriter writer)
        {
            writer.Write(image.Length);
            writer.Write(image);
        }

        public byte[] ReadBytes(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            byte[] data = reader.ReadBytes(length);
            return data;
        }
    }
}
