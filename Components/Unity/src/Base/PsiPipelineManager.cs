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

    private RendezvousClient rendezVousClient;
    private Rendezvous.Process process;
    private Pipeline pipeline;
    private Pipeline commandSubPipeline;
    private KnownSerializers serializers;
#if !PLATFORM_ANDROID
    private Dictionary<string, ConnectToImporterEndPoint> importerDelegates;
    private Dictionary<string, RemoteImporter> remoteImporters;
    private RemoteExporter eventExporter;
    private List<RemoteExporter> exportersRegistered;
#else
    private Dictionary<string, ConnectToSourceEndPoint> sourceDelegates;
    private Dictionary<string, Rendezvous.TcpSourceEndpoint> sourceEndpoint;
#endif
    private List<string> logBuffer;
    private TMP_Text text;
    private Thread connectionThread;
    private int exporterCount;
    private int waitedRendezVousCount;
    private bool initializedEventTriggered;

    public enum PsiManagerStartMode { Manual, Connection, Automatic };
    public PsiManagerStartMode StartMode = PsiManagerStartMode.Automatic;
    public int StreamNumberExpectedAtStart = 0;
    public string RendezVousServerAddress = "";
    public int RendezVousServerPort = 13331;
    public string RendezVousAppName = "Unity";
    public string HostAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
    public List<string> WaitedRendezVousApp;
    public delegate void PsiEvent();
    public PsiEvent onConnected;
    public PsiEvent onInitialized;
#if !PLATFORM_ANDROID
    public TransportKind ExportersTransportType = TransportKind.Tcp;
    public int ExportersMaxLowFrequencyStreams = 12;
#endif
    public int ExportersStartingPort = 11411;
    public GameObject TextLogObject;
    public Emitter<(Command, string)> CommandEmitter { get; private set; } = null;
    public int CommandEmitterPort = 11511;

    public enum PsiPipelineManagerState { Instantiated, Connecting, Connected, Served, Initializing, Initialized, Running, Stopped, Failed };
    public PsiPipelineManagerState State { private set; get; } = PsiPipelineManagerState.Instantiated;

    PsiPipelineManager()
    {
#if !PLATFORM_ANDROID
        importerDelegates = new Dictionary<string, ConnectToImporterEndPoint>();
        remoteImporters = new Dictionary<string, RemoteImporter>();
        exportersRegistered = new List<RemoteExporter>();
        eventExporter = null;
#else
        sourceDelegates = new Dictionary<string, ConnectToSourceEndPoint>();
        sourceEndpoint = new Dictionary<string, Rendezvous.TcpSourceEndpoint>();
#endif
        logBuffer = new List<string>();
        exporterCount = waitedRendezVousCount = 0;
        initializedEventTriggered = false;
        serializers = KnownSerializers.GetKnownSerializers();
        InitializeSerializer(serializers);
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
        sourceDelegates.Add(streamName, connectionDelegate);
        // If subscriber is late.
        if (sourceEndpoint.ContainsKey(streamName))
            connectionDelegate(sourceEndpoint[streamName]);
    }
