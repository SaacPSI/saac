using UnityEngine;
using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Interop.Transport;
using System;
using System.Collections.Generic;
using TMPro;
using System.Threading;
using System.Linq;
using Microsoft.Psi.Serialization;
using System.Net;

public class PsiPipelineManager : MonoBehaviour
{
#if !PLATFORM_ANDROID
    public enum ExportType { LowFrequency, HighFrequency, Unknow };
    public delegate void ConnectToImporterEndPoint(RemoteImporter importer);
#else
    public delegate void ConnectToSourceEndPoint(Rendezvous.TcpSourceEndpoint source);
#endif

    private RendezvousClient RendezVousClient;
    private Rendezvous.Process Process;
    private Pipeline Pipeline;
    private KnownSerializers Serializers;
    private string HostAddress;
#if !PLATFORM_ANDROID
    private Dictionary<string, ConnectToImporterEndPoint> ImporterDelegates;
    private Dictionary<string, RemoteImporter> RemoteImporters;
    private RemoteExporter EventExporter;
    private List<RemoteExporter> ExportersRegistered;
#else
    private Dictionary<string, ConnectToSourceEndPoint> SourceDelegates;
    private Dictionary<string, Rendezvous.TcpSourceEndpoint> SourceEndpoint;
#endif
    private List<string> LogBuffer;
    private TMP_Text Text;
    private Thread ConnectionThread;
    private int ExporterCount;

    public string RendezVousServerAddress = "";
    public int RendezVousServerPort = 13331;
    public string RendezVousAppName = "Unity";
#if !PLATFORM_ANDROID
    public TransportKind ExportersTransportType = TransportKind.Tcp;
    public int ExportersMaxLowFrequencyStreams = 12;
#endif
    public int ExportersStartingPort = 11411;
    public GameObject TextLogObject;

    public bool IsRunning { private set; get; } = false;
    public bool IsInitialized { private set; get; } = false;

    PsiPipelineManager()
    {
#if !PLATFORM_ANDROID
        ImporterDelegates = new Dictionary<string, ConnectToImporterEndPoint>();
        RemoteImporters = new Dictionary<string, RemoteImporter>();
        ExportersRegistered = new List<RemoteExporter>();
        EventExporter = null;
#else
        SourceDelegates = new Dictionary<string, ConnectToSourceEndPoint>();
        SourceEndpoint = new Dictionary<string, Rendezvous.TcpSourceEndpoint>();
#endif
        LogBuffer = new List<string>();
        ExporterCount = 0;
        Serializers = KnownSerializers.GetKnownSerializers();
        InitializeSerializer(Serializers);
    }

#if !PLATFORM_ANDROID
    public void RegisterComponentImporter(string streamName, ConnectToImporterEndPoint connectionDelegate)
    {
        ImporterDelegates.Add(streamName, connectionDelegate);
        // If subscriber is late.
        if(RemoteImporters.ContainsKey(streamName))
        {
            connectionDelegate(RemoteImporters[streamName]);
        }
    }
#else
    public void RegisterComponentImporter(string streamName, ConnectToSourceEndPoint connectionDelegate)
    {
        SourceDelegates.Add(streamName, connectionDelegate);
        // If subscriber is late.
        if (SourceEndpoint.ContainsKey(streamName))
        {
            connectionDelegate(SourceEndpoint[streamName]);
        }
    }
#endif

    public void AddLog(string message)
    {
        LogBuffer.Add(message);
    }

    protected void InitializeSerializer(KnownSerializers serializers)
    {
        serializers.Register<bool, BoolSerializer>();
        serializers.Register<char, CharSerializer>();
        serializers.Register<System.Numerics.Vector3, Vector3Serializer>();
        serializers.Register<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>, TupleOfVector3Serializer>();
    }

