using Microsoft.Psi;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Remoting;
using System;
using UnityEngine;

public class PsiStudioReplayCommand : MonoBehaviour
{
    public class QuickConnector : IPsiImporter
    {
        private PsiStudioReplayCommand command;

        public QuickConnector(PsiStudioReplayCommand command)
        {
            this.command = command;
        }

        public override void ConnectionToImporter(RemoteImporter importer)
        { }

        public override void ConnectionToTcpSource(Rendezvous.TcpSourceEndpoint source, Pipeline parent)
        {
            this.command.ConnectToSource(source, parent);
        }
    }

    private PsiPipelineManager PsiManager;
    private SAAC.PsiStudioReplayExtension.PsiStudioNetworkConnector connector;

    public string PsiStudioOutgoingProcessName = "UnityReplayCommand";
    public string PsiStudioIncomingStreamName = "PsiStudio";
    public int Port = 18888;
    public bool IsInitialized { get; private set; } = false;

    public EventHandler<Microsoft.Psi.PsiStudio.PsiStudioNetworkInfo>? OnReceiveMessage => connector.OnReceiveMessage;


    // Start is called before the first frame update
    public void Start()
    {
        connector = new SAAC.PsiStudioReplayExtension.PsiStudioNetworkConnector();
        PsiManager = GameObject.FindAnyObjectByType<PsiPipelineManager>();
        if (PsiManager == null)
        {
            Debug.LogError("Could not found PsiPipelineManager script ! Have you put it in your scene ?");
            return;
        }
        PsiManager.onInitialized += Initialize;
    }

    public void SendPlay(TimeInterval? timeInterval = null) => connector.SendPlay(timeInterval);
    public void SendPause() => connector.SendPause();
    public void SendResume() => connector.SendResume();
    public void SendStop() => connector.SendStop();
    public void SendPlaySpeed(double speed) => connector.SendPlaySpeed(speed);

    public void Initialize()
    {
        try
        {
            PsiManager.AddProcess(connector.CreateProcessWriter(PsiManager.GetPipeline(), PsiManager.UsedAddress, Port, PsiStudioOutgoingProcessName));
            PsiManager.RegisterComponentImporter(PsiStudioIncmoingStreamName, new QuickConnector(this));
        }
        catch (Exception e)
        {
            PsiManager.AddLog($"PsiStudioReplayCommand Exception: {e.Message} \n {e.InnerException} \n {e.Source} \n {e.StackTrace}");
        }
    }

    internal void ConnectToSource(Rendezvous.TcpSourceEndpoint source, Pipeline parent)
    {
        this.connector.ConnectToSource(parent, source.Host, source.Port);
        PsiManager.AddLog($"PsiStudioReplayCommand connected to PsiStudio !");
        IsInitialized = true;
    }
}