#endif

    public bool IsRunning()
    {
        return State == PsiPipelineManagerState.Running;
    }

    public void AddLog(string message)
    {
        logBuffer.Add(message);
    }

    protected void InitializeSerializer(KnownSerializers serializers)
    {
        serializers.Register<bool, BoolSerializer>();
        serializers.Register<char, CharSerializer>();
        serializers.Register<DateTime, DateTimeSerializer>();
        serializers.Register<byte[], BytesSerializer>();
        serializers.Register<System.Numerics.Vector3, Vector3Serializer>();
        serializers.Register<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>, TupleOfVector3Serializer>();
        serializers.Register<System.Numerics.Matrix4x4, Matrix4x4Serializer>();
    }

    void SyncServerConnection()
    {
        try
        {
            AddLog("PsiPipelineManager : Waiting for server");
            rendezVousClient.Start();
            if (!rendezVousClient.Connected.WaitOne())
            {
                AddLog("PsiPipelineManager : Failed to connect to the server !");
                return;
            }
            AddLog("PsiPipelineManager : Connected with rendezvous server!");
            rendezVousClient.Rendezvous.ProcessRemoved += ProcessRemoved;
            rendezVousClient.Error += RendezVousClient_Error;
            rendezVousClient.Rendezvous.ProcessAdded += ProcessAdded;
            State = PsiPipelineManagerState.Connected;
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

        State = waitedRendezVousCount >= WaitedRendezVousApp.Count ? PsiPipelineManagerState.Served : PsiPipelineManagerState.Connected;
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
                remoteClockEndpoint.ToRemoteClockImporter(pipeline);
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
                        commandSubPipeline = Pipeline.Create(process.Name);
                        var tcpSource = source.ToTcpSource<(Command, string)>(commandSubPipeline, PsiFormatCommandString.GetFormat(), null, true, stream.StreamName);
                        SAAC.RendezVousPipelineServices.Helpers.PipeToMessage<(Command, string)> p2m = new SAAC.RendezVousPipelineServices.Helpers.PipeToMessage<(Command, string)>(commandSubPipeline, CommandHandling, process.Name);
                        Microsoft.Psi.Operators.PipeTo(tcpSource.Out, p2m.In);
                        if (CommandEmitterPort != 0)
                        {
                            TcpWriter<(Command, string)> writer = new TcpWriter<(Command, string)>(commandSubPipeline, CommandEmitterPort, PsiFormatCommandString.GetFormat(), CommandProcessName);
                            CommandEmitter = commandSubPipeline.CreateEmitter<(Command, string)>(this, $"{RendezVousAppName}-{CommandProcessName}");
                            CommandEmitter.PipeTo(writer.In);
                            if (HostAddress.Length == 0)
                                HostAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
                            var commandProcess = new Rendezvous.Process($"{RendezVousAppName}-{CommandProcessName}");
                            commandProcess.AddEndpoint(writer.ToRendezvousEndpoint(HostAddress, CommandProcessName));
                            rendezVousClient.Rendezvous.TryAddProcess(commandProcess);
                        }
                        commandSubPipeline.RunAsync();
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
                    sourceEndpoint.Add(stream.StreamName, tcpRemoteEndpoint);
                    if (sourceDelegates.ContainsKey(stream.StreamName))
                    {
                        sourceDelegates[stream.StreamName](tcpRemoteEndpoint);
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
            waitedRendezVousCount++;
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
                InitializeExporters();
                AddProcess();
                break;
            case Command.Run:
                StartMode = PsiManagerStartMode.Automatic;
                break;
            case Command.Close:
            case Command.Stop:
                StopPsi();
                break;
            case Command.Status:
                if(CommandEmitter != null)
                    CommandEmitter.Post((Command.Status, State.ToString()), commandSubPipeline.GetCurrentTime());
                break;
        }
    }

    private void RendezVousClient_Error(object sender, Exception e)
    {
        AddLog($"PsiPipelineManager Exception in rendezVousClient: {e.Message} \n {e.InnerException} \n {e.Source} \n {e.StackTrace}");
    }

    public void InitializeExporters()
    {
        if (State > PsiPipelineManagerState.Served)
            return;
        if (!initializedEventTriggered && onInitialized != null)
        {
            onInitialized();
            initializedEventTriggered = true;
            State = PsiPipelineManagerState.Initializing;
        }
    }

    public void AddProcess()
    {
        if (State > PsiPipelineManagerState.Initializing)
            return;

        Rendezvous.Process proc = GetProcess();
        int count = proc.Endpoints.Count();
        if (StreamNumberExpectedAtStart != 0 && count < StreamNumberExpectedAtStart)
            return;
        rendezVousClient.Rendezvous.TryAddProcess(proc);
        AddLog($"PsiPipelineManager : Add process with {count} endpoints.");
        State = PsiPipelineManagerState.Initialized;
    }

    private void StopPsi()
    {
        State = PsiPipelineManagerState.Stopped;
        if (connectionThread.IsAlive)
            connectionThread.Abort();
        if (commandSubPipeline != null)
            commandSubPipeline.Dispose();
        if (pipeline != null)
            pipeline.Dispose();
        rendezVousClient.Stop();
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
            // Deactivation of the optimisation due to the ref exporter issue.
            //case ExportType.LowFrequency:
            //    {
            //        if (eventExporter != null && eventExporter.Exporter.Metadata.Count() >= ExportersMaxLowFrequencyStreams)
            //            exporter = eventExporter;
            //        else
            //            eventExporter = exporter = CreateRemoteExporter();
            //    }
            //    break;
            default:
                exporter = CreateRemoteExporter();
                break;
        }
    }

    public void RegisterExporter(ref RemoteExporter exporter)
    {
        if (HostAddress.Length == 0)
            HostAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
        if (exportersRegistered.Contains(exporter) == false)
        {
            ExportersRegistered.Add(exporter);
            GetProcess().AddEndpoint(exporter.ToRendezvousEndpoint(HostAddress));
        }
    }
