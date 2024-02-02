using UnityEngine;
using Microsoft.Psi;
using System;

public abstract class PsiExporter<T> : MonoBehaviour, IProducer<T>
{
    public string TopicName = "Topic";
    public PsiPipelineManager.ExportType ExportType = PsiPipelineManager.ExportType.Unknow;
        
    private PsiPipelineManager PsiManager;
    protected Emitter<T> Out;
    Emitter<T> IProducer<T>.Out => ((IProducer<T>)Out).Out;

    protected bool IsInitialized = false;

    // Start is called before the first frame update
    protected void Start()
    {
        PsiManager = GameObject.FindAnyObjectByType<PsiPipelineManager>();
        if (PsiManager == null)
        {
            Debug.LogError("Could not found PsiPipelineManager script ! Have you put it in your scene ?");
            return;
        }
        try
        {
#if HOLOLENS
            //Hololens Version
            TcpWriter<T> tcpWriter = new TcpWriter<T>(PsiManager.GetPipeline(), Port, GetSerializer(), TopicName);
            Out.PipeTo(tcpWriter);
            PsiManager.RegisterTCPWriter(tcpWriter, TopicName);
#endif

            Out = PsiManager.GetPipeline().CreateEmitter<T>(this, TopicName);
            PsiManager.GetRemoteExporter(ExportType).Exporter.Write(Out, TopicName);
            IsInitialized = true;
        }
        catch (Exception e)
        {
            PsiManager.AddLog($"PsiExporter Exception: {e.Message} \n {e.InnerException} \n {e.Source} \n {e.StackTrace}");
        }
    }

    protected bool CanSend()
    {
        return IsInitialized && PsiManager.IsRunning;
    }

    protected DateTime GetCurrentTime()
    {
        return PsiManager.GetPipeline().GetCurrentTime();
    }

    // Update is called once per frame
    void Update()
    {
        if (CanSend())
        {
           // Do stuff in your script
        }
    }

#if HOLOLENS
    protected abstract Microsoft.Psi.Interop.Serialization.IFormatSerializer GetSerializer();
#endif
}