using UnityEngine;
using Microsoft.Psi;
using System;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Transport;
using SAAC.PipelineServices;

public abstract class PsiExporter<T> : MonoBehaviour, IProducer<T>
{
    public string TopicName = "Topic";
    public float DataPerSecond = 0.0f;
    public bool AutoRegister = false;

    public PsiPipelineManager.ExportType ExportType = PsiPipelineManager.ExportType.Unknow;

    protected PsiPipelineManager PsiManager;
    public Emitter<T> Out { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    protected float DataTime;
    protected DateTime Timestamp = DateTime.MinValue;

#if PSI_TCP_STREAMS
    private TcpWriterMulti<T> TcpWriter;
#endif
    private RemoteExporter Exporter;

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
        PsiManager.onInitialized += Initialize;
    }

    public void Initialize()
    {
        try
        {
            Out = PsiManager.GetPipeline().CreateEmitter<T>(this, TopicName);
            switch (ExportType)
            {
#if PSI_TCP_STREAMS
                case PsiPipelineManager.ExportType.TCPWriter:
                    TcpWriter = PsiManager.GetTcpWriter<T>(TopicName, GetSerializer(), AutoRegister);
                    Out.PipeTo(TcpWriter);
                    break;
#endif
                default:
                    {
                        RemoteExporter exporter;
                        PsiManager.GetRemoteExporter(ExportType, out Exporter);
                        Exporter.Exporter.Write(Out, TopicName);
                        if (!AutoRegister)
                            PsiManager.RegisterExporter(ref exporter);
                    }
                    break;
            }
            IsInitialized = true;
            if (AutoRegister)
                PsiManager.onInitialized += OnProcessStart;
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
    protected abstract Microsoft.Psi.Interop.Serialization.IFormatSerializer<T> GetSerializer();
#endif

    private void OnProcessStart()
    {
        switch (ExportType)
        {
#if PSI_TCP_STREAMS
            case PsiPipelineManager.ExportType.TCPWriter:
                PsiManager.RegisterTCPWriter(TcpWriter, TopicName);
                break;
#endif
            default:
                PsiManager.RegisterExporter(ref exporter);
                break;
        }
    }
}