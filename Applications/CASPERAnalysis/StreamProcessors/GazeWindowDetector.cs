using System;
using System.Linq;
using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace CASPERAnalysis.StreamProcessors
{
    /// <summary>
    /// Detects gaze events within specified time windows (e.g., 50-150ms)
    /// </summary>
    public class GazeWindowDetector : ConsumerProducer<(int, bool, string), bool>
    {
        private readonly TimeSpan windowStart;
        private readonly TimeSpan windowEnd;
        private readonly string targetObject; // Object to detect gaze on (e.g., "door", "indicator")

        public GazeWindowDetector(
            Pipeline pipeline, 
            TimeSpan windowStart, 
            TimeSpan windowEnd, 
            string targetObject)
            : base(pipeline)
        {
            this.windowStart = windowStart;
            this.windowEnd = windowEnd;
            this.targetObject = targetObject;
        }

        protected override void Receive((int, bool, string) data, Envelope envelope)
        {
            var (id, isGazing, objectName) = data;
            
            // Check if gaze is on target object
            bool isGazingOnTarget = isGazing && 
                (string.IsNullOrEmpty(targetObject) || objectName.Contains(targetObject, StringComparison.OrdinalIgnoreCase));
            
            // The windowing logic would be handled by Psi's Window operator upstream
            // This component just checks if the gaze event matches the target
            Out.Post(isGazingOnTarget, envelope.OriginatingTime);
        }
    }
}

