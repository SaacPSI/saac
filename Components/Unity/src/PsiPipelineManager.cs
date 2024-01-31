using UnityEngine;
using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Interop.Transport;
using System;
using System.Collections.Generic;
using TMPro;
using System.Threading;

public class PsiPipelineManager : MonoBehaviour
{
    public delegate void ConnectToImporterEndPoint(RemoteImporter importer);

    private RendezvousClient RendezVousClient;
    private Rendezvous.Process Process;
    private Pipeline Pipeline;
    private string HostAddress;
    private Dictionary<string, ConnectToImporterEndPoint> ImporterDelegates;
    private Dictionary<string, RemoteImporter> RemoteImporters;
    private List<string> LogBuffer;
    private TMP_Text Text;
    private Thread ConnectionThread;

    public string RendezVousServerAddress = "";
    public int RendezVousServerPort = 13331;
    public string RendezVousAppName = "Unity";
    public GameObject TextObject;


    public bool IsRunning { private set; get; } = false;
    public bool IsInitialized { private set; get; } = false;

    PsiPipelineManager()
    {
        ImporterDelegates = new Dictionary<string, ConnectToImporterEndPoint>();
        RemoteImporters = new Dictionary<string, RemoteImporter>();
        LogBuffer = new List<string>();
    }

    public void RegisterComponentImporter(string streamName, ConnectToImporterEndPoint connectionDelegate)
    {
        ImporterDelegates.Add(streamName, connectionDelegate);
    }

    public void AddLog(string message)
    {
        LogBuffer.Add(message);
    }

    void SyncServerConnection()
    {
        try
        {
            RendezVousClient.Start();
            AddLog("PsiPipelineManager : Waiting for server");
            if (!RendezVousClient.Connected.WaitOne())
            {
                AddLog("PsiPipelineManager : Failed to connect to the server !");
                return;
            }
            AddLog("PsiPipelineManager : Connected!");
            RendezVousClient.Rendezvous.ProcessAdded += (_, p) =>
            {
                if (p.Name.Equals(RendezVousAppName))
                    return;

                AddLog($"PsiPipelineManager : Remote App found: {p.Name}");
                foreach (var endpoint in p.Endpoints)
                {
                    if (endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockEndpoint)
                    {
                        AddLog($"PsiPipelineManager : Remote clock found!");
                        var remoteClockImporter = remoteClockEndpoint.ToRemoteClockImporter(Pipeline);
                    }
                    else if (endpoint is Rendezvous.RemoteExporterEndpoint remoteEndpoint)
                    {
                        RemoteImporter remoteImporter = remoteEndpoint.ToRemoteImporter(Pipeline);
                        foreach (Rendezvous.Stream stream in remoteEndpoint.Streams)
                        {
                            AddLog($"PsiPipelineManager : Remote stream {stream.StreamName} found!");
                            RemoteImporters.Add(stream.StreamName, remoteImporter);
                            if (ImporterDelegates.ContainsKey(stream.StreamName))
                            {
                                ImporterDelegates[stream.StreamName](remoteImporter);
                            }
                        }
                    }
                }
            };
            RendezVousClient.Rendezvous.ProcessRemoved += (_, p) =>
            {
                IsRunning = IsInitialized = false;
                Pipeline.Dispose();
            };
            Pipeline.PipelineExceptionNotHandled += (_, ex) =>
            {
                AddLog($"Pipeline Error: {ex.Exception.Message}");
            };

            RendezVousClient.Error += RendezVousClient_Error;
            HostAddress = RendezVousClient.ClientAddress;
            Process = new Rendezvous.Process(RendezVousAppName, "1.0");
            AddLog("PsiPipelineManager : Process created!");
            IsInitialized = true;
        }
        catch (Exception e)
        {
            AddLog($"PsiPipelineManager : Exception in SyncServerConnectionCoroutine: {e.Message} \n {e.InnerException} \n {e.Source} \n {e.StackTrace}");
        }
    }

    private void RendezVousClient_Error(object sender, Exception e)
    {
        AddLog("ERROR: " + e.Message);
    }

    public void RegisterExporter(RemoteExporter exporter)
    {
        Process.AddEndpoint(exporter.ToRendezvousEndpoint(HostAddress));
    }

    public void RegisterTCPWriter<T>(TcpWriter<T> writer, string topic)
    {
        Process.AddEndpoint(writer.ToRendezvousEndpoint<T>(HostAddress, topic));
    }

    public Pipeline GetPipeline()
    {
        if (Pipeline == null)
            Pipeline = Pipeline.Create("UnityPipeline");
        return Pipeline;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!IsInitialized)
        {
            if (TextObject != null)
            {
                Text = TextObject.GetComponent<TMP_Text>();
                if (Text != null)
                    Text.text = "Hello! Log:\n";
            }
            else
                Text = null;
            GetPipeline();
            RendezVousClient = new RendezvousClient(RendezVousServerAddress, RendezVousServerPort);
            ConnectionThread = new Thread(SyncServerConnection);
            ConnectionThread.Start();
        }
    }
    
    void Update()
    {
        if(IsInitialized && !IsRunning)
        {
            RendezVousClient.Rendezvous.TryAddProcess(Process);
            IsRunning = true;
            Pipeline.RunAsync();
            AddLog("PsiPipelineManager : Pipeline running");
        }
        if (LogBuffer.Count > 0)
        {
            string logBuffer = "";
            foreach (string log in LogBuffer)
            {
                Debug.Log(log);
                logBuffer += log + "\n";
            }
            if (Text != null)
                Text.text += logBuffer;
            LogBuffer.Clear();
        }
    }

    void OnApplicationQuit()
    {
        if(Pipeline != null)
            Pipeline.Dispose();
    }
}