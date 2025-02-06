using Microsoft.Psi;
using Microsoft.Psi.Common;
using Microsoft.Psi.Interop.Transport;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Serialization;
using System;
using TsAPI.Types;
using TsSDK;
using Unity.VisualScripting;
using UnityEngine;
using SAAC.PsiFormat;

public struct HapticParams
{
    public int Frequency { get; private set; }
    public int Amplitude { get; private set; }
    public int PulseWidth { get; private set; }
    public long Duration { get; private set; }

    public HapticParams(int frequency, int amplitude, int pulseWidth, long duration)
    {
        Frequency = frequency;
        Amplitude = amplitude;
        PulseWidth = pulseWidth;
        Duration = duration;
    }
}

public struct HapticPlayable
{
    public ulong Id   { get; private set; }
    public HapticParams HapticParams { get; private set; }

    public HapticPlayable(ulong id, HapticParams hapticParams)
    {
        Id = id;
        HapticParams = hapticParams;
    }

    public HapticPlayable(ulong id, int frequency, int amplitude, int pulseWidth, long duration)
    {
        Id = id;
        HapticParams = new HapticParams(frequency, amplitude, pulseWidth, duration);
    }
}

public class HapticParamsSerializer : PsiASerializer<HapticParams>
{
    public override void Serialize(BufferWriter writer, HapticParams instance, SerializationContext context) { }
    public override void Deserialize(BufferReader reader, ref HapticParams target, SerializationContext context) { }
}

public class HapticPlayableSerializer : PsiASerializer<HapticPlayable>
{
    public override void Serialize(BufferWriter writer, HapticPlayable instance, SerializationContext context) { }
    public override void Deserialize(BufferReader reader, ref HapticPlayable target, SerializationContext context) { }
}

[RequireComponent(typeof(TsDeviceBehaviour))]
public class PsiTsHapicPlayer : TsHapticPlayer
{
    public string TouchTopicName = "HapticTouch";
    public string PlaybleTopicName = "HapticPlayable";
    public float DataPerSecond = 0.0f;
    public PsiPipelineManager.ExportType ExportType = PsiPipelineManager.ExportType.Unknow;

    protected PsiPipelineManager PsiManager;
    public Emitter<HapticParams> TouchOut { get; private set; }
    public Emitter<HapticPlayable> PlayablehOut { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    protected float DataTime;
    protected DateTime Timestamp = DateTime.UtcNow;

    /// <summary>
    /// Returns IHapticPlayable by given haptic <paramref name="asset"/>. Should be used to access cross-device playable instance.
    /// </summary>
    /// <param name="asset">Asset used to get playable.</param>
    /// <returns>IHapticPlayable interface</returns>
    public override IHapticPlayable GetPlayable(IHapticAsset asset)
    {
        if (m_hapticPlayer == null)
        {
            return null;
        }

        IHapticPlayable hapticPlayable = m_hapticPlayer.GetPlayable(asset);
        if (hapticPlayable != null && CanSend())
        {
            int frequency = -1;
            int amplitude = -1;
            int pulseWidth = -1;
            foreach (TsHapticParamMultiplier param in  hapticPlayable.Multipliers)
            {
                switch(param.type)
                {
                    default:
                    case TsHapticParamType.Undefined:
                    case TsHapticParamType.Temperature:
                        break;

                    case TsHapticParamType.Period:
                        frequency = (int)param.value * 1000000;
                        break;
                   case TsHapticParamType.Amplitude:
                        amplitude = (int)param.value;
                        break;
                    case TsHapticParamType.PulseWidth:
                        pulseWidth = (int)param.value;
                        break;
                }
            }
            PlayablehOut.Post(new HapticPlayable(hapticPlayable.Id, frequency, amplitude, pulseWidth, (long)hapticPlayable.DurationMs), Timestamp);
        }
        return hapticPlayable;
    }

    /// <summary>
    /// Creates touch playable from touch parameters.
    /// </summary>
    /// <param name="frequency">Touch Frequency parameter. Should be in the range [0:150]</param>
    /// <param name="amplitude">Touch Amplitude parameter. Should be in the range [0:100]</param>
    /// <param name="pulseWidth">Touch PulseWidth parameter. Should be in the range [0:320]</param>
    /// <param name="duration">Touch Duration in milliseconds</param>
    public override IHapticDynamicPlayable CreateTouch(int frequency, int amplitude, int pulseWidth, long duration)
    {
        if(CanSend())
            TouchOut.Post(new HapticParams(frequency, amplitude, pulseWidth, duration), Timestamp);
        return base.CreateTouch(frequency, amplitude, pulseWidth, duration);
    }

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
        PsiManager.Serializers.Register<HapticParams, HapticParamsSerializer>();
        PsiManager.Serializers.Register<HapticPlayable, HapticPlayableSerializer>();
        base.Start();
    }

    public void Initialize()
    {
        try
        {
            TouchOut = PsiManager.GetPipeline().CreateEmitter<HapticParams>(this, TouchTopicName);
            PlayablehOut = PsiManager.GetPipeline().CreateEmitter<HapticPlayable>(this, PlaybleTopicName);
            switch (ExportType)
            {
#if PSI_TCP_STREAMS
                case PsiPipelineManager.ExportType.TCPWriter:
                    TcpWriter<HapticParams> tcpWriterTouch = PsiManager.GetTcpWriter<HapticParams>(TouchTopicName, GetSerializerHapticParams());
                    TouchOut.PipeTo(tcpWriterTouch);
                    TcpWriter<HapticPlayable> tcpWriterPlayable = PsiManager.GetTcpWriter<HapticPlayable>(PlaybleTopicName, GetSerializerHapticPlayable());
                    PlayablehOut.PipeTo(tcpWriterPlayable);
                    break;
#endif
                default:
                    {
                        RemoteExporter exporter;
                        PsiManager.GetRemoteExporter(ExportType, out exporter);
                        exporter.Exporter.Write(TouchOut, TouchTopicName);
                        exporter.Exporter.Write(PlayablehOut, PlaybleTopicName);
                        PsiManager.RegisterExporter(ref exporter);
                    }
                    break;
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

#if PSI_TCP_STREAMS
    protected Microsoft.Psi.Interop.Serialization.IFormatSerializer<HapticParams> GetSerializerHapticParams()
    {
        return PsiFormatHapticParams.GetFormat();
    }

    protected Microsoft.Psi.Interop.Serialization.IFormatSerializer<HapticPlayable> GetSerializerHapticPlayable()
    {
        return PsiFormatHapticPlayable.GetFormat();
    }
#endif
}
