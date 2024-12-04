using UnityEngine;
using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;

public abstract class IPsiImporter : MonoBehaviour
{
    public abstract void ConnectionToImporter(RemoteImporter importer);
#if PSI_TCP_STREAMS
    public abstract void ConnectionToTcpSource(Rendezvous.TcpSourceEndpoint source);
#endif
}

public abstract class PsiImporter<T> : IPsiImporter
{
    public string TopicName = "Topic";
    protected bool IsInitialized = false;
    public delegate void RecieveMessage(T message, DateTime time);
    public RecieveMessage onRecieved;


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

    protected virtual void Process(T message, Envelope enveloppe)
    {
        onRecieved(message, enveloppe.OriginatingTime);
    }

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
    public override void ConnectionToTcpSource(Rendezvous.TcpSourceEndpoint source)
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
