// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using MathNet.Spatial.Euclidean;

namespace SAAC.NatNetComponent
{
    /// <summary>
    /// RigidBody data structure.
    /// </summary>
    public struct RigidBody
    {
        /// <summary>
        /// The name of the rigid body.
        /// </summary>
        public string Name;

        /// <summary>
        /// The 3D position of the rigid body.
        /// </summary>
        public Vector3D Position;

        /// <summary>
        /// The orientation quaternion of the rigid body.
        /// </summary>
        public Quaternion Orientation;
    }

    /// <summary>
    /// Joint data structure.
    /// </summary>
    public struct Joint
    {
        /// <summary>
        /// The unique identifier of the joint.
        /// </summary>
        public uint Id;

        /// <summary>
        /// The tracking confidence level of the joint.
        /// </summary>
        public float Confidence;

        /// <summary>
        /// The 3D position of the joint.
        /// </summary>
        public Vector3D Position;

        /// <summary>
        /// The orientation quaternion of the joint.
        /// </summary>
        public Quaternion Orientation;
    }

    /// <summary>
    /// Skeleton data structure.
    /// </summary>
    public struct Skeleton
    {
        /// <summary>
        /// The unique identifier of the skeleton.
        /// </summary>
        public uint Id;

        /// <summary>
        /// The list of joints that compose the skeleton body.
        /// </summary>
        public List<Joint> Body;
    }
}
