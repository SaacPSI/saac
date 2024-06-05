using MathNet.Spatial.Euclidean;

namespace SAAC.NatNetComponent
{
    /// <summary>
    /// RigidBody data structure.
    /// </summary>
    public struct RigidBody
    {
        public string name;
        public Vector3D position;
        public Quaternion orientation;
    }

    /// <summary>
    /// Joint data structure.
    /// </summary>
    public struct Joint
    {
        public uint id;
        public float confidence;
        public Vector3D position;
        public Quaternion orientation;
    }

    /// <summary>
    /// Skeleton data structure.
    /// </summary>
    public struct Skeleton
    {
        public uint id;
        public List<Joint> body;
    }
}