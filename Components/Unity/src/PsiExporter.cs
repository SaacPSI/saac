using UnityEngine;
using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Transport;
using System;
using System.Reflection;
using System.Reflection.Emit;

public abstract class PsiExporter<T> : MonoBehaviour, IProducer<T>
{
    public string TopicName = "Topic";
    public TransportKind TransportType = TransportKind.Tcp;
    public int Port = 11411;

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
            //Hololens Version
            //TcpWriter<T> tcpWriter = new TcpWriter<T>(PsiManager.GetPipeline(), Port, GetSerializer(), TopicName);
            //Out.PipeTo(tcpWriter);
            //PsiManager.RegisterTCPWriter(tcpWriter, TopicName);
            RemoteExporter exporter = new RemoteExporter(PsiManager.GetPipeline(), Port, TransportType);
            Out = PsiManager.GetPipeline().CreateEmitter<T>(this, TopicName);
            exporter.Exporter.Write(Out, TopicName);
            PsiManager.RegisterExporter(exporter);
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

    protected abstract Microsoft.Psi.Interop.Serialization.IFormatSerializer GetSerializer();
}