using Microsoft.Azure.Kinect.BodyTracking;
using MathNet.Spatial.Euclidean;

namespace SAAC.Bodies
{
    public class SimplifiedBody
    {
        public uint Id { get; set; } = uint.MaxValue;
        public enum SensorOrigin { Nuitrack, Azure };

        public SensorOrigin Origin { get; private set; }
        public Dictionary<JointId, Tuple<JointConfidenceLevel, Vector3D>> Joints { get; set; }

        public SimplifiedBody(SensorOrigin origin, uint id, Dictionary<JointId, Tuple<JointConfidenceLevel, Vector3D>>? joints = null)
        {
            Origin = origin;
            Id = id;
            Joints = joints ?? new Dictionary<JointId, Tuple<JointConfidenceLevel, Vector3D>>();
        }
    }
}
