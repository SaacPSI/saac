using Microsoft.Psi.Interop.Serialization;
using System.IO;
using SAAC.Bodies;
using Microsoft.Azure.Kinect.BodyTracking;
using static SAAC.Bodies.SimplifiedBody;

namespace SAAC.PipelineServices
{
    public class PsiFormatListOfSimplifiedBody : IPsiFormat
    {
        public dynamic GetFormat()
        {
            return new Format<List<SimplifiedBody>>(WriteSimplifiedBodies, ReadSimplifiedBodies);
        }

        public void WriteSimplifiedBodies(List<SimplifiedBody> bodies, BinaryWriter writer)
        {
            writer.Write(bodies.Count);
            foreach (SimplifiedBody body in bodies)
            {
                writer.Write(body.Id);
                writer.Write((int)body.Origin);
                writer.Write(body.Joints.Count);
                foreach (var joint in body.Joints)
                {
                    writer.Write((int)joint.Key);
                    writer.Write((int)joint.Value.Item1);
                    writer.Write(joint.Value.Item2.X);
                    writer.Write(joint.Value.Item2.Y);
                    writer.Write(joint.Value.Item2.Z);
                };
            }
        }

        public List<SimplifiedBody> ReadSimplifiedBodies(BinaryReader reader)
        {
            List<SimplifiedBody> bodies = new List<SimplifiedBody>();
            int count = reader.ReadInt32();
            for(int bodiesIterator = 0; bodiesIterator < count; bodiesIterator++)
            {
                uint id = reader.ReadUInt32();
                SensorOrigin origin = (SensorOrigin)reader.ReadInt32();
                int jointCount = reader.ReadInt32();
                Dictionary<JointId, Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D>> joints = new Dictionary<JointId, Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D>>();
                for(int jointIterator = 0; jointIterator <jointCount; jointIterator++ )
                {
                    joints.Add((JointId)reader.ReadInt32(), new Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D>((JointConfidenceLevel)reader.ReadInt32(), 
                        new MathNet.Spatial.Euclidean.Vector3D(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble())));
                }
                bodies.Add(new SimplifiedBody(origin, id, joints));
            }
            return bodies;
        }
    }
}