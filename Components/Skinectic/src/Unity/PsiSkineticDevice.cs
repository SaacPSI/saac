using Microsoft.Psi;
using Microsoft.Psi.Common;
using Microsoft.Psi.Interop.Transport;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Serialization;
using Skinetic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class SkineticHapticEffectSerializer : PsiASerializer<SkineticHapticEffect>
{
    public override void Serialize(BufferWriter writer, SkineticHapticEffect instance, SerializationContext context) { }
    public override void Deserialize(BufferReader reader, ref SkineticHapticEffect target, SerializationContext context) { }
}

[RequireComponent(typeof(SkineticDevice))]
public class PsiSkineticDevice : SkineticDevice
{
    public string HapticEffectTopicName = "HapticEffect";
    public float DataPerSecond = 0.0f;
    public PsiPipelineManager.ExportType ExportType = PsiPipelineManager.ExportType.Unknow;
    public int EffectBoost = 0;

    protected PsiPipelineManager PsiManager;
    public Emitter<SkineticHapticEffect> HapticEffectOut { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    protected float DataTime;
    protected DateTime Timestamp = DateTime.UtcNow;

    // Start is called before the first frame update
    void Start()
    {
        DataTime = DataPerSecond == 0.0f ? 0.0f : 1.0f / DataPerSecond;
        PsiManager = GameObject.FindAnyObjectByType<PsiPipelineManager>();
        if (PsiManager == null)
        {
            Debug.LogError("Could not found PsiPipelineManager script ! Have you put it in your scene ?");
            return;
        }
        PsiManager.onInitialized += Initialize;
        PsiManager.Serializers.Register<SkineticHapticEffect, SkineticHapticEffectSerializer>();
    }

    public void Initialize()
    {
        try
        {
            HapticEffectOut = PsiManager.GetPipeline().CreateEmitter<SkineticHapticEffect>(this, HapticEffectTopicName);
            switch (ExportType)
            {
#if PSI_TCP_STREAMS
                case PsiPipelineManager.ExportType.TCPWriter:
                    TcpWriter<SkineticHapticEffect> tcpWriterTouch = PsiManager.GetTcpWriter<SkineticHapticEffect>(HapticEffectTopicName, GetSerializerHapticEffect());
                    HapticEffectOut.PipeTo(tcpWriterTouch);
                    break;
#endif
                default:
                    {
                        RemoteExporter exporter;
                        PsiManager.GetRemoteExporter(ExportType, out exporter);
                        exporter.Exporter.Write(HapticEffectOut, HapticEffectTopicName);
                        PsiManager.RegisterExporter(ref exporter);
                    }
                    break;
            }
            PsiImporterInteger importerInteger = GetComponent<PsiImporterInteger>();
            if (importerInteger != null && importerInteger.TopicName == "BoostEffect")
            {
                importerInteger.onRecieved += ModifyEffectBoost;
            }
            IsInitialized = true;
        }
        catch (Exception e)
        {
            PsiManager.AddLog($"PsiExporter Exception: {e.Message} \n {e.InnerException} \n {e.Source} \n {e.StackTrace}");
        }
    }

    protected bool CanSend()
    {
        if (IsInitialized && PsiManager.IsRunning() && (DataTime == 0.0f || GetCurrentTime().Subtract(Timestamp).TotalSeconds > DataTime))
        {
            Timestamp = GetCurrentTime();
            return true;
        }
        return false;
    }

    protected DateTime GetCurrentTime()
    {
        return PsiManager.GetPipeline().GetCurrentTime();
    }

    public override bool PlayEffect(HapticEffect hapticEffect)
    {
        hapticEffect.EffectBoost += EffectBoost;
        bool isSuccess = base.PlayEffect(hapticEffect);
        if (isSuccess && CanSend())
            HapticEffectOut.Post(new SkineticHapticEffect(hapticEffect), Timestamp);
        return isSuccess;
    }

    public override bool StopEffect(HapticEffect hapticEffect, float time)
    {
        bool isSuccess = base.StopEffect(hapticEffect, time);
        if (isSuccess && CanSend())
            HapticEffectOut.Post(new SkineticHapticEffect(hapticEffect), Timestamp);
        return isSuccess;
    }

    protected void ModifyEffectBoost(int value, DateTime time)
    {
        EffectBoost = value;
    }

#if PSI_TCP_STREAMS
    protected Microsoft.Psi.Interop.Serialization.IFormatSerializer<SkineticHapticEffect> GetSerializerHapticEffect()
    {
        return PsiFormatSkineticHapticEffect.GetFormat();
    }
#endif
}
