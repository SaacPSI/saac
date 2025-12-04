
using SAAC.GlobalHelpers;
using Microsoft.Psi.Components;
using Microsoft.Psi;

// from Unity XR Interaction Toolkit
namespace SAAC.Gestures
{
    public class PokeDetector : IConsumerProducer<Hand, bool>
    {
        public Receiver<Hand> In { get; }

        public Emitter<bool> Out { get; }

        private string name;

        public PokeDetector(Pipeline pipeline, string name = nameof(PokeDetector))
        {
            this.name = name;
            this.In = pipeline.CreateReceiver<Hand>(this, Process, $"{name}-In");
            this.Out = pipeline.CreateEmitter<bool>(this, $"{name}-Out");
        }

        public override string ToString() => this.name;

        private void Process(Hand hand, Envelope enveloppe)
        {
           this.Out.Post(IsIndexExtended(hand) && IsMiddleGrabbing(hand) && IsRingGrabbing(hand) &&
                        IsLittleGrabbing(hand), enveloppe.OriginatingTime);
        }

        /// <summary>
        /// Returns true if the given hand's index finger tip is farther from the wrist than the index intermediate joint.
        /// </summary>
        /// <param name="hand">Hand to check for the required pose.</param>
        /// <returns>True if the given hand's index finger tip is farther from the wrist than the index intermediate joint, false otherwise.</returns>
        static bool IsIndexExtended(Hand hand)
        {
            return System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.IndexTip], hand.HandJoints[Hand.EHandJointID.Wrist]) >
                System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.IndexIntermediate], hand.HandJoints[Hand.EHandJointID.Wrist]);
        }

        /// <summary>
        /// Returns true if the given hand's middle finger tip is closer to the wrist than the middle proximal joint.
        /// </summary>
        /// <param name="hand">Hand to check for the required pose.</param>
        /// <returns>True if the given hand's middle finger tip is closer to the wrist than the middle proximal joint, false otherwise.</returns>
        static bool IsMiddleGrabbing(Hand hand)
        {
            return System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.MiddleProximal], hand.HandJoints[Hand.EHandJointID.Wrist]) >
              System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.MiddleTip], hand.HandJoints[Hand.EHandJointID.Wrist]);
        }

        /// <summary>
        /// Returns true if the given hand's ring finger tip is closer to the wrist than the ring proximal joint.
        /// </summary>
        /// <param name="hand">Hand to check for the required pose.</param>
        /// <returns>True if the given hand's ring finger tip is closer to the wrist than the ring proximal joint, false otherwise.</returns>
        static bool IsRingGrabbing(Hand hand)
        {
            return System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.RingProximal], hand.HandJoints[Hand.EHandJointID.Wrist]) >
                System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.RingTip], hand.HandJoints[Hand.EHandJointID.Wrist]);
        }

        /// <summary>
        /// Returns true if the given hand's little finger tip is closer to the wrist than the little proximal joint.
        /// </summary>
        /// <param name="hand">Hand to check for the required pose.</param>
        /// <returns>True if the given hand's little finger tip is closer to the wrist than the little proximal joint, false otherwise.</returns>
        static bool IsLittleGrabbing(Hand hand)
        {
            return System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.LittleProximal], hand.HandJoints[Hand.EHandJointID.Wrist]) >
                System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.LittleTip], hand.HandJoints[Hand.EHandJointID.Wrist]);
        }
    }
}
