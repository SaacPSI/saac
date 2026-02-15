// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Class representing an eye movement from a set of samplings. This movement can be a fixation or a saccade.
    /// </summary>
    public class EyeMovement
    {
        /// <summary>
        /// Gets or sets a value indicating whether the movement is a fixation. If not, then it is a saccade.
        /// </summary>
        public bool IsFixation { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the first EyeTrackingData labeled in this movement.
        /// </summary>
        public DateTime FirstTimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last EyeTrackingData labeled in this movement.
        /// </summary>
        public DateTime LastTimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the amount of eyeTracking messages in this movement.
        /// </summary>
        public int MessagesCount { get; set; }

        /// <summary>
        /// Gets or sets the average position of the fixation (null if the movement is a saccade).
        /// </summary>
        public System.Numerics.Vector3 FixDirection { get; set; }

        /// <summary>
        /// Gets or sets the identification key of the fixated object (null if the movement is a saccade).
        /// </summary>
        public (int, string) FixedObjectKey { get; set; }

        /// <summary>
        /// Gets or sets the direction of the starting fixation of the saccade (null if the movement is a fixation).
        /// </summary>
        public System.Numerics.Vector3 SaccStartDirection { get; set; }

        /// <summary>
        /// Gets or sets the direction of the ending fixation of the saccade (null if the movement is a fixation).
        /// </summary>
        public System.Numerics.Vector3 SaccEndDirection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeMovement"/> class.
        /// </summary>
        /// <param name="beginTime">The beginning time of the movement.</param>
        /// <param name="isFix">A value indicating whether this is a fixation.</param>
        public EyeMovement(DateTime beginTime, bool isFix)
        {
            this.FirstTimeStamp = beginTime;
            this.IsFixation = isFix;
            this.MessagesCount = 1;
        }

        /// <summary>
        /// Gets the duration of the eye movement.
        /// </summary>
        /// <returns>The duration as a TimeSpan.</returns>
        public TimeSpan GetDuration()
        {
            return this.LastTimeStamp - this.FirstTimeStamp;
        }

        /// <summary>
        /// Gets the saccade amplitude in degrees.
        /// </summary>
        /// <returns>The saccade amplitude in degrees.</returns>
        public double GetSaccAmplitude()
        {
            double angle = 0;
            if (!this.IsFixation && this.SaccStartDirection != this.SaccEndDirection)
            {
                double dot = System.Numerics.Vector3.Dot(this.SaccStartDirection, this.SaccEndDirection);
                if (dot >= -1 && dot <= 1)
                {
                    angle = Math.Acos(dot);
                }
            }

            return angle * 180 / Math.PI;
        }

        /// <summary>
        /// Equality operator for EyeMovement.
        /// </summary>
        /// <param name="e1">First eye movement.</param>
        /// <param name="e2">Second eye movement.</param>
        /// <returns>True if equal; otherwise false.</returns>
        public static bool operator ==(EyeMovement e1, EyeMovement e2)
        {
            if (e1 is null || e2 is null)
            {
                return false;
            }

            return e1.IsFixation == e2.IsFixation
                        && e1.FirstTimeStamp == e2.FirstTimeStamp
                        && e1.LastTimeStamp == e2.LastTimeStamp
                        && e1.MessagesCount == e2.MessagesCount
                        && e1.FixDirection == e2.FixDirection
                        && e1.FixedObjectKey == e2.FixedObjectKey
                        && e1.SaccStartDirection == e2.SaccStartDirection
                        && e1.SaccEndDirection == e2.SaccEndDirection;
        }

        /// <summary>
        /// Inequality operator for EyeMovement.
        /// </summary>
        /// <param name="e1">First eye movement.</param>
        /// <param name="e2">Second eye movement.</param>
        /// <returns>True if not equal; otherwise false.</returns>
        public static bool operator !=(EyeMovement e1, EyeMovement e2)
        {
            if (e1 is null || e2 is null)
            {
                return false;
            }

            return e1.IsFixation != e2.IsFixation
                        || e1.FirstTimeStamp != e2.FirstTimeStamp
                        || e1.LastTimeStamp != e2.LastTimeStamp
                        || e1.MessagesCount != e2.MessagesCount
                        || e1.FixDirection != e2.FixDirection
                        || e1.FixedObjectKey != e2.FixedObjectKey
                        || e1.SaccStartDirection != e2.SaccStartDirection
                        || e1.SaccEndDirection != e2.SaccEndDirection;
        }
    }
}
