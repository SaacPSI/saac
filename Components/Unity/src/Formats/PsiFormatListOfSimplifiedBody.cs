using Microsoft.Psi.Interop.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SimplifiedBody
{
    public uint Id { get; set; } = uint.MaxValue;
    public enum SensorOrigin { Nuitrack, Azure, TeslaSuit };

    // From Kinect Azure
    public enum JointId
    {
        //
        // Summary:
        //     Pelvis
        //[NativeReference("K4ABT_JOINT_PELVIS")]
        Pelvis,
        //
        // Summary:
        //     Spine navel
        //[NativeReference("K4ABT_JOINT_SPINE_NAVEL")]
        SpineNavel,
        //
        // Summary:
        //     Spine chest
        //[NativeReference("K4ABT_JOINT_SPINE_CHEST")]
        SpineChest,
        //
        // Summary:
        //     Neck
        //[NativeReference("K4ABT_JOINT_NECK")]
        Neck,
        //
        // Summary:
        //     Left clavicle
        //[NativeReference("K4ABT_JOINT_CLAVICLE_LEFT")]
        ClavicleLeft,
        //
        // Summary:
        //     Left shoulder
        //[NativeReference("K4ABT_JOINT_SHOULDER_LEFT")]
        ShoulderLeft,
        //
        // Summary:
        //     Left elbow
        //[NativeReference("K4ABT_JOINT_ELBOW_LEFT")]
        ElbowLeft,
        //
        // Summary:
        //     Left wrist
        //[NativeReference("K4ABT_JOINT_WRIST_LEFT")]
        WristLeft,
        //
        // Summary:
        //     Left hand
        //[NativeReference("K4ABT_JOINT_HAND_LEFT")]
        HandLeft,
        //
        // Summary:
        //     Left hand tip
        //[NativeReference("K4ABT_JOINT_HANDTIP_LEFT")]
        HandTipLeft,
        //
        // Summary:
        //     Left thumb
        //[NativeReference("K4ABT_JOINT_THUMB_LEFT")]
        ThumbLeft,
        //
        // Summary:
        //     Right clavicle
        //[NativeReference("K4ABT_JOINT_CLAVICLE_RIGHT")]
        ClavicleRight,
        //
        // Summary:
        //     Right shoulder
        //[NativeReference("K4ABT_JOINT_SHOULDER_RIGHT")]
        ShoulderRight,
        //
        // Summary:
        //     Right elbow
        //[NativeReference("K4ABT_JOINT_ELBOW_RIGHT")]
        ElbowRight,
        //
        // Summary:
        //     Right wrist
        //[NativeReference("K4ABT_JOINT_WRIST_RIGHT")]
        WristRight,
        //
        // Summary:
        //     Right hand
        //[NativeReference("K4ABT_JOINT_HAND_RIGHT")]
        HandRight,
        //
        // Summary:
        //     Right hand tip
        //[NativeReference("K4ABT_JOINT_HANDTIP_RIGHT")]
        HandTipRight,
        //
        // Summary:
        //     Right thumb
        //[NativeReference("K4ABT_JOINT_THUMB_RIGHT")]
        ThumbRight,
        //
        // Summary:
        //     Left hip
        //[NativeReference("K4ABT_JOINT_HIP_LEFT")]
        HipLeft,
        //
        // Summary:
        //     Left knee
        //[NativeReference("K4ABT_JOINT_KNEE_LEFT")]
        KneeLeft,
        //
        // Summary:
        //     Left ankle
        //[NativeReference("K4ABT_JOINT_ANKLE_LEFT")]
        AnkleLeft,
        //
        // Summary:
        //     Left foot
        //[NativeReference("K4ABT_JOINT_FOOT_LEFT")]
        FootLeft,
        //
        // Summary:
        //     Right hip
        //[NativeReference("K4ABT_JOINT_HIP_RIGHT")]
        HipRight,
        //
        // Summary:
        //     Right knee
        //[NativeReference("K4ABT_JOINT_KNEE_RIGHT")]
        KneeRight,
        //
        // Summary:
        //     Right ankle
        //[NativeReference("K4ABT_JOINT_ANKLE_RIGHT")]
        AnkleRight,
        //
        // Summary:
        //     Right foot
        //[NativeReference("K4ABT_JOINT_FOOT_RIGHT")]
        FootRight,
        //
        // Summary:
        //     Head
        //[NativeReference("K4ABT_JOINT_HEAD")]
        Head,
        //
        // Summary:
        //     Nose
        //[NativeReference("K4ABT_JOINT_NOSE")]
        Nose,
        //
        // Summary:
        //     Left eye
        //[NativeReference("K4ABT_JOINT_EYE_LEFT")]
        EyeLeft,
        //
        // Summary:
        //     Left ear
        //[NativeReference("K4ABT_JOINT_EAR_LEFT")]
        EarLeft,
        //
        // Summary:
        //     Right eye
        //[NativeReference("K4ABT_JOINT_EYE_RIGHT")]
        EyeRight,
        //
        // Summary:
        //     Right ear
        //[NativeReference("K4ABT_JOINT_EAR_RIGHT")]
        EarRight,
        //
        // Summary:
        //     Number of different joints defined in this enumeration.
        //[NativeReference("K4ABT_JOINT_COUNT")]
        Count
    }

    public enum JointConfidenceLevel
    {
        //
        // Summary:
        //     The joint is out of range (too far from depth camera)
        //[NativeReference("K4ABT_JOINT_CONFIDENCE_NONE")]
        None,
        //
        // Summary:
        //     The joint is not observed (likely due to occlusion), predicted joint pose
        //[NativeReference("K4ABT_JOINT_CONFIDENCE_LOW")]
        Low,
        //
        // Summary:
        //     Medium confidence in joint pose. Current SDK will only provide joints up to this
        //     confidence level
        //[NativeReference("K4ABT_JOINT_CONFIDENCE_MEDIUM")]
        Medium,
        //
        // Summary:
        //     High confidence in joint pose. Placeholder for future SDK
        //[NativeReference("K4ABT_JOINT_CONFIDENCE_HIGH")]
        High,
        //
        // Summary:
        //     The total number of confidence levels.
        //[NativeReference("K4ABT_JOINT_CONFIDENCE_LEVELS_COUNT")]
        Count
    }

    public SensorOrigin Origin { get; private set; }
    public Dictionary<JointId, Tuple<JointConfidenceLevel, Vector3>> Joints { get; set; }

    public SimplifiedBody(SensorOrigin origin, uint id, Dictionary<JointId, Tuple<JointConfidenceLevel, Vector3>>? joints = null)
    {
        Origin = origin;
        Id = id;
        Joints = joints ?? new Dictionary<JointId, Tuple<JointConfidenceLevel, Vector3>>();
    }
}

