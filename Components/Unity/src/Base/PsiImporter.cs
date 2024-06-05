using UnityEngine;
using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
using System;
using Microsoft.Psi.Data;

public abstract class PsiImporter<T> : MonoBehaviour
{
    public string TopicName = "Topic";
    protected bool IsInitialized = false;

    protected PsiPipelineManager PsiManager;

    // Start is called before the first frame update
    public virtual void Start()
    {
        PsiManager = GameObject.FindAnyObjectByType<PsiPipelineManager>();
        if (PsiManager == null)
        {
            Debug.LogError("Could not found PsiPipelineManager script ! Have you put it in your scene ?");
            return;
        }
#if PLATFORM_ANDROID
        PsiPipelineManager.ConnectToSourceEndPoint ConnectionDelegate = ConnectionToTcpSource;
        PsiManager.RegisterComponentImporter(TopicName, ConnectionToTcpSource);
#else
        PsiPipelineManager.ConnectToImporterEndPoint ConnectionDelegate = ConnectionToImporter;
        PsiManager.RegisterComponentImporter(TopicName, ConnectionToImporter);
#endif
    }

    protected abstract void Process(T message, Envelope enveloppe);

#if !PLATFORM_ANDROID
    void ConnectionToImporter(RemoteImporter importer)
    {
        PsiManager.AddLog($"Connecting to stream {TopicName}");
        if (importer.Connected.WaitOne(1000) == false)
        {
            PsiManager.AddLog($"Failed to connect stream {TopicName}");
            return;
        }
        var stream = importer.Importer.OpenStream<T>(TopicName);
        PsiManager.AddLog($"Stream {TopicName} connected.");
        stream.Do(Process);
        IsInitialized = true;
    }

#else

    void ConnectionToTcpSource(Rendezvous.TcpSourceEndpoint source)
    {
        PsiManager.AddLog($"Connecting to stream {TopicName}");
        var stream = source.ToTcpSource<T>(PsiManager.GetPipeline(), GetDeserializer(), null, true, TopicName);
        PsiManager.AddLog($"Stream {TopicName} connected.");
        stream.Do(Process);
        IsInitialized = true;
    }

    protected abstract Microsoft.Psi.Interop.Serialization.IFormatDeserializer<T> GetDeserializer();
#endif
}
