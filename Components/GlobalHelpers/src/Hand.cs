using System;
using System.Collections.Generic;
using System.Numerics;

namespace SAAC.GlobalHelpers
{
    public class Hand
    {
        public enum EHandJointID // taking the OpenXR def for the moment
        {
            Invalid = 0,
            Wrist = 1,
            Palm = 2,
            ThumbMetacarpal = 3,
            ThumbProximal = 4,
            ThumbDistal = 5,
            ThumbTip = 6,
            IndexMetacarpal = 7,
            IndexProximal = 8,
            IndexIntermediate = 9,
            IndexDistal = 10,
            IndexTip = 11,
            MiddleMetacarpal = 12,
            MiddleProximal = 13,
            MiddleIntermediate = 14,
            MiddleDistal = 15,
            MiddleTip = 16,
            RingMetacarpal = 17,
            RingProximal = 18,
            RingIntermediate = 19,
            RingDistal = 20,
            RingTip = 21,
            LittleMetacarpal = 22,
            LittleProximal = 23,
            LittleIntermediate = 24,
            LittleDistal = 25,
            LittleTip = 26
        };

        public static List<(EHandJointID, EHandJointID)> Bones = new List<(EHandJointID, EHandJointID)>()
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
        public enum EHandType { Left, Right };
        public EHandType Type;

        public enum EOrigin { OpenXR, Meta };
        public EOrigin Origin;

        public Dictionary<EHandJointID, System.Numerics.Vector3> HandJoints;

        public System.Numerics.Vector3 RootPosition;
        public System.Numerics.Quaternion RootOrientation;

        public Hand()
        {
            HandJoints = new Dictionary<EHandJointID, System.Numerics.Vector3>();
        }

        public static Hand CreateHand(EHandType type, EOrigin origin)
        {
            Hand hand = new Hand();
            hand.Type = type;
            hand.Origin = origin;
            return hand;
        }
    }
}
