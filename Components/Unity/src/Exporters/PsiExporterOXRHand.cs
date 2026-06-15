using Microsoft.Psi.Interop.Serialization;
using SAAC.PsiFormats;
using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Transport;
using UnityEngine;
using UnityEngine.XR.Hands;
using System.Collections.Generic;
using System;
using Meta.WitAi;

public class PsiExporterOXRHand : PsiExporter<SAAC.GlobalHelpers.Hand>
{
    public Transform LeftReference;
    public Transform RightReference;
    public XRHandJointID JointReference;
    public Emitter<SAAC.GlobalHelpers.Hand> Out2 { get; private set; }

    private XRHandSubsystem HandSubsystem;

    public void Initialize()
    {
        try
        {
            Out = PsiManager.GetPipeline().CreateEmitter<SAAC.GlobalHelpers.Hand>(this, $"{TopicName}-Left");
            Out2 = PsiManager.GetPipeline().CreateEmitter<SAAC.GlobalHelpers.Hand>(this, $"{TopicName}-Right");
            switch (ExportType)
            {
#if PSI_TCP_STREAMS
                case PsiPipelineManager.ExportType.TCPWriter:
                    TcpWriter<SAAC.GlobalHelpers.Hand> tcpWriter = PsiManager.GetTcpWriter<SAAC.GlobalHelpers.Hand>($"{TopicName}-Left", GetSerializer());
                    Out.PipeTo(tcpWriter);
                    TcpWriter<SAAC.GlobalHelpers.Hand> tcpWriter2 = PsiManager.GetTcpWriter<SAAC.GlobalHelpers.Hand>($"{TopicName}-Right", GetSerializer());
                    Out2.PipeTo(tcpWriter2);
                    break;
#endif
                default:
                    {
                        RemoteExporter exporter;
                        PsiManager.GetRemoteExporter(ExportType, out exporter);
                        exporter.Exporter.Write(Out, $"{TopicName}-Left");
                        exporter.Exporter.Write(Out2, $"{TopicName}-Right");
                        PsiManager.RegisterExporter(ref exporter);
                    }
                    break;
            }
            base.IsInitialized = true;
        }
        catch (Exception e)
        {
            PsiManager.AddLog($"PsiExporter Exception: {e.Message} \n {e.InnerException} \n {e.Source} \n {e.StackTrace}");
        }
    }

    protected void OnEnable()
    {
        List<XRHandSubsystem> availableSubsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(availableSubsystems);
        if (availableSubsystems.Count == 0)
        {
            Debug.LogError("No hand tracking system found !", this);
            return;
        }

        HandSubsystem = availableSubsystems[0];
        HandSubsystem.updatedHands += OnUpdatedHands;
    }

    private void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags flags, XRHandSubsystem.UpdateType type)
    {
        if (flags == XRHandSubsystem.UpdateSuccessFlags.None || type == XRHandSubsystem.UpdateType.Dynamic || !CanSend())
            return;
        if (subsystem.leftHand.isTracked)
        {
            SAAC.GlobalHelpers.Hand left = SAAC.GlobalHelpers.Hand.CreateHand(SAAC.GlobalHelpers.Hand.EHandType.Left, SAAC.GlobalHelpers.Hand.EOrigin.OpenXR);
            CalculateOffset(JointReference, subsystem.leftHand, LeftReference, out Vector3 offset);
            ProcessRootPose(subsystem.leftHand.rootPose, ref left, offset);
            processHandJoints(subsystem.leftHand, ref left, offset);
            Out.Post(left, Timestamp);
        }
        if (subsystem.rightHand.isTracked)
        {
            SAAC.GlobalHelpers.Hand right = SAAC.GlobalHelpers.Hand.CreateHand(SAAC.GlobalHelpers.Hand.EHandType.Right, SAAC.GlobalHelpers.Hand.EOrigin.OpenXR);
            CalculateOffset(JointReference, subsystem.rightHand, RightReference, out Vector3 offset);
            ProcessRootPose(subsystem.rightHand.rootPose, ref right, offset);
            processHandJoints(subsystem.rightHand, ref right, offset);
            Out2.Post(right, Timestamp);
        }
    }

    static private void CalculateOffset(XRHandJointID joint, XRHand hand, Transform transform, out Vector3 offset)
    {
        Vector3 basePos;
        if (joint == XRHandJointID.Invalid || !hand.GetJoint(joint).TryGetPose(out Pose pose))
            basePos = hand.rootPose.position;
        else
            basePos = pose.position;
        offset = transform.position - basePos;
    }

    static private void ProcessRootPose(in Pose root, ref SAAC.GlobalHelpers.Hand psiHand, Vector3 offset)
    {
        psiHand.RootPosition = new System.Numerics.Vector3(offset.x, offset.y, offset.z);
        psiHand.RootOrientation = new System.Numerics.Quaternion(root.rotation.x, root.rotation.y, root.rotation.z, root.rotation.w);
    }

    static private void processHandJoints(in XRHand hand, ref SAAC.GlobalHelpers.Hand psiHand, Vector3 offset)
    {
        for (XRHandJointID jointIterator = XRHandJointID.BeginMarker; jointIterator < XRHandJointID.EndMarker; jointIterator++)
            if (hand.GetJoint(jointIterator).TryGetPose(out var Pose))
                psiHand.HandJoints.Add((SAAC.GlobalHelpers.Hand.EHandJointID)jointIterator, new System.Numerics.Vector3(Pose.position.x + offset.x, Pose.position.y + offset.y, Pose.position.z + offset.z));
    }

    protected override IFormatSerializer<SAAC.GlobalHelpers.Hand> GetSerializer()
    {
        return SAAC.PsiFormats.PsiFormatHand.GetFormat();
    }
}
