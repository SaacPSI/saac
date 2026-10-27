// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Gestures
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using SAAC.GlobalHelpers;

    /// <summary>
    /// Component that detects a poke gesture from hand tracking data.
    /// A poke gesture is defined as: index finger extended, middle/ring/little fingers curled (grabbing).
    /// Based on Unity XR Interaction Toolkit.
    /// </summary>
    public class PokeDetector : IConsumerProducer<Hand, bool>
    {
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="PokeDetector"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">The name of the component.</param>
        public PokeDetector(Pipeline pipeline, string name = nameof(PokeDetector))
        {
            this.name = name;
            this.In = pipeline.CreateReceiver<Hand>(this, this.Process, $"{name}-In");
            this.Out = pipeline.CreateEmitter<bool>(this, $"{name}-Out");
        }

        /// <summary>
        /// Gets the receiver for input hand data.
        /// </summary>
        public Receiver<Hand> In { get; }

        /// <summary>
        /// Gets the emitter for poke detection output (true if poke gesture detected).
        /// </summary>
        public Emitter<bool> Out { get; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Processes hand data to detect poke gesture.
        /// </summary>
        /// <param name="hand">The hand tracking data.</param>
        /// <param name="enveloppe">The message envelope.</param>
        private void Process(Hand hand, Envelope enveloppe)
        {
            this.Out.Post(
                IsIndexExtended(hand) && IsMiddleGrabbing(hand) && IsRingGrabbing(hand) &&
                         IsLittleGrabbing(hand), enveloppe.OriginatingTime);
        }

        /// <summary>
        /// Returns true if the given hand's index finger tip is farther from the wrist than the index intermediate joint.
        /// </summary>
        /// <param name="hand">Hand to check for the required pose.</param>
        /// <returns>True if the given hand's index finger tip is farther from the wrist than the index intermediate joint, false otherwise.</returns>
        private static bool IsIndexExtended(Hand hand)
        {
            return System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.IndexTip], hand.HandJoints[Hand.EHandJointID.Wrist]) >
                System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.IndexIntermediate], hand.HandJoints[Hand.EHandJointID.Wrist]);
        }

        /// <summary>
        /// Returns true if the given hand's middle finger tip is closer to the wrist than the middle proximal joint.
        /// </summary>
        /// <param name="hand">Hand to check for the required pose.</param>
        /// <returns>True if the given hand's middle finger tip is closer to the wrist than the middle proximal joint, false otherwise.</returns>
        private static bool IsMiddleGrabbing(Hand hand)
        {
            return System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.MiddleProximal], hand.HandJoints[Hand.EHandJointID.Wrist]) >
              System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.MiddleTip], hand.HandJoints[Hand.EHandJointID.Wrist]);
        }

        /// <summary>
        /// Returns true if the given hand's ring finger tip is closer to the wrist than the ring proximal joint.
        /// </summary>
        /// <param name="hand">Hand to check for the required pose.</param>
        /// <returns>True if the given hand's ring finger tip is closer to the wrist than the ring proximal joint, false otherwise.</returns>
        private static bool IsRingGrabbing(Hand hand)
        {
            return System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.RingProximal], hand.HandJoints[Hand.EHandJointID.Wrist]) >
                System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.RingTip], hand.HandJoints[Hand.EHandJointID.Wrist]);
        }

        /// <summary>
        /// Returns true if the given hand's little finger tip is closer to the wrist than the little proximal joint.
        /// </summary>
        /// <param name="hand">Hand to check for the required pose.</param>
        /// <returns>True if the given hand's little finger tip is closer to the wrist than the little proximal joint, false otherwise.</returns>
        private static bool IsLittleGrabbing(Hand hand)
        {
            return System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.LittleProximal], hand.HandJoints[Hand.EHandJointID.Wrist]) >
                System.Numerics.Vector3.DistanceSquared(hand.HandJoints[Hand.EHandJointID.LittleTip], hand.HandJoints[Hand.EHandJointID.Wrist]);
        }
    }
}
