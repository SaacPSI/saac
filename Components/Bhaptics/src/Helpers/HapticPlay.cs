// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bhaptic.Helpers
{
    /// <summary>
    /// Represents a pattern-based haptic play event triggered by the bHaptics SDK.
    /// Contains all parameters required to replay or record the event.
    /// </summary>
    public struct HapticPlay
    {
        /// <summary>Identifier of the haptic pattern event to play.</summary>
        public string EventId;

        /// <summary>Unique identifier for this play request.</summary>
        public int RequestId;

        /// <summary>Start position within the haptic pattern, in milliseconds.</summary>
        public int StartMillis;

        /// <summary>Playback intensity multiplier, in the range [0, 1].</summary>
        public float Intensity;

        /// <summary>Playback duration multiplier, in the range [0, 1].</summary>
        public float Duration;

        /// <summary>Rotation angle around the X axis, in degrees.</summary>
        public float AngleX;

        /// <summary>Vertical offset applied to the pattern, in the range [-0.5, 0.5].</summary>
        public float OffsetY;

        /// <summary>Number of times the pattern is repeated. 0 means play once.</summary>
        public int Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="HapticPlay"/> struct.
        /// </summary>
        /// <param name="eventId">Identifier of the haptic pattern event.</param>
        /// <param name="requestId">Unique identifier for this play request.</param>
        /// <param name="startMillis">Start offset within the pattern, in milliseconds.</param>
        /// <param name="intensity">Intensity multiplier in [0, 1].</param>
        /// <param name="duration">Duration multiplier in [0, 1].</param>
        /// <param name="angleX">Rotation angle around the X axis, in degrees.</param>
        /// <param name="offsetY">Vertical offset in [-0.5, 0.5].</param>
        /// <param name="count">Repeat count (0 = play once).</param>
        public HapticPlay(string eventId, int requestId, int startMillis, float intensity, float duration, float angleX, float offsetY, int count)
        {
            this.EventId = eventId;
            this.RequestId = requestId;
            this.StartMillis = startMillis;
            this.Intensity = intensity;
            this.Duration = duration;
            this.AngleX = angleX;
            this.OffsetY = offsetY;
            this.Count = count;
        }
    }
}
