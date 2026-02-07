using UnityEngine;
using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
using UnityEngine.Rendering;
using System;

public abstract class IPsiImporter : MonoBehaviour
{
    public abstract void ConnectionToImporter(RemoteImporter importer);
#if PSI_TCP_STREAMS
    public abstract void ConnectionToTcpSource(Rendezvous.TcpSourceEndpoint source, Pipeline parent);
#endif
}

public abstract class PsiImporter<T> : IPsiImporter, IDisposable
{
    [Tooltip("Name of the topic from which data will be imported")]
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
        PsiManager.RegisterComponentImporter(TopicName, this);
    }

    public void Dispose()
    {
        IsInitialized = false;
    }

    protected abstract void Process(T message, Envelope enveloppe);

    public override void ConnectionToImporter(RemoteImporter importer)
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

#if PSI_TCP_STREAMS
    public override void ConnectionToTcpSource(Rendezvous.TcpSourceEndpoint source, Pipeline parent)
    {
        PsiManager.AddLog($"Connecting to stream {TopicName}");
        var stream = source.ToTcpSource<T>(parent, GetDeserializer(), null, false, TopicName);
        PsiManager.AddLog($"Stream {TopicName} connected.");
        stream.Do(Process);
        IsInitialized = true;
    }

    protected abstract Microsoft.Psi.Interop.Serialization.IFormatDeserializer<T> GetDeserializer();
#endif
}
