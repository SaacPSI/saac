using Microsoft.Psi.Common;
using Microsoft.Psi.Serialization;
using System;
using System.Collections.Generic;
using TsAPI.Types;
using TsSDK;
using UnityEngine;


public class TsMotionSerializer : PsiASerializer<Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4>>
{
    public override void Serialize(BufferWriter writer, Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4> instance, SerializationContext context) { }
    public override void Deserialize(BufferReader reader, ref Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4> target, SerializationContext context) { }
}

// Adaptation of TsHumanAnimator.cs
public class PsiExporterTsMotion : PsiExporter<Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4>>
{
    [SerializeField]
    private TsMotionProvider m_motionProvider;

    [SerializeField]
    private TsAvatarSettings m_avatarSettings;

    private Dictionary<TsHumanBoneIndex, Transform> m_bonesTransforms = new Dictionary<TsHumanBoneIndex, Transform>();
    private TsHumanBoneIndex m_rootBone = TsHumanBoneIndex.Hips;

    override public void Start()
    {
        base.Start();
        PsiManager.Serializers.Register<Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4>, TsMotionSerializer>();
        if (m_avatarSettings == null)
        {
            Debug.LogError("Missing avatar settings for this character.");
            enabled = false;
            return;
        }

        if (!m_avatarSettings.IsValid)
        {
            Debug.LogError("Invalid avatar settings for this character. Check that all required bones is configured correctly.");
            enabled = false;
            return;
        }

        SetupAvatarBones();
    }

    private void SetupAvatarBones()
    {
        foreach (var reqBoneIndex in TsHumanBones.SuitBones)
        {
            var transformName = m_avatarSettings.GetTransformName(reqBoneIndex);
            var boneTransform = TransformUtils.FindChildRecursive(transform, transformName);
            if (boneTransform != null && !m_bonesTransforms.ContainsKey(reqBoneIndex))
            {
                m_bonesTransforms.Add(reqBoneIndex, boneTransform);
            }
        }
        if (m_bonesTransforms.Count == 0)
        {
            this.enabled = false;
            PsiManager.AddLog($"ERROR : PsiExporterTsMotion failed to SetupAvatarBones, is the script in a bone setup ?");
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (CanSend() && m_motionProvider.Running && Update(m_motionProvider.GetSkeleton(Time.time)))
        {
            Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4> data = new Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4>();
            foreach (var bone in m_bonesTransforms)
                data.Add(bone.Key, TransformToMatrix(bone.Value));
            Out.Post(data, Timestamp);
        }
    }

    public bool IPose = false;
    private bool Update(ISkeleton skeleton)
    {
        if (skeleton == null)
        {
            return false;
        }
        foreach (var boneIndex in TsHumanBones.SuitBones)
        {
            var poseRotation = m_avatarSettings.GetIPoseRotation(boneIndex);
            var targetRotation = Conversion.TsRotationToUnityRotation(skeleton.GetBoneTransform(boneIndex).rotation);

            TryDoWithBone(boneIndex, (boneTransform) =>
            {
                boneTransform.rotation = targetRotation * poseRotation;
            });
        }

        TryDoWithBone(m_rootBone, (boneTransform) =>
        {
            var hipsPos = skeleton.GetBoneTransform(TsHumanBoneIndex.Hips).position;
            boneTransform.transform.position = Conversion.TsVector3ToUnityVector3(hipsPos);
        });

        if (IPose)
        {
            m_motionProvider.Calibrate();
            IPose = false;
        }
        return true;
    }

    public void Calibrate()
    {
        m_motionProvider?.Calibrate();
    }

    private void TryDoWithBone(TsHumanBoneIndex boneIndex, Action<Transform> action)
    {
        if (!m_bonesTransforms.TryGetValue(boneIndex, out var boneTransform))
        {
            return;
        }

        action(boneTransform);
    }

    private System.Numerics.Matrix4x4 TransformToMatrix(Transform transform)
    {
        return new System.Numerics.Matrix4x4(transform.worldToLocalMatrix[0, 0], transform.worldToLocalMatrix[0, 1], transform.worldToLocalMatrix[0, 2], transform.worldToLocalMatrix[0, 3],
                                                                          transform.worldToLocalMatrix[1, 0], transform.worldToLocalMatrix[1, 1], transform.worldToLocalMatrix[1, 2], transform.worldToLocalMatrix[1, 3],
                                                                          transform.worldToLocalMatrix[2, 0], transform.worldToLocalMatrix[2, 1], transform.worldToLocalMatrix[2, 2], transform.worldToLocalMatrix[2, 3],
                                                                          transform.worldToLocalMatrix[3, 0], transform.worldToLocalMatrix[3, 1], transform.worldToLocalMatrix[3, 2], transform.worldToLocalMatrix[3, 3]); 
    }


#if PLATFORM_ANDROID
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4>> GetSerializer()
    {
        return PsiFormatTsMotion.GetFormat();
    }
#endif
}
