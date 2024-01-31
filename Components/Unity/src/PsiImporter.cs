using UnityEngine;
using Microsoft.Psi;
using Microsoft.Psi.Remoting;

public abstract class PsiImporter<T> : MonoBehaviour
{
    public string TopicName = "Topic";
    protected bool IsInitialized = false;

    protected PsiPipelineManager PsiManager;

    // Start is called before the first frame update
    void Start()
    {
        PsiManager = GameObject.FindAnyObjectByType<PsiPipelineManager>();
        if (PsiManager == null)
        {
            Debug.LogError("Could not found PsiPipelineManager script ! Have you put it in your scene ?");
            return;
        }
        PsiPipelineManager.ConnectToImporterEndPoint ConnectionDelegate = ConnectionToImporter;
        PsiManager.RegisterComponentImporter(TopicName, ConnectionToImporter);
    }

    void ConnectionToImporter(RemoteImporter importer)
    {
        var stream = importer.Importer.OpenStream<T>(TopicName);
        stream.Do(Process);
        IsInitialized = true;
    }

    protected abstract void Process(T message, Envelope enveloppe);
}
