using System;
using System.Numerics;
using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace CASPERAnalysis.StreamProcessors
{
    /// <summary>
    /// Detects if hand is touching door or moving toward door
    /// </summary>
    public class HandDoorProximityDetector : ConsumerProducer<(Vector3, Vector3), bool>
    {
        private readonly float proximityThreshold; // Distance threshold in meters
        private readonly float velocityThreshold; // Velocity threshold for "moving toward"

        public HandDoorProximityDetector(Pipeline pipeline, float proximityThreshold = 0.15f, float velocityThreshold = 0.1f)
            : base(pipeline)
        {
            this.proximityThreshold = proximityThreshold;
            this.velocityThreshold = velocityThreshold;
        }

        protected override void Receive((Vector3, Vector3) data, Envelope envelope)
        {
            var (handPosition, doorPosition) = data;
            
            // Calculate distance
            var distance = Vector3.Distance(handPosition, doorPosition);
            
            // Check if hand is in proximity of door
            bool isNearDoor = distance < proximityThreshold;
            
            // TODO: Add velocity-based detection (check if hand is moving toward door)
            // This would require buffering previous positions
            
            Out.Post(isNearDoor, envelope.OriginatingTime);
        }
    }
}

