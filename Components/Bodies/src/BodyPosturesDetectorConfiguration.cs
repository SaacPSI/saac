using Microsoft.Azure.Kinect.BodyTracking;

namespace Bodies 
{
    public class BodyPosturesDetectorConfiguration 
    {
        public JointConfidenceLevel MinimumConfidenceLevel { get; set; } = JointConfidenceLevel.Medium;
        public double MinimumDistanceThreshold { get; set; } = 0.1;
        public double MinimumSittingDegrees { get; set; } = 90.0;
        public double MaximumStandingDegrees { get; set; } = 15.0;
        public double MaximumPointingDegrees { get; set; } = 25.0;
    }
}
