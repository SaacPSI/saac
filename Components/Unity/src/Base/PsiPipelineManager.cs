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
using SAAC.PsiFormats;
using UnityEngine.Rendering;

public class PsiPipelineManager : MonoBehaviour
{
    public const string ClockSynchProcessName = "ClockSynch";
    public const string CommandProcessName = "Command";
    public enum Command { Initialize, Run, Stop, Reset, Close, Status };

    private RendezvousClient rendezVousClient;
    private Rendezvous.Process process;
    private Pipeline pipeline;
    private Pipeline commandPipeline;
    public KnownSerializers Serializers { get; private set; }

    public enum ExportType
    {
#if PSI_TCP_STREAMS
        TCPWriter,
#endif
        LowFrequency, HighFrequency, Unknow
    };
    private RemoteExporter eventExporter;
    private List<RemoteExporter> exportersRegistered;

    private Dictionary<string, IPsiImporter> importerComponents;
    private Dictionary<string, Rendezvous.TcpSourceEndpoint> sourceEndpoint;
    private Dictionary<string, RemoteImporter> remoteImporters;
    private List<string> exportedTopics;
    private List<Subpipeline> subPipelines;

    private List<string> logBuffer;
    private TMP_Text text;
    private Thread connectionThread;
    private int exporterCount;
    private int waitedRendezVousCount;
    private bool initializedEventTriggered;

    public enum PsiManagerStartMode { Manual, Connection, Automatic };
    public PsiManagerStartMode StartMode = PsiManagerStartMode.Automatic;
    public int ExporterNumberExpectedAtStart = 0;
    public string RendezVousServerAddress = "";
    public int RendezVousServerPort = 13331;
    public string UsedProcessName = "Unity";
    public string UsedAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
    public List<string> WaitedProcess;
    public List<string> AcceptedProcess;
    public delegate void PsiEvent();
    public PsiEvent onConnected;
    public PsiEvent onInitialized;
    public int ExportersMaxLowFrequencyStreams = 12;
    public int ExportersStartingPort = 11411;
    public GameObject TextLogObject;
    public Emitter<(Command, string)> CommandEmitter { get; private set; } = null;
    public int CommandEmitterPort = 11511;

    public enum PsiPipelineManagerState { Instantiated, Connecting, Connected, Served, Initializing, Initialized, Running, Stopped, Failed };
    public PsiPipelineManagerState State { private set; get; } = PsiPipelineManagerState.Instantiated;

    PsiPipelineManager()
    {
        exportersRegistered = new List<RemoteExporter>();
        eventExporter = null;
        importerComponents = new Dictionary<string, IPsiImporter>();
        sourceEndpoint = new Dictionary<string, Rendezvous.TcpSourceEndpoint>();
        remoteImporters = new Dictionary<string, RemoteImporter>();
        logBuffer = new List<string>();
        subPipelines = new List<Subpipeline>();
        exporterCount = waitedRendezVousCount = 0;
        initializedEventTriggered = false;
        text = null;
        Serializers = KnownSerializers.GetKnownSerializers();
        InitializeSerializer(Serializers);
    }

    public void RegisterComponentImporter(string streamName, IPsiImporter component)
    {
        importerComponents.Add(streamName, component);
        // If subscriber is late.
        //#if PSI_TCP_STREAMS
        //        if (sourceEndpoint.ContainsKey(streamName))
        //            component.ConnectionToTcpSource(sourceEndpoint[streamName]);
        //#endif
        //        if (remoteImporters.ContainsKey(streamName))
        //            component.ConnectionToImporter(remoteImporters[streamName]);
    }

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
            commandPipeline = Pipeline.Create(CommandProcessName);
            if (CommandEmitterPort != 0)
            {
                TcpWriter<(Command, string)> writer = new TcpWriter<(Command, string)>(commandPipeline, CommandEmitterPort, PsiFormatCommandString.GetFormat(), CommandProcessName);
                CommandEmitter = commandPipeline.CreateEmitter<(Command, string)>(this, $"{UsedProcessName}-{CommandProcessName}");
                CommandEmitter.PipeTo(writer.In);
                if (UsedAddress.Length == 0)
                    UsedAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
                var commandProcess = new Rendezvous.Process($"{UsedProcessName}-{CommandProcessName}");
                commandProcess.AddEndpoint(writer.ToRendezvousEndpoint(UsedAddress, CommandProcessName));
                rendezVousClient.Rendezvous.TryAddProcess(commandProcess);
            }
            commandPipeline.RunAsync();
            rendezVousClient.Rendezvous.ProcessRemoved += ProcessRemoved;
            rendezVousClient.Error += RendezVousClient_Error;
            rendezVousClient.Rendezvous.ProcessAdded += ProcessAdded;
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
        else
        {
            ProcessAddedData(process);
        }

