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

/// <summary>
/// Provides serialization for Skinetic haptic effect data in Psi pipelines.
/// </summary>
public class SkineticHapticEffectSerializer : PsiASerializer<SkineticHapticEffect>
{
    /// <inheritdoc/>
    public override void Serialize(BufferWriter writer, SkineticHapticEffect instance, SerializationContext context)
    {
    }

    /// <inheritdoc/>
    public override void Deserialize(BufferReader reader, ref SkineticHapticEffect target, SerializationContext context)
    {
    }
}

/// <summary>
/// Unity component that integrates Skinetic haptic device with Psi pipeline.
/// </summary>
[RequireComponent(typeof(SkineticDevice))]
public class PsiSkineticDevice : SkineticDevice
{
    /// <summary>
    /// Gets or sets the topic name for haptic effect data.
    /// </summary>
    public string HapticEffectTopicName = "HapticEffect";

    /// <summary>
    /// Gets or sets the data transmission rate per second.
    /// </summary>
    public float DataPerSecond = 0.0f;

    /// <summary>
    /// Gets or sets the export type for the Psi pipeline.
    /// </summary>
    public PsiPipelineManager.ExportType ExportType = PsiPipelineManager.ExportType.Unknow;

    /// <summary>
    /// Gets or sets the boost value applied to haptic effects.
    /// </summary>
    public int EffectBoost = 0;

    /// <summary>
    /// Gets the haptic effect output emitter.
    /// </summary>
    public Emitter<SkineticHapticEffect> HapticEffectOut { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the device is initialized.
    /// </summary>
    public bool IsInitialized { get; private set; } = false;

    /// <summary>
    /// Gets or sets the Psi pipeline manager.
    /// </summary>
    protected PsiPipelineManager PsiManager;

    /// <summary>
    /// Gets or sets the time interval for data transmission.
    /// </summary>
    protected float DataTime;

    /// <summary>
    /// Gets or sets the current timestamp.
    /// </summary>
    protected DateTime Timestamp = DateTime.UtcNow;

    /// <summary>
    /// Unity Start method called before the first frame update.
    /// </summary>
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

    /// <summary>
    /// Initializes the Psi pipeline connections for the Skinetic device.
    /// </summary>
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

    /// <summary>
    /// Determines whether data can be sent based on timing constraints.
    /// </summary>
    /// <returns>True if data can be sent; otherwise, false.</returns>
    protected bool CanSend()
    {
        if (IsInitialized && PsiManager.IsRunning() && (DataTime == 0.0f || GetCurrentTime().Subtract(Timestamp).TotalSeconds > DataTime))
        {
            Timestamp = GetCurrentTime();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the current time from the Psi pipeline.
    /// </summary>
    /// <returns>The current pipeline time.</returns>
    protected DateTime GetCurrentTime()
    {
        return PsiManager.GetPipeline().GetCurrentTime();
    }

    /// <summary>
    /// Plays a haptic effect and posts it to the Psi pipeline.
    /// </summary>
    /// <param name="hapticEffect">The haptic effect to play.</param>
    /// <returns>True if the effect was successfully played; otherwise, false.</returns>
    public override bool PlayEffect(HapticEffect hapticEffect)
    {
        hapticEffect.EffectBoost += EffectBoost;
        bool isSuccess = base.PlayEffect(hapticEffect);
        if (isSuccess && CanSend())
            HapticEffectOut.Post(new SkineticHapticEffect(hapticEffect), Timestamp);
        return isSuccess;
    }

    /// <summary>
    /// Stops a haptic effect and posts the stop event to the Psi pipeline.
    /// </summary>
    /// <param name="hapticEffect">The haptic effect to stop.</param>
    /// <param name="time">The time duration for stopping the effect.</param>
    /// <returns>True if the effect was successfully stopped; otherwise, false.</returns>
    public override bool StopEffect(HapticEffect hapticEffect, float time)
    {
        bool isSuccess = base.StopEffect(hapticEffect, time);
        if (isSuccess && CanSend())
            HapticEffectOut.Post(new SkineticHapticEffect(hapticEffect), Timestamp);
        return isSuccess;
    }

    /// <summary>
    /// Modifies the effect boost value.
    /// </summary>
    /// <param name="value">The new boost value.</param>
    /// <param name="time">The timestamp of the modification.</param>
    protected void ModifyEffectBoost(int value, DateTime time)
    {
        EffectBoost = value;
    }

#if PSI_TCP_STREAMS
/// <summary>
/// Gets the serializer for haptic effect data.
/// </summary>
/// <returns>The format serializer for Skinetic haptic effects.</returns>
protected Microsoft.Psi.Interop.Serialization.IFormatSerializer<SkineticHapticEffect> GetSerializerHapticEffect()
{
        return PsiFormatSkineticHapticEffect.GetFormat();
    }
#endif
}