#else

    public TcpWriter<T> GetTcpWriter<T>(string topic, Microsoft.Psi.Interop.Serialization.IFormatSerializer<T> serializers)
    {
        TcpWriter<T> tcpWriter = new TcpWriter<T>(GetPipeline(), ExportersStartingPort + exporterCount++, serializers, topic);
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
        if (pipeline == null)
        {
            pipeline = Pipeline.Create(RendezVousAppName);
            if (HostAddress.Length == 0)
                HostAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
        }
        return pipeline;
    }

    public Rendezvous.Process GetProcess()
    {
        if (process == null)
            process = new Rendezvous.Process(RendezVousAppName, "1.0");
        return process;
    }

    public int GetEndpointsCount()
    {
        return GetProcess().Endpoints.Count();
    }

    public void StartPipeline()
    {
        if (State == PsiPipelineManagerState.Initialized)
        {
            pipeline.PipelineExceptionNotHandled += (_, ex) =>
            {
                AddLog($"pipeline Error: {ex.Exception.Message}");
                State = PsiPipelineManagerState.Failed;
                StopPsi();
            };
            pipeline.RunAsync();
            State = PsiPipelineManagerState.Running;
            AddLog("PsiPipelineManager : pipeline running");
        }
    }

    public void StartPsi()
    {
        if (State < PsiPipelineManagerState.Connecting)
        {
            AddLog($"PsiPipelineManager: IP used {HostAddress}");
            if (TextLogObject != null)
            {
                text = TextLogObject.GetComponent<TMP_Text>();
                if (text != null)
                    text.text = "PsiPipelineManager logs:\n";
            }
            else
                text = null;
            GetPipeline();
            rendezVousClient = new RendezvousClient(RendezVousServerAddress, RendezVousServerPort);
            connectionThread = new Thread(SyncServerConnection);
            connectionThread.Start();
            State = PsiPipelineManagerState.Connecting;
        }
    }

    // Start is called before the first frame update
    void Start()
    { }

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
                        InitializeExporters();
                        break;
                    case PsiPipelineManagerState.Initializing:
                        AddProcess();
                        break;
                    case PsiPipelineManagerState.Initialized:
                        StartPipeline();
                        break;
                    default: // nothing to do on others cases
                        break;
                }
                break;
            case PsiManagerStartMode.Manual:
                break; // nothing to do
        }
        if (logBuffer.Count > 0)
        {
            string logBuffer = "";
            var LogBufferCopy = this.logBuffer.DeepClone();
            this.logBuffer.Clear();
            foreach (string log in LogBufferCopy)
            {
                Debug.Log(log);
                logBuffer += $"{log}\n";
            }
            if (text != null)
                text.text += logBuffer;
        }
    }

    void OnApplicationQuit()
    {
        StopPsi();
    }
}