public class PsiFormatListOfSimplifiedBody 
{
    public static Format<List<SimplifiedBody>> GetFormat()
    {
        return new Format<List<SimplifiedBody>>(WriteSimplifiedBodies, ReadSimplifiedBodies);
    }

    public static void WriteSimplifiedBodies(List<SimplifiedBody> bodies, BinaryWriter writer)
    {
        writer.Write(bodies.Count);
        foreach (SimplifiedBody body in bodies)
        {
            writer.Write(body.Id);
            writer.Write((int)body.Origin);
            writer.Write(body.Joints.Count);
            foreach (var joint in body.Joints)
            {
                writer.Write((int)joint.Key);
                writer.Write((int)joint.Value.Item1);
                writer.Write((double)joint.Value.Item2.x);
                writer.Write((double)joint.Value.Item2.y);
                writer.Write((double)joint.Value.Item2.z);
            };
        }
    }

    public static List<SimplifiedBody> ReadSimplifiedBodies(BinaryReader reader)
    {
        List<SimplifiedBody> bodies = new List<SimplifiedBody>();
        int count = reader.ReadInt32();
        for (int bodiesIterator = 0; bodiesIterator < count; bodiesIterator++)
        {
            uint id = reader.ReadUInt32();
            SimplifiedBody.SensorOrigin origin = (SimplifiedBody.SensorOrigin)reader.ReadInt32();
            int jointCount = reader.ReadInt32();
            Dictionary<SimplifiedBody.JointId, Tuple<SimplifiedBody.JointConfidenceLevel, Vector3>> joints = new Dictionary<SimplifiedBody.JointId, Tuple<SimplifiedBody.JointConfidenceLevel, Vector3>>();
            for (int jointIterator = 0; jointIterator < jointCount; jointIterator++)
            {
                joints.Add((SimplifiedBody.JointId)reader.ReadInt32(), new Tuple<SimplifiedBody.JointConfidenceLevel, Vector3>((SimplifiedBody.JointConfidenceLevel)reader.ReadInt32(),
                    new Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble())));
            }
            bodies.Add(new SimplifiedBody(origin, id, joints));
        }
        return bodies;
    }
}
