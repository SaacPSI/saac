// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.BodyTracking;

    /// <summary>
    /// Represents a simplified body with joint positions.
    /// </summary>
    public class SimplifiedBody
    {
        /// <summary>
        /// Sensor origin types.
        /// </summary>
        public enum SensorOrigin
        {
            /// <summary>
            /// Nuitrack sensor.
            /// </summary>
            Nuitrack,

            /// <summary>
            /// Azure Kinect sensor.
            /// </summary>
            Azure,

            /// <summary>
            /// Tesla Suit sensor.
            /// </summary>
            TeslaSuit,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimplifiedBody"/> class.
        /// </summary>
        /// <param name="origin">The sensor origin.</param>
        /// <param name="id">The body ID.</param>
        /// <param name="joints">The joints dictionary.</param>
        public SimplifiedBody(SensorOrigin origin, uint id, Dictionary<JointId, Tuple<JointConfidenceLevel, Vector3D>>? joints = null)
        {
            this.Origin = origin;
            this.Id = id;
            this.Joints = joints ?? new Dictionary<JointId, Tuple<JointConfidenceLevel, Vector3D>>();
        }

        /// <summary>
        /// Gets or sets the body ID.
        /// </summary>
        public uint Id { get; set; } = uint.MaxValue;

        /// <summary>
        /// Gets the sensor origin.
        /// </summary>
        public SensorOrigin Origin { get; private set; }

        /// <summary>
        /// Gets or sets the joints dictionary.
        /// </summary>
        public Dictionary<JointId, Tuple<JointConfidenceLevel, Vector3D>> Joints { get; set; }
    }
}