        if (State <= PsiPipelineManagerState.Connected)
            State = waitedRendezVousCount >= WaitedProcess.Count ? PsiPipelineManagerState.Served : PsiPipelineManagerState.Connected;
        if (State == PsiPipelineManagerState.Served && onConnected != null)
            onConnected();
    }

    protected void ProcessAddedClock(Rendezvous.Process process)
    {
        foreach (var endpoint in process.Endpoints)
        {
            if (endpoint is Rendezvous.RemotePipelineClockExporterEndpoint remoteClockEndpoint)
            {
                AddLog($"PsiPipelineManager : Remote clock found!");
                remoteClockEndpoint.ToRemotePipelineClockImporter(pipeline);
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
                        var commandSubPipeline = Subpipeline.Create(commandPipeline, process.Name);
                        var tcpSource = source.ToTcpSource<(Command, string)>(commandSubPipeline, PsiFormatCommandString.GetFormat(), null, true, stream.StreamName);
                        SAAC.RendezVousPipelineServices.Helpers.PipeToMessage<(Command, string)> p2m = new SAAC.RendezVousPipelineServices.Helpers.PipeToMessage<(Command, string)>(commandSubPipeline, CommandHandling, process.Name);
                        Microsoft.Psi.Operators.PipeTo(tcpSource.Out, p2m.In);
                        commandSubPipeline.Start((d) => {});
                        AddLog($"PsiPipelineManager : Subpipeline {process.Name} started.");
                        return;
                    }
                }
            }
        }
    }

    protected void ProcessAddedData(Rendezvous.Process process)
    {
        if (!WaitedProcess.Contains(process.Name) && !AcceptedProcess.Contains(process.Name))
            return;

        Subpipeline subPipeline = Subpipeline.Create(pipeline, process.Name);
        foreach (var endpoint in process.Endpoints)
        {
#if PSI_TCP_STREAMS
            if (endpoint is Rendezvous.TcpSourceEndpoint tcpRemoteEndpoint)
            {
                foreach (Rendezvous.Stream stream in tcpRemoteEndpoint.Streams)
                {
                    AddLog($"PsiPipelineManager : Remote stream {stream.StreamName} found!");
                    sourceEndpoint.Add(stream.StreamName, tcpRemoteEndpoint);
                    if (importerComponents.ContainsKey(stream.StreamName))
                    {
                        importerComponents[stream.StreamName].ConnectionToTcpSource(tcpRemoteEndpoint, subPipeline);
                    }
                }
            }
#endif
            if (endpoint is Rendezvous.RemoteExporterEndpoint remoteEndpoint)
            {
                RemoteImporter remoteImporter = remoteEndpoint.ToRemoteImporter(subPipeline);
                foreach (Rendezvous.Stream stream in remoteEndpoint.Streams)
                {
                    AddLog($"PsiPipelineManager : Remote stream {stream.StreamName} found!");
                    remoteImporters.Add(stream.StreamName, remoteImporter);
                    if (importerComponents.ContainsKey(stream.StreamName))
                    {
                        importerComponents[stream.StreamName].ConnectionToImporter(remoteImporter);
                    }
                }
            }
        }
        if (WaitedProcess.Contains(process.Name))
            waitedRendezVousCount++;
        subPipelines.Add(subPipeline);
        if (State == PsiPipelineManagerState.Running)
            subPipeline.Start((d) => { });
        AddLog($"PsiPipelineManager : Subpipeline {process.Name} started.");
    }

    protected void CommandHandling(string processName, Message<(Command, string)> message)
    {
        var args = message.Data.Item2.Split(';');
        if (args[0] != UsedProcessName && message.Data.Item2.Length != 0)
            return;
        Command command = (Command)message.Data.Item1;
        AddLog($"PsiPipelineManager Recieve Command {command} from {processName} @{message.OriginatingTime} with argument {message.Data.Item2} \n");
        switch (command)
        {
            case Command.Reset:
                Reset();
                if (CommandEmitter != null)
                    CommandEmitter.Post((Command.Status, State.ToString()), commandPipeline.GetCurrentTime());
                break;
            case Command.Initialize:
                InitializeExporters();
                AddManagerProcess();
                break;
            case Command.Run:
                StartMode = PsiManagerStartMode.Automatic;
                break;
            case Command.Close:
            case Command.Stop:
                StopPsi();
                break;
            case Command.Status:
                if (CommandEmitter != null)
                    CommandEmitter.Post((Command.Status, State.ToString()), commandPipeline.GetCurrentTime());
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
        if (this.exporterCount == 0)
            State = PsiPipelineManagerState.Initializing;
    }

    public void AddProcess(Rendezvous.Process process)
    {
        if (State > PsiPipelineManagerState.Initializing)
            return;
        rendezVousClient.Rendezvous.TryAddProcess(process);
        AddLog($"PsiPipelineManager : Add process {process.Name} with {process.Endpoints.Count()} endpoints.");
    }

    public void AddManagerProcess()
    {
        if (State > PsiPipelineManagerState.Initializing)
            return;

        Rendezvous.Process proc = GetProcess();
        int count = proc.Endpoints.Count();
        if (ExporterNumberExpectedAtStart != 0 && count < ExporterNumberExpectedAtStart)
            return;
        rendezVousClient.Rendezvous.TryAddProcess(proc);
        AddLog($"PsiPipelineManager : Add process {proc.Name} with {count} endpoints.");
        State = PsiPipelineManagerState.Initialized;
    }

    private void StopPsi(int waitedMillisec = 1000)
    {
        State = PsiPipelineManagerState.Stopped;
        if (connectionThread != null && connectionThread.IsAlive)
            connectionThread.Abort();
        if (commandPipeline != null)
            commandPipeline.WaitAll(waitedMillisec);
        foreach (Subpipeline subP in subPipelines)
            subP.Stop(pipeline.GetCurrentTime(), () => { AddLog($"PsiPipelineManager : Subpipeline {subP.Name} stopped."); });
        if (pipeline != null)
            pipeline.WaitAll(waitedMillisec);
        pipeline = null;
        rendezVousClient.Stop();
    }

    protected RemoteExporter CreateRemoteExporter()
    {
        return new RemoteExporter(GetPipeline(), ExportersStartingPort + exporterCount++, TransportKind.NamedPipes);
    }

    public void GetRemoteExporter(ExportType type, out RemoteExporter exporter)
    {
        exporter = null;
        switch (type)
        {
            // Ref exporter issue to investigate.
            case ExportType.LowFrequency:
                {
                    if (eventExporter != null && eventExporter.Exporter.Metadata.Count() >= ExportersMaxLowFrequencyStreams)
                        exporter = eventExporter;
                    else
                        eventExporter = exporter = CreateRemoteExporter();
                }
                break;
            default:
                exporter = CreateRemoteExporter();
                break;
        }
    }

    public void RegisterExporter(ref RemoteExporter exporter)
    {
        if (UsedAddress.Length == 0)
            UsedAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
        if (exportersRegistered.Contains(exporter) == false)
        {
            exportersRegistered.Add(exporter);
            GetProcess().AddEndpoint(exporter.ToRendezvousEndpoint(UsedAddress));
        }
    }

    public TcpWriter<T> GetTcpWriter<T>(string topic, Microsoft.Psi.Interop.Serialization.IFormatSerializer<T> serializers)
    {
        TcpWriter<T> tcpWriter = new TcpWriter<T>(GetPipeline(), ExportersStartingPort + exporterCount++, serializers, topic);
        RegisterTCPWriter(tcpWriter, topic);
        return tcpWriter;
    }

    public void RegisterTCPWriter<T>(TcpWriter<T> writer, string topic)
    {
        if (UsedAddress.Length == 0)
            UsedAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
        AddLog($"PsiPipelineManager : Add {topic} endpoint to process.");
        GetProcess().AddEndpoint(writer.ToRendezvousEndpoint(UsedAddress, topic));
    }

    public Pipeline GetPipeline()
    {
        if (pipeline == null)
        {
            pipeline = Pipeline.Create(UsedProcessName);
            if (UsedAddress.Length == 0)
                UsedAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
        }
        return pipeline;
    }

    public Rendezvous.Process GetProcess()
    {
        if (process == null)
            process = new Rendezvous.Process(UsedProcessName, "1.0");
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
            AddLog($"PsiPipelineManager : pipeline running!");
        }
    }

    public void StartRendezVous()
    {
        if (State < PsiPipelineManagerState.Connecting)
        {
            AddLog($"PsiPipelineManager: IP used {UsedAddress}");
            if (TextLogObject != null && text == null)
            {
                text = TextLogObject.GetComponent<TMP_Text>();
                if (text != null)
                    text.text = "PsiPipelineManager logs:\n";
            }
            GetPipeline();
            rendezVousClient = new RendezvousClient(RendezVousServerAddress, RendezVousServerPort);
            connectionThread = new Thread(SyncServerConnection);
            connectionThread.Start();
            State = PsiPipelineManagerState.Connecting;
        }
    }

    public void Reset()
    {
        try
        {
            if (rendezVousClient != null)
            {
                foreach (Rendezvous.Process prc in rendezVousClient.Rendezvous.Processes)
                    if (!prc.Name.Contains(CommandProcessName))
                        rendezVousClient.Rendezvous.TryRemoveProcess(prc);

#if PSI_TCP_STREAMS
                sourceEndpoint.Clear();
#endif
                remoteImporters.Clear();
            }

            if (pipeline != null)
            {
                pipeline.Dispose();
                pipeline = Pipeline.Create(UsedProcessName);
            }
        }
        catch (Exception e)
        {
            AddLog($"PipelineManager : Reset error: {e.Message}");
            pipeline = Pipeline.Create(UsedProcessName);
        }
        AddLog("PipelineManager reset complete !");
        State = PsiPipelineManagerState.Connected;
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
                    StartRendezVous();
                break;
            case PsiManagerStartMode.Automatic:
                switch (State)
                {
                    case PsiPipelineManagerState.Instantiated:
                        StartRendezVous();
                        break;
                    case PsiPipelineManagerState.Served:
                        InitializeExporters();
                        break;
                    case PsiPipelineManagerState.Initializing:
                        AddManagerProcess();
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