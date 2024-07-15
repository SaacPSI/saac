using UnityEngine;
using Microsoft.Psi;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Interop.Transport;
using Microsoft.Psi.Remoting;
using System;
using System.Collections.Generic;
using TMPro;
using System.Threading;
using System.Linq;
using Microsoft.Psi.Serialization;
using System.Net;

public class PsiPipelineManager : MonoBehaviour
{
    public const string ClockSynchProcessName = "ClockSynch";
    public const string CommandProcessName = "Command";
    public enum Command { Initialize, Run, Stop, Restart, Close, Status };

#if !PLATFORM_ANDROID
    public enum ExportType { LowFrequency, HighFrequency, Unknow };
    public delegate void ConnectToImporterEndPoint(RemoteImporter importer);
#else
    public delegate void ConnectToSourceEndPoint(Rendezvous.TcpSourceEndpoint source);
#endif

    private RendezvousClient RendezVousClient;
    private Rendezvous.Process Process;
    private Pipeline Pipeline;
    private Pipeline CommandSubPipeline;
    private KnownSerializers Serializers;
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
    private int WaitedRendezVousCount;

    public enum PsiManagerStartMode { Manual, Connection, Automatic };
    public PsiManagerStartMode StartMode = PsiManagerStartMode.Automatic;
    public int StreamNumberExpectedAtStart = 0;
    public string RendezVousServerAddress = "";
    public int RendezVousServerPort = 13331;
    public string RendezVousAppName = "Unity";
    public string HostAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
    public List<string> WaitedRendezVousApp;
    public delegate void PsiEvent();
    public static PsiEvent onConnected;
#if !PLATFORM_ANDROID
    public TransportKind ExportersTransportType = TransportKind.Tcp;
    public int ExportersMaxLowFrequencyStreams = 12;
#endif
    public int ExportersStartingPort = 11411;
    public GameObject TextLogObject;

    public enum PsiPipelineManagerState { Instantiated, Connecting, Connected, Served, Initialised, Running, Stopped, Failed };
    public PsiPipelineManagerState State { private set; get; } = PsiPipelineManagerState.Instantiated;

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
        ExporterCount = WaitedRendezVousCount = 0;
        Serializers = KnownSerializers.GetKnownSerializers();
        InitializeSerializer(Serializers);
    }

#if !PLATFORM_ANDROID
    public void RegisterComponentImporter(string streamName, ConnectToImporterEndPoint connectionDelegate)
    {
        ImporterDelegates.Add(streamName, connectionDelegate);
        // If subscriber is late.
        if(RemoteImporters.ContainsKey(streamName))
            connectionDelegate(RemoteImporters[streamName]);
    }
#else
    public void RegisterComponentImporter(string streamName, ConnectToSourceEndPoint connectionDelegate)
    {
        SourceDelegates.Add(streamName, connectionDelegate);
        // If subscriber is late.
        if (SourceEndpoint.ContainsKey(streamName))
            connectionDelegate(SourceEndpoint[streamName]);
    }
