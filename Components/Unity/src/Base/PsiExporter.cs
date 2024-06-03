using UnityEngine;
using Microsoft.Psi;
using System;
using System.Net.Sockets;
using Microsoft.Psi.Interop.Transport;
using Microsoft.Psi.Components;

public abstract class PsiExporter<T> : MonoBehaviour, IProducer<T>
{
    public string TopicName = "Topic";
    public float DataPerSecond = 0.0f;

#if !PLATFORM_ANDROID
    public PsiPipelineManager.ExportType ExportType = PsiPipelineManager.ExportType.Unknow;
#endif
    
    protected PsiPipelineManager PsiManager;
    protected Emitter<T> Out;
    Emitter<T> IProducer<T>.Out => ((IProducer<T>)Out).Out;

    protected bool IsInitialized = false;
    protected float DataTime;
    protected DateTime Timestamp = DateTime.UtcNow;

    // Start is called before the first frame update
    public virtual void Start()
    {
        DataTime = DataPerSecond == 0.0f ? 0.0f : 1.0f / DataPerSecond;
        PsiManager = GameObject.FindAnyObjectByType<PsiPipelineManager>();
        if (PsiManager == null)
        {
            Debug.LogError("Could not found PsiPipelineManager script ! Have you put it in your scene ?");
            return;
        }
        try
        {
            Out = PsiManager.GetPipeline().CreateEmitter<T>(this, TopicName);
#if PLATFORM_ANDROID
            TcpWriter<T> tcpWriter = PsiManager.GetTcpWriter<T>(TopicName, GetSerializer());
            Out.PipeTo(tcpWriter);
#else
            PsiManager.GetRemoteExporter(ExportType).Exporter.Write(Out, TopicName);
#endif
            IsInitialized = true;
        }
        catch (Exception e)
        {
            PsiManager.AddLog($"PsiExporter Exception: {e.Message} \n {e.InnerException} \n {e.Source} \n {e.StackTrace}");
        }
    }

    protected bool CanSend()
    {
        if (IsInitialized && PsiManager.IsRunning && (DataTime == 0.0f || GetCurrentTime().Subtract(Timestamp).TotalSeconds > DataTime))
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

#if PLATFORM_ANDROID
    protected abstract Microsoft.Psi.Interop.Serialization.IFormatSerializer<T> GetSerializer();
#endif
}