    void SyncServerConnection()
    {
        try
        {
            AddLog("PsiPipelineManager : Waiting for server");
            RendezVousClient.Start();
            if (!RendezVousClient.Connected.WaitOne())
            {
                AddLog("PsiPipelineManager : Failed to connect to the server !");
                return;
            }
            AddLog("PsiPipelineManager : Connected!");
            RendezVousClient.Rendezvous.ProcessRemoved += (_, p) =>
            {
                AddLog("PsiPipelineManager : ProcessRemoved!");
                IsRunning = IsInitialized = false;
                Pipeline.Dispose();
            };
            RendezVousClient.Error += RendezVousClient_Error;
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
#if PLATFORM_ANDROID
                    else if (endpoint is Rendezvous.TcpSourceEndpoint tcpRemoteEndpoint)
                    {
                        foreach (Rendezvous.Stream stream in tcpRemoteEndpoint.Streams)
                        {
                            AddLog($"PsiPipelineManager : Remote stream {stream.StreamName} found!");
                            SourceEndpoint.Add(stream.StreamName, tcpRemoteEndpoint);
                            if (SourceDelegates.ContainsKey(stream.StreamName))
                            {
                                SourceDelegates[stream.StreamName](tcpRemoteEndpoint);
                            }
                        }
                    }
#else
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
#endif
                }
            };
            IsInitialized = true;
        }
        catch (Exception e)
        {
            AddLog($"PsiPipelineManager : Exception in SyncServerConnectionCoroutine: {e.Message} \n {e.InnerException} \n {e.Source} \n {e.StackTrace}");
        }
    }

    private void RendezVousClient_Error(object sender, Exception e)
    {
        AddLog($"ERROR: {e.Message}");
    }

#if !PLATFORM_ANDROID
    protected RemoteExporter CreateRemoteExporter()
    {
        return new RemoteExporter(GetPipeline(), ExportersStartingPort + ExporterCount++, ExportersTransportType);
    }

    public RemoteExporter GetRemoteExporter(ExportType type)
    {
        RemoteExporter exporter = null;
        switch (type)
        {
            case ExportType.LowFrequency:
                {
                    if (EventExporter != null && EventExporter.Exporter.Metadata.Count() >= ExportersMaxLowFrequencyStreams)
                        return EventExporter;
                    EventExporter = exporter = CreateRemoteExporter();
                }
                break;
            default:
                exporter = CreateRemoteExporter();
                break;
        }
        RegisterExporter(ref exporter);
        return exporter;
    }
        
    public void RegisterExporter(ref RemoteExporter exporter)
    {
        if(ExportersRegistered.Contains(exporter) == false)
        {
            ExportersRegistered.Add(exporter);
            GetProcess().AddEndpoint(exporter.ToRendezvousEndpoint(HostAddress));
        }
    }
#endif

    public TcpWriter<T> GetTcpWriter<T>(string topic, Microsoft.Psi.Interop.Serialization.IFormatSerializer<T> serializers)
    {
        TcpWriter<T> tcpWriter = new TcpWriter<T>(GetPipeline(), ExportersStartingPort + ExporterCount++, serializers, topic);
        RegisterTCPWriter(tcpWriter, topic);
        return tcpWriter;
    }

    public void RegisterTCPWriter<T>(TcpWriter<T> writer, string topic)
    {
        GetProcess().AddEndpoint(writer.ToRendezvousEndpoint(HostAddress, topic));
    }

    public Pipeline GetPipeline()
    {
        if (Pipeline == null)
        {
            Pipeline = Pipeline.Create("UnityPipeline");
            HostAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
        }
        return Pipeline;
    }

    public Rendezvous.Process GetProcess()
    {
        if (Process == null)
            Process = new Rendezvous.Process(RendezVousAppName, "1.0");
        return Process;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!IsInitialized)
        {
            AddLog($"PsiPipelineManager: IP used {HostAddress}");
            if (TextLogObject != null)
            {
                Text = TextLogObject.GetComponent<TMP_Text>();
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
            RendezVousClient.Rendezvous.TryAddProcess(GetProcess());
            IsRunning = true;
            Pipeline.PipelineExceptionNotHandled += (_, ex) =>
            {
                AddLog($"Pipeline Error: {ex.Exception.Message}");
                IsRunning = false;
                Pipeline.Dispose();
            };
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
        ConnectionThread.Abort();
    }
}