#endif

    public bool IsRunning()
    {
        return State == PsiPipelineManagerState.Running;
    }

    public void AddLog(string message)
    {
        LogBuffer.Add(message);
    }

    protected void InitializeSerializer(KnownSerializers serializers)
    {
        serializers.Register<bool, BoolSerializer>();
        serializers.Register<char, CharSerializer>();
        serializers.Register<DateTime,DateTimeSerializer>();
        serializers.Register<byte[], BytesSerializer>();
        serializers.Register<System.Numerics.Vector3, Vector3Serializer>();
        serializers.Register<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>, TupleOfVector3Serializer>();
        serializers.Register<System.Numerics.Matrix4x4,  Matrix4x4Serializer >();
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
            AddLog("PsiPipelineManager : Connected with rendezvous server!");
            RendezVousClient.Rendezvous.ProcessRemoved += ProcessRemoved;
            RendezVousClient.Error += RendezVousClient_Error;
            RendezVousClient.Rendezvous.ProcessAdded += ProcessAdded;
        }
        catch (Exception e)
        {
            AddLog($"PsiPipelineManager : Exception in SyncServerConnectionCoroutine: {e.Message} \n {e.InnerException} \n {e.Source} \n {e.StackTrace}");
        }
    }

    protected void ProcessRemoved(object sender, Rendezvous.Process process)
    {
        AddLog($"PsiPipelineManager : {process.Name} ProcessRemoved!");
    }

    protected void ProcessAdded(object sender, Rendezvous.Process process)
    {
        AddLog($"PsiPipelineManager : Remote App found: {process.Name}");
        if (process.Name == ClockSynchProcessName)
        {
            ProcessAddedClock(process);
        }
        else if (process.Name.Contains(CommandProcessName))
        {
            ProcessAddedCommand(process);
        }
        else if (WaitedRendezVousApp.Contains(process.Name))
        {
            ProcessAddedData(process);
        }
        else
            return;

        State = WaitedRendezVousCount >= WaitedRendezVousApp.Count ? PsiPipelineManagerState.Served : PsiPipelineManagerState.Connected;
        if (State == PsiPipelineManagerState.Served && onConnected != null)
            onConnected();
    }

    protected void ProcessAddedClock(Rendezvous.Process process)
    {
        foreach (var endpoint in process.Endpoints)
        {
            if (endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockEndpoint)
            {
                AddLog($"PsiPipelineManager : Remote clock found!");
                remoteClockEndpoint.ToRemoteClockImporter(Pipeline);
                return;
            }
        }
    }

    protected void ProcessAddedCommand(Rendezvous.Process process)
    {
        foreach (var endpoint in process.Endpoints)
        {
            if (endpoint is Rendezvous.TcpSourceEndpoint)
            {
                Rendezvous.TcpSourceEndpoint source = endpoint as Rendezvous.TcpSourceEndpoint;
                if (source == null)
                    continue;
                foreach (var stream in endpoint.Streams)
                {
                    if (stream.StreamName == CommandProcessName)
                    {
                        CommandSubPipeline = Pipeline.Create(process.Name);
                        var tcpSource = source.ToTcpSource<(Command, string)>(CommandSubPipeline, PsiFormatCommandString.GetFormat(), null, true, stream.StreamName);
                        SAAC.RendezVousPipelineServices.Helpers.PipeToMessage<(Command, string)> p2m = new SAAC.RendezVousPipelineServices.Helpers.PipeToMessage<(Command, string)>(CommandSubPipeline, CommandHandling, process.Name);
                        Microsoft.Psi.Operators.PipeTo(tcpSource.Out, p2m.In);
                        CommandSubPipeline.RunAsync();
                        AddLog($"SubPipeline {process.Name} started.");
                        return;
                    }
                }
            }
        }
    }

    protected void ProcessAddedData(Rendezvous.Process process)
    {
        foreach (var endpoint in process.Endpoints)
        {
#if PLATFORM_ANDROID
            if (endpoint is Rendezvous.TcpSourceEndpoint tcpRemoteEndpoint)
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
            if (endpoint is Rendezvous.RemoteExporterEndpoint remoteEndpoint)
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
            WaitedRendezVousCount++;
        }
    }

    protected void CommandHandling(string processName, Message<(Command, string)> message)
    {
        if (message.Data.Item2 != RendezVousAppName && message.Data.Item2.Length != 0)
            return;
        Command command = (Command)message.Data.Item1;
        AddLog($"PsiPipelineManager Recieve Command {command} from {processName} @{message.OriginatingTime} with argument {message.Data.Item2} \n");
        switch (command)
        {
            case Command.Initialize:
                AddProcess();
                break;
            case Command.Run:
                StartMode = PsiManagerStartMode.Automatic;
                break;
            case Command.Close:
            case Command.Stop:
                StopPsi();
                break;
        }
    }

    private void RendezVousClient_Error(object sender, Exception e)
    {
        AddLog($"PsiPipelineManager Exception in RendezVousClient: {e.Message} \n {e.InnerException} \n {e.Source} \n {e.StackTrace}");
    }

    private void AddProcess()
    {
        if (State > PsiPipelineManagerState.Served)
            return;
        Rendezvous.Process proc = GetProcess();
        int count = proc.Endpoints.Count();
        if (StreamNumberExpectedAtStart != 0 && count != StreamNumberExpectedAtStart)
            return;
        RendezVousClient.Rendezvous.TryAddProcess(proc);
        AddLog($"PsiPipelineManager : Add process with {count} endpoints.");
        State = PsiPipelineManagerState.Initialised;
    }

    private void StopPsi()
    {
        State = PsiPipelineManagerState.Stopped;
        if (ConnectionThread.IsAlive)
            ConnectionThread.Abort();
        if (CommandSubPipeline != null)
            CommandSubPipeline.Dispose();
        if (Pipeline != null)
            Pipeline.Dispose();
        RendezVousClient.Stop();
    }

#if !PLATFORM_ANDROID
    protected RemoteExporter CreateRemoteExporter()
    {
        return new RemoteExporter(GetPipeline(), ExportersStartingPort + ExporterCount++, ExportersTransportType);
    }

    public void GetRemoteExporter(ExportType type, out RemoteExporter exporter)
    {
        exporter = null;
        switch (type)
        {
            case ExportType.LowFrequency:
                {
                    if (EventExporter != null && EventExporter.Exporter.Metadata.Count() >= ExportersMaxLowFrequencyStreams)
                        exporter = EventExporter;
                    else
                        EventExporter = exporter = CreateRemoteExporter();
                }
                break;
            default:
                exporter = CreateRemoteExporter();
                break;
        }
    }

public void RegisterExporter(ref RemoteExporter exporter)
    {
        if (HostAddress.Length == 0)
            HostAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
        if (ExportersRegistered.Contains(exporter) == false)
        {
            ExportersRegistered.Add(exporter);
            GetProcess().AddEndpoint(exporter.ToRendezvousEndpoint(HostAddress));
        }
    }
#else

public TcpWriter<T> GetTcpWriter<T>(string topic, Microsoft.Psi.Interop.Serialization.IFormatSerializer<T> serializers)
    {
        TcpWriter<T> tcpWriter = new TcpWriter<T>(GetPipeline(), ExportersStartingPort + ExporterCount++, serializers, topic);
        RegisterTCPWriter(tcpWriter, topic);
        return tcpWriter;
    }

    public void RegisterTCPWriter<T>(TcpWriter<T> writer, string topic)
    {
        if (HostAddress.Length == 0)
            HostAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
        AddLog($"PsiPipelineManager : Add {topic} endpoint to process.");
        GetProcess().AddEndpoint(writer.ToRendezvousEndpoint(HostAddress, topic));
    }
#endif

public Pipeline GetPipeline()
{
    if (Pipeline == null)
        {
            Pipeline = Pipeline.Create(RendezVousAppName);
            if (HostAddress.Length == 0)
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

    public int GetEndpointsCount()
    {
       return GetProcess().Endpoints.Count();
    }

    public void StartPipeline()
    {
        if (State == PsiPipelineManagerState.Initialised)
        {
            Pipeline.PipelineExceptionNotHandled += (_, ex) =>
            {
                AddLog($"Pipeline Error: {ex.Exception.Message}");
                State = PsiPipelineManagerState.Failed;
                StopPsi();
            };
            Pipeline.RunAsync();
            State = PsiPipelineManagerState.Running;
            AddLog("PsiPipelineManager : Pipeline running");
        }
    }

    public void StartPsi()
    {
        if (State < PsiPipelineManagerState.Connecting)
        {
            AddLog($"PsiPipelineManager: IP used {HostAddress}");
            if (TextLogObject != null)
            {
                Text = TextLogObject.GetComponent<TMP_Text>();
                if (Text != null)
                    Text.text = "PsiPipelineManager logs:\n";
            }
            else
                Text = null;
            GetPipeline();
            RendezVousClient = new RendezvousClient(RendezVousServerAddress, RendezVousServerPort);
            ConnectionThread = new Thread(SyncServerConnection);
            ConnectionThread.Start();
            State = PsiPipelineManagerState.Connecting;
        }
    }

    // Start is called before the first frame update
    void Start()
    {}

    void Update()
    {
        switch (StartMode)
        {
            case PsiManagerStartMode.Connection:
                if (State == PsiPipelineManagerState.Instantiated)
                    StartPsi();
                break;
            case PsiManagerStartMode.Automatic:
                switch (State)
                {
                    case PsiPipelineManagerState.Instantiated:
                        StartPsi();
                        break;
                    case PsiPipelineManagerState.Served:
                        AddProcess();
                        break;
                    case PsiPipelineManagerState.Initialised:
                        StartPipeline();
                        break;
                    default: // nothing to do on others cases
                        break;
                }
                break;
            case PsiManagerStartMode.Manual:
                break; // nothing to do
        }
        if (LogBuffer.Count > 0)
        {
            string logBuffer = "";
            var LogBufferCopy = LogBuffer.DeepClone();
            LogBuffer.Clear();
            foreach (string log in LogBufferCopy)
            {
                Debug.Log(log);
                logBuffer += $"{log}\n";
            }
            if (Text != null)
                Text.text += logBuffer;
        }
    }

    void OnApplicationQuit()
    {
        StopPsi();
    }
}