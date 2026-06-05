// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bhaptic.Helpers
{
    /// <summary>
    /// Represents a direct motor-level haptic event triggered by the bHaptics SDK.
    /// Allows fine-grained control over individual actuators on a haptic device.
    /// </summary>
    public struct MotorPlay
    {
        /// <summary>Target device position (maps to a <c>PositionType</c> value).</summary>
        public int Position;

        /// <summary>Unique identifier for this play request.</summary>
        public int RequestId;

        /// <summary>
        /// Per-motor intensity values (0–100) for each actuator on the target device.
        /// The array length must match the actuator count of the targeted <c>PositionType</c>.
        /// </summary>
        public int[] Motors;

        /// <summary>Duration of the motor activation, in milliseconds.</summary>
        public int DurationMillis;

        /// <summary>
        /// Initializes a new instance of the <see cref="MotorPlay"/> struct.
        /// </summary>
        /// <param name="position">Target device position (maps to a <c>PositionType</c> value).</param>
        /// <param name="requestId">Unique identifier for this play request.</param>
        /// <param name="motors">Per-motor intensity values (0–100) for each actuator.</param>
        /// <param name="durationMillis">Duration of the activation, in milliseconds.</param>
        public MotorPlay(int position, int requestId, int[] motors, int durationMillis)
        {
            this.Position = position;
            this.RequestId = requestId;
            this.Motors = motors;
            this.DurationMillis = durationMillis;
        }
    }
}
