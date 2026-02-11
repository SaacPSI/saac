// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.GlobalHelpers
{
    /// <summary>
    /// Represents a hand with joint tracking data following OpenXR definitions.
    /// </summary>
    public class Hand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Hand"/> class.
        /// </summary>
        public Hand()
        {
            this.HandJoints = new Dictionary<EHandJointID, System.Numerics.Vector3>();
        }

        /// <summary>
        /// Enumeration of hand joint identifiers following OpenXR definitions.
        /// </summary>
        public enum EHandJointID
        {
            /// <summary>Invalid joint.</summary>
            Invalid = 0,

            /// <summary>Wrist joint.</summary>
            Wrist = 1,

            /// <summary>Palm joint.</summary>
            Palm = 2,

            /// <summary>Thumb metacarpal joint.</summary>
            ThumbMetacarpal = 3,

            /// <summary>Thumb proximal joint.</summary>
            ThumbProximal = 4,

            /// <summary>Thumb distal joint.</summary>
            ThumbDistal = 5,

            /// <summary>Thumb tip.</summary>
            ThumbTip = 6,

            /// <summary>Index metacarpal joint.</summary>
            IndexMetacarpal = 7,

            /// <summary>Index proximal joint.</summary>
            IndexProximal = 8,

            /// <summary>Index intermediate joint.</summary>
            IndexIntermediate = 9,

            /// <summary>Index distal joint.</summary>
            IndexDistal = 10,

            /// <summary>Index finger tip.</summary>
            IndexTip = 11,

            /// <summary>Middle metacarpal joint.</summary>
            MiddleMetacarpal = 12,

            /// <summary>Middle proximal joint.</summary>
            MiddleProximal = 13,

            /// <summary>Middle intermediate joint.</summary>
            MiddleIntermediate = 14,

            /// <summary>Middle distal joint.</summary>
            MiddleDistal = 15,

            /// <summary>Middle finger tip.</summary>
            MiddleTip = 16,

            /// <summary>Ring metacarpal joint.</summary>
            RingMetacarpal = 17,

            /// <summary>Ring proximal joint.</summary>
            RingProximal = 18,

            /// <summary>Ring intermediate joint.</summary>
            RingIntermediate = 19,

            /// <summary>Ring distal joint.</summary>
            RingDistal = 20,

            /// <summary>Ring finger tip.</summary>
            RingTip = 21,

            /// <summary>Little metacarpal joint.</summary>
            LittleMetacarpal = 22,

            /// <summary>Little proximal joint.</summary>
            LittleProximal = 23,

            /// <summary>Little intermediate joint.</summary>
            LittleIntermediate = 24,

            /// <summary>Little distal joint.</summary>
            LittleDistal = 25,

            /// <summary>Little finger tip.</summary>
            LittleTip = 26
        }

        /// <summary>
        /// Enumeration of hand types.
        /// </summary>
        public enum EHandType
        {
            /// <summary>Left hand.</summary>
            Left,

            /// <summary>Right hand.</summary>
            Right
        }

        /// <summary>
        /// Enumeration of hand tracking origins.
        /// </summary>
        public enum EOrigin
        {
            /// <summary>OpenXR tracking system.</summary>
            OpenXR,

            /// <summary>Meta Quest tracking system.</summary>
            Meta
        }

        /// <summary>
        /// Gets or sets the list of bone connections between hand joints.
        /// </summary>
        public static List<(EHandJointID, EHandJointID)> Bones { get; set; } = new List<(EHandJointID, EHandJointID)>()
        {
            // Wrist to Palm
            (EHandJointID.Wrist, EHandJointID.Palm),

            // Thumb
            (EHandJointID.Palm, EHandJointID.ThumbMetacarpal),
            (EHandJointID.ThumbMetacarpal, EHandJointID.ThumbProximal),
            (EHandJointID.ThumbProximal, EHandJointID.ThumbDistal),
            (EHandJointID.ThumbDistal, EHandJointID.ThumbTip),

            // Index
            (EHandJointID.Palm, EHandJointID.IndexMetacarpal),
            (EHandJointID.IndexMetacarpal, EHandJointID.IndexProximal),
            (EHandJointID.IndexProximal, EHandJointID.IndexIntermediate),
            (EHandJointID.IndexIntermediate, EHandJointID.IndexDistal),
            (EHandJointID.IndexDistal, EHandJointID.IndexTip),

            // Middle
            (EHandJointID.Palm, EHandJointID.MiddleMetacarpal),
            (EHandJointID.MiddleMetacarpal, EHandJointID.MiddleProximal),
            (EHandJointID.MiddleProximal, EHandJointID.MiddleIntermediate),
            (EHandJointID.MiddleIntermediate, EHandJointID.MiddleDistal),
            (EHandJointID.MiddleDistal, EHandJointID.MiddleTip),

            // Ring
            (EHandJointID.Palm, EHandJointID.RingMetacarpal),
            (EHandJointID.RingMetacarpal, EHandJointID.RingProximal),
            (EHandJointID.RingProximal, EHandJointID.RingIntermediate),
            (EHandJointID.RingIntermediate, EHandJointID.RingDistal),
            (EHandJointID.RingDistal, EHandJointID.RingTip),

            // Little (Pinky)
            (EHandJointID.Palm, EHandJointID.LittleMetacarpal),
            (EHandJointID.LittleMetacarpal, EHandJointID.LittleProximal),
            (EHandJointID.LittleProximal, EHandJointID.LittleIntermediate),
            (EHandJointID.LittleIntermediate, EHandJointID.LittleDistal),
            (EHandJointID.LittleDistal, EHandJointID.LittleTip)
        };

        /// <summary>
        /// Gets or sets the hand type (left or right).
        /// </summary>
        public EHandType Type { get; set; }

        /// <summary>
        /// Gets or sets the tracking system origin.
        /// </summary>
        public EOrigin Origin { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of hand joints with their 3D positions.
        /// </summary>
        public Dictionary<EHandJointID, System.Numerics.Vector3> HandJoints { get; set; }

        /// <summary>
        /// Gets or sets the root position of the hand.
        /// </summary>
        public System.Numerics.Vector3 RootPosition { get; set; }

        /// <summary>
        /// Gets or sets the root orientation of the hand.
        /// </summary>
        public System.Numerics.Quaternion RootOrientation { get; set; }

        /// <summary>
        /// Creates a new hand with the specified type and origin.
        /// </summary>
        /// <param name="type">The hand type (left or right).</param>
        /// <param name="origin">The tracking system origin.</param>
        /// <returns>A new hand instance.</returns>
        public static Hand CreateHand(EHandType type, EOrigin origin)
        {
            Hand hand = new Hand();
            hand.Type = type;
            hand.Origin = origin;
            return hand;
        }
    }
}
