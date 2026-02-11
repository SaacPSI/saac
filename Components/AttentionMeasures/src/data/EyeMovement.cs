// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.AttentionMeasures
{
    // Class representing an eye movement from a set of samplings. This movement can be a fixation or a saccade
    public class EyeMovement
    {
        // Is the movement a fixation ? If not, then it is a saccade
        public bool IsFixation;

        // Timestamps of the first  and last EyeTrackingData labeled in this movement
        public DateTime FirstTimeStamp;
        public DateTime LastTimeStamp;

        // Amount of eyeTracking messages in this mouvement
        public int MessagesCount;

        // Average position of the fixation (null if the movement is a saccade)
        public System.Numerics.Vector3 FixDirection;

        // Identification key of the fixated object (null if the movement is a saccade)
        public (int, string) FixedObjectKey;

        // Direction of the starting fixation of the saccade (null if the movement is a fixation)
        public System.Numerics.Vector3 SaccStartDirection;

        // Direction of the ending fixation of the saccade (null if the movement is a fixation)
        public System.Numerics.Vector3 SaccEndDirection;

        // Constructor
        public EyeMovement(DateTime beginTime, bool isFix)
        {
            this.FirstTimeStamp = beginTime;
            this.IsFixation = isFix;
            this.MessagesCount = 1;
        }

        // Getters
        public TimeSpan GetDuration()
        {
            return this.LastTimeStamp - this.FirstTimeStamp;
        }

        // Saccade amplitude in degrees
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
