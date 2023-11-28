using System;
using Microsoft.Psi;
using Microsoft.Azure.Kinect.BodyTracking;

namespace Bodies
{
    public class HandsProximityDetectorConfiguration 
    {
        /// <summary>
        /// Gets or sets the minimum confidence level of joints used in the algorithm.
        /// </summary>
        public JointConfidenceLevel MinimumConfidenceLevel { get; set; } = JointConfidenceLevel.Low;

        /// <summary>
        /// Gets or sets the minimum distance between hands used in the algorithm.
        /// </summary>
        public double MinimumDistanceThreshold { get; set; } = 0.1;

        /// <summary>
        /// Select if only received pair avec checked or if all possible pair are tested.
        /// </summary>
        public bool IsPairToCheckGiven { get; set; } = false;
    }
}
