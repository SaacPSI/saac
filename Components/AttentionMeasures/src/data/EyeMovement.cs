namespace SAAC.AttentionMeasures
{
    //Class representing an eye movement from a set of samplings. This movement can be a fixation or a saccade
    public class EyeMovement
    {

        //Is the movement a fixation ? If not, then it is a saccade
        public bool isFixation;
        //Timestamps of the first  and last EyeTrackingData labeled in this movement
        public DateTime firstTimeStamp;
        public DateTime lastTimeStamp;
        //Amount of eyeTracking messages in this mouvement
        public int messagesCount;
        //Average position of the fixation (null if the movement is a saccade)
        public System.Numerics.Vector3 fixDirection;
        //Identification key of the fixated object (null if the movement is a saccade)
        public (int, string) fixedObjectKey;
        //Direction of the starting fixation of the saccade (null if the movement is a fixation)
        public System.Numerics.Vector3 saccStartDirection;
        //Direction of the ending fixation of the saccade (null if the movement is a fixation)
        public System.Numerics.Vector3 saccEndDirection;

        //Constructor
        public EyeMovement(DateTime beginTime, bool isFix)
        {
            firstTimeStamp = beginTime;
            isFixation = isFix;
            messagesCount = 1;
        }

        //Getters
        public TimeSpan GetDuration() { return lastTimeStamp - firstTimeStamp; }

        //Saccade amplitude in degrees
        public double GetSaccAmplitude()
        {
            double angle = 0;
            if (!isFixation && saccStartDirection != saccEndDirection)
            {
                double dot = System.Numerics.Vector3.Dot(saccStartDirection, saccEndDirection);
                if (dot >= -1 && dot <= 1)
                {
                    angle = Math.Acos(dot);
                }
            }
            return angle * 180 / Math.PI;
        }

        public static bool operator ==(EyeMovement e1, EyeMovement e2)
        {
            if (e1 is null || e2 is null) return false;
            return e1.isFixation == e2.isFixation
                        && e1.firstTimeStamp == e2.firstTimeStamp
                        && e1.lastTimeStamp == e2.lastTimeStamp
                        && e1.messagesCount == e2.messagesCount
                        && e1.fixDirection == e2.fixDirection
                        && e1.fixedObjectKey == e2.fixedObjectKey
                        && e1.saccStartDirection == e2.saccStartDirection
                        && e1.saccEndDirection == e2.saccEndDirection;

        }

        public static bool operator !=(EyeMovement e1, EyeMovement e2)
        {
            if (e1 is null || e2 is null) return false;
            return e1.isFixation != e2.isFixation
                        || e1.firstTimeStamp != e2.firstTimeStamp
                        || e1.lastTimeStamp != e2.lastTimeStamp
                        || e1.messagesCount != e2.messagesCount
                        || e1.fixDirection != e2.fixDirection
                        || e1.fixedObjectKey != e2.fixedObjectKey
                        || e1.saccStartDirection != e2.saccStartDirection
                        || e1.saccEndDirection != e2.saccEndDirection;
        }


    }
}