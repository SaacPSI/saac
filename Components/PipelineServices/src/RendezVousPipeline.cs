using Microsoft.Psi.Data;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Interop.Serialization;
using Microsoft.Psi.Remoting;
using Microsoft.Psi;
using Microsoft.Psi.Diagnostics;
using Microsoft.Psi.Components;
using Microsoft.Psi.Interop.Transport;
using System.Runtime.InteropServices;
using System.Diagnostics.Eventing.Reader;

// USING https://github.com/SaacPSI/psi/ branch 'Pipeline' version of Psi.Runtime package

namespace SAAC.PipelineServices
{
    public class RendezVousPipeline : DatasetPipeline, ISourceComponent
    {
        public const string ClockSynchProcessName = "ClockSynch";
        public const string DiagnosticsProcessName = "Diagnostics";
        public const string CommandProcessName = "Command";

        public enum Command { Initialize, Run, Stop, Reset, Close, Status };

        public RendezVousPipelineConfiguration Configuration { get; private set; }
        public delegate void OnCommandReceive(string process, (Command, string) command);

        protected Emitter<(Command, string)> CommandEmitter { get; private set; }
        protected List<string> processNames; 
        protected bool isStarted;
        protected RendezvousRelay rendezvousRelay;
        protected dynamic rendezVous;
        protected Pipeline commandPipeline;
        protected List<Rendezvous.Process> rendezvousProcessesToAddWhenActive;

        private Helpers.PipeToMessage<(Command, string)> p2m;

        public RendezVousPipeline(Pipeline parent, RendezVousPipelineConfiguration? configuration, string name = nameof(RendezVousPipeline), string? rendezVousServerAddress = null, LogStatus? log = null, Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null)
            : base(parent, configuration, name, log, connectors)
        {
            Initialize(configuration, name, rendezVousServerAddress, log);
        }

        public RendezVousPipeline(RendezVousPipelineConfiguration? configuration, string name = nameof(RendezVousPipeline), string? rendezVousServerAddress = null, LogStatus? log = null, Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null)
            : base(configuration, name, log, connectors)
        {
            Initialize(configuration, name, rendezVousServerAddress, log);
        }

        public void Start(TimeInterval? interval = null)
        {
            if (isStarted)
                return;
            isStarted = true;
            if (!StartRendezVousRelay(interval ?? TimeInterval.Infinite))
                return;
            if (this.Configuration.AutomaticPipelineRun && this.Configuration.ClockPort != 0)
                RunPipelineAndSubpipelines();
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            Start();
            notifyCompletionTime(DateTime.MaxValue);
        }

        public override void Stop(int maxWaitingTime = 1000)
        {
            Dataset?.Save();
            if (!isStarted)
                return;
            isStarted = false;
            rendezVous.Stop();
            base.Stop(maxWaitingTime);
        }
        
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            Stop();
            notifyCompleted();
        }

        public void Dispose()
        {
            Dataset?.Save();
            rendezVous.Dispose();
            commandPipeline.Dispose();
            base.Dispose();
            Stores = null;
        }

        public override void Reset(Pipeline? pipeline = null)
        {
            base.Reset(pipeline);
            processNames?.Clear();
            foreach(Rendezvous.Process prc in rendezvousRelay.Rendezvous.Processes)
                if (!prc.Name.Contains(CommandProcessName))
                    rendezvousRelay.Rendezvous.TryRemoveProcess(prc);
        }

        public bool SendCommand(Command command, string target, string arguments)
        {
            if (CommandEmitter != null)
            {
                CommandEmitter.Post((command, $"{target};{arguments}"), commandPipeline.GetCurrentTime());
                return true;
            }
            return false;
        }

        public bool AddSynchClockProcess(TimeInterval interval)
        {
            if (!processNames.Contains(ClockSynchProcessName))
            {
                if (OwningPipeline)
                {
                    var remoteClock = new RemoteClockExporter(Configuration.ClockPort);
                    AddProcess(new Rendezvous.Process(ClockSynchProcessName, [remoteClock.ToRendezvousEndpoint(Configuration.RendezVousHost)]));
                }
                else
                {
                    var remoteClock = new RemotePipelineClockExporter(Pipeline, Configuration.ClockPort, interval);
                    AddProcess(new Rendezvous.Process(ClockSynchProcessName, [remoteClock.ToRendezvousEndpoint(Configuration.RendezVousHost)]));
                }

                return true;
            }
            return false;
        }

        public bool AddConnectingProcess(string name, EventHandler<Rendezvous.Process> eventProcess)
        {
            if (rendezvousRelay == null)
                return false;
            rendezvousRelay.Rendezvous.ProcessAdded += eventProcess;
            processNames.Add(name);
            return true;
        }

        public bool AddProcess(Rendezvous.Process newProcess)
        {
            if (rendezvousRelay == null)
                return false;
            processNames.Add(newProcess.Name);
            if (rendezVous.IsActive)
            {
                if (!rendezvousRelay.Rendezvous.TryAddProcess(newProcess))
                {
                    processNames.Remove(newProcess.Name);
                    Log($"Failed to AddProcess {newProcess.Name}");
                    return false;
                }
            }
            else
                rendezvousProcessesToAddWhenActive.Add(newProcess);
            return true;
        }

        public bool RemoveProcess(string processName)
        {
            if (rendezvousRelay == null)
                return false;
            if (rendezvousRelay.Rendezvous.TryRemoveProcess(processName))
            {
                processNames.Remove(processName);
                Connectors.Remove(processName);
            }
            else
                return false;
            return true;
        }

        public void GenerateTCPEnpoint<T>(Pipeline parent, int port, IProducer<T> producer, string streamName, ref Rendezvous.Process process)
        {
            Type type = typeof(T);
            if (!Configuration.TypesSerializers.ContainsKey(type))
                throw new Exception($"Missing serializer of type {type} in configuration.");
            TcpWriter<T> writer = new TcpWriter<T>(parent, port, Configuration.TypesSerializers[type].GetFormat());
            producer.PipeTo(writer);
            process.AddEndpoint(writer.ToRendezvousEndpoint(Configuration.RendezVousHost, streamName));
        }

        public int GenerateTCPProcessFromConnectors(string storeName, int startingPort)
        {
            if (Connectors.ContainsKey(storeName))
                return GenerateTCPProcessFromConnectors(storeName, Connectors[storeName], startingPort);
            return 0;
        }

        public int GenerateTCPProcessFromConnectors(string processName, Dictionary<string, ConnectorInfo> connectors, int startingPort)
        {
            Rendezvous.Process process = new Rendezvous.Process(processName);
            Subpipeline parent = GetOrCreateSubpipeline(processName);
            foreach (var connector in connectors)
            {
                var producer = typeof(ConnectorInfo).GetMethod("CreateBridge").MakeGenericMethod(connector.Value.DataType).Invoke(connector.Value, [parent]);
                typeof(RendezVousPipeline).GetMethod("GenerateTCPEnpoint").MakeGenericMethod(connector.Value.DataType).Invoke(this, [parent, startingPort++, producer, connector.Key, process]);
            }
            if (isPipelineRunning)
                parent.Start((d) => { Log($"SubPipeline {process.Name} started @{d}."); });
            return AddProcess(process) ? startingPort : 0;
        }

        public void GenerateRemoteEnpoint(Pipeline parent, int port, Dictionary<string, ConnectorInfo> connectors, ref Rendezvous.Process process)
        {
            RemoteExporter writer = new RemoteExporter(parent, port, TransportKind.Tcp);
            foreach (var connector in connectors)
            {
                var producer = typeof(ConnectorInfo).GetMethod("CreateBridge").MakeGenericMethod(connector.Value.DataType).Invoke(connector.Value, [parent]);
                // Marshal.SizeOf(connector.Value.DataType) > 4096 if true allow only one stream in exporter ?
                typeof(Exporter).GetMethod("Write").MakeGenericMethod(connector.Value.DataType).Invoke(writer.Exporter, [producer, connector.Key, Marshal.SizeOf(connector.Value.DataType) > 4096]);
            }
            process.AddEndpoint(writer.ToRendezvousEndpoint(Configuration.RendezVousHost));
        }

        public bool GeneraterRmoteProcessFromConnectors(string storeName, int startingPort)
        {
            if (Connectors.ContainsKey(storeName))
                return GenerateRemoteProcessFromConnectors(storeName, Connectors[storeName], startingPort);
            return false;
        }

        public bool GenerateRemoteProcessFromConnectors(string processName, Dictionary<string, ConnectorInfo> connectors, int startingPort)
        {
            Rendezvous.Process process = new Rendezvous.Process(processName);
            Pipeline parent = GetOrCreateSubpipeline(processName);
            GenerateRemoteEnpoint(parent, startingPort, connectors, ref process);
            parent.RunAsync();
            return AddProcess(process);
        }

        protected bool StartRendezVousRelay(TimeInterval? interval = null, bool createSynchClockProcess = true)
        {
            if (rendezvousRelay == null)
                return false;
            rendezvousRelay.Rendezvous.ProcessAdded += RendezvousProcessAdded;
            rendezvousRelay.Rendezvous.ProcessRemoved += RendezvousProcessRemoved;
            rendezvousRelay.Error += (s, e) => { Log(e.Message); Log(e.HResult.ToString()); };
            rendezVous.Start();
            foreach (Rendezvous.Process prc in rendezvousProcessesToAddWhenActive)
                AddProcess(prc);
            rendezvousProcessesToAddWhenActive.Clear();
            if (Dataset != null)
            {
                switch (Configuration.Diagnostics)
                {
                    case DiagnosticsMode.Store:
                        CreateStore(Pipeline, CreateOrGetSessionFromMode(Configuration.SessionName + "_Diagnostics"), name, DiagnosticsProcessName, Pipeline.Diagnostics);
                        break;
                    case DiagnosticsMode.Export:
                        var remoteDiagnostics = new RemoteExporter(Pipeline, Configuration.DiagnosticPort, TransportKind.Tcp);
                        remoteDiagnostics.Exporter.Write(Pipeline.Diagnostics, DiagnosticsProcessName);
                        AddProcess(new Rendezvous.Process($"{name}-{DiagnosticsProcessName}", [remoteDiagnostics.ToRendezvousEndpoint(Configuration.RendezVousHost)]));
                        break;
                }
            }
            if (this.Configuration.ClockPort != 0)
                AddSynchClockProcess(interval);
            commandPipeline = Pipeline.Create(CommandProcessName,DeliveryPolicy.SynchronousOrThrottle, enableDiagnostics: false);
            if (Configuration.CommandPort != 0)
            {
                CommandEmitter = commandPipeline.CreateEmitter<(Command, string)>(this, $"{name}-CommandEmitter");
                TcpWriterMulti<(Command, string)> writer = new TcpWriterMulti<(Command, string)>(commandPipeline, Configuration.CommandPort, PsiFormats.PsiFormatCommand.GetFormat(), CommandProcessName);
                CommandEmitter.PipeTo(writer);
                AddProcess(new Rendezvous.Process($"{name}-{CommandProcessName}", [writer.ToRendezvousEndpoint(Configuration.RendezVousHost, CommandProcessName)]));
            }
            commandPipeline.RunAsync(ReplayDescriptor.ReplayAllRealTime);
            Log("RendezVous started!");
            return true;
        }

        protected void RendezvousProcessAdded(object? sender, Rendezvous.Process process)
        {
            Log($"Process {process.Name}");
            if (processNames.Contains(process.Name))
                return;
            if (process.Name.Contains(CommandProcessName))
            {
                if (!process.Name.Contains(name))
                    ProcessAddedCommand(process);
                else
                    return;
            }
            else if (process.Name.Contains(DiagnosticsProcessName))
            {
                if (!process.Name.Contains(name))
                    ProcessAddedDiagnotics(process);
                else
                    return;
            }
            else
            {
                switch (process.Name)
                {
                    case ClockSynchProcessName:
                        if (Configuration.ClockPort == 0)
                            ProcessAddedClock(process);
                        break;
                    default:
                        if (Configuration.RecordIncomingProcess)
                            ProcessAddedData(process);
                        break;
                }
            }
        }

        protected void ProcessAddedClock(Rendezvous.Process process)
        {
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.RemotePipelineClockExporterEndpoint remotePipelineClockEndpoint) 
                {
                    remotePipelineClockEndpoint.ToRemotePipelineClockImporter(Pipeline);
                    if (this.Configuration.AutomaticPipelineRun)
                        RunPipelineAndSubpipelines();
                    return;
                }
                else if (endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockEndpoint)
                {
                    remoteClockEndpoint.ToRemoteClockImporter(Pipeline);
                    if (this.Configuration.AutomaticPipelineRun)
                        RunPipelineAndSubpipelines();
                    return;
                }
            }
        }

        protected void ProcessAddedCommand(Rendezvous.Process process)
        {
            if (Configuration.CommandDelegate == null)
                return;
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint)
                {
                    Rendezvous.TcpSourceEndpoint? source = endpoint as Rendezvous.TcpSourceEndpoint;
                    if (source == null)
                        continue;
                    foreach (var stream in endpoint.Streams)
                    {
                        if (stream.StreamName == CommandProcessName)
                        {
                            Subpipeline subCommandPipeline = Subpipeline.Create(commandPipeline, process.Name);
                            var tcpSource = Microsoft.Psi.Interop.Rendezvous.Operators.ToTcpSource<(Command, string)>(source, subCommandPipeline, PsiFormats.PsiFormatCommand.GetFormat(), null, true, stream.StreamName);
                            p2m = new Helpers.PipeToMessage<(Command, string)>(subCommandPipeline, Configuration.CommandDelegate, process.Name, $"p2m-{process.Name}");
                            Microsoft.Psi.Operators.PipeTo(tcpSource.Out, p2m.In);
                            subCommandPipeline.Start((d) => {}); 
                            Log($"Subpipeline {process.Name} started."); 
                            return;
                        }
                    }
                }
            }
        }

        protected void ProcessAddedDiagnotics(Rendezvous.Process process)
        {
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.RemoteExporterEndpoint)
                {
                    Rendezvous.RemoteExporterEndpoint? source = endpoint as Rendezvous.RemoteExporterEndpoint;
                    if (source == null)
                        continue;
                    foreach (var stream in source.Streams)
                    {
                        if (stream.GetType() == typeof(PipelineDiagnostics))
                        {
                            Subpipeline processSubPipeline = GetOrCreateSubpipeline(process.Name);
                            Connection(stream.StreamName, DiagnosticsProcessName, CreateOrGetSession(Configuration.SessionName + "_Diagnostics"), source, processSubPipeline, true);
                            if (isPipelineRunning)
                            {
                                processSubPipeline.Start((d) => {});
                                Log($"SubPipeline {process.Name} started.");
                            }
                            return;
                        }
                    }
                }
            }
        }

        protected void ProcessAddedData(Rendezvous.Process process)
        {
            int elementAdded = 0;
            Subpipeline processSubPipeline = GetOrCreateSubpipeline(process.Name);
            Session? session = CreateOrGetSessionFromMode(process.Name);
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint)
                {
                    Rendezvous.TcpSourceEndpoint? source = endpoint as Rendezvous.TcpSourceEndpoint;
                    if (source == null)
                        continue;
                    foreach (var stream in endpoint.Streams)
                    {
                        Log($"\tStream {stream.StreamName}");
                        if (Configuration.TopicsTypes.ContainsKey(stream.StreamName))
                        {
                            Type type = Configuration.TopicsTypes[stream.StreamName];
                            if (!Configuration.TypesSerializers.ContainsKey(type))
                                throw new Exception($"Missing serializer of type {type} in configuration.");
                            Connection(stream.StreamName, process.Name, session, source, processSubPipeline, !this.Configuration.NotStoredTopics.Contains(stream.StreamName), Configuration.TypesSerializers[type].GetFormat(), Configuration.Transformers.ContainsKey(stream.StreamName) ? Configuration.Transformers[stream.StreamName] : null);
                            elementAdded++;
                        }
                    }
                }
                else if (endpoint is Rendezvous.RemoteExporterEndpoint)
                {
                    Rendezvous.RemoteExporterEndpoint? source = endpoint as Rendezvous.RemoteExporterEndpoint;
                    if (source == null)
                        continue;
                    foreach (var stream in source.Streams)
                        elementAdded += Connection(stream.StreamName, process.Name, session, source, processSubPipeline, !this.Configuration.NotStoredTopics.Contains(stream.StreamName)) ? 1 : 0;
                }
            }
            Log($"Process {process.Name} sources added : {elementAdded}");
            if (elementAdded == 0 && session != null)
            {
                processSubPipeline.Dispose();
                if (session.Partitions.Count() == 0)
                    Dataset?.RemoveSession(session);
                return;
            }
            else if (isPipelineRunning)
            {
                processSubPipeline.Start((d) => { });
                Log($"SubPipeline {process.Name} started.");
            }
            TriggerNewProcessEvent(process.Name);
            //Dataset?.Save();
        }

        private void RendezvousProcessRemoved(object sender, Rendezvous.Process e)
        {
            if (subpipelines.ContainsKey(e.Name) )
            {
                subpipelines[e.Name].Dispose();
                Connectors.Remove(e.Name);
            }
            RemovedEntry?.Invoke(this, e.Name);
        }

        protected void Connection<T>(string streamName, string processName, Session? session, Rendezvous.TcpSourceEndpoint source, Pipeline p, bool storeSteam, Format<T> deserializer, Type? transformerType)
        {
            var storeName = GetStoreName(streamName, processName, session);
            var tcpSource = source.ToTcpSource<T>(p, deserializer, null, true, $"{processName}-{streamName}");
            if (Configuration.Debug)
                tcpSource.Do((d, e) => { Log($"Recieve {processName}-{streamName} data @{e.OriginatingTime} : {d}"); });
            if (transformerType != null)
            {
                dynamic transformer = Activator.CreateInstance(transformerType, [p, $"{processName}-{streamName}_transformer"]);
                Microsoft.Psi.Operators.PipeTo(tcpSource.Out, transformer.In);
                if (transformerType.GetInterfaces().Intersect([typeof(IComplexTransformer)]).Count() > 0)
                    transformer.CreateConnections(streamName, storeName, session, p, storeSteam, this);
                else
                    CreateConnectorAndStore(storeName.Item1, storeName.Item2, session, p, transformer.Out.Type, transformer, storeSteam);
            }
            else
                CreateConnectorAndStore(storeName.Item1, storeName.Item2, session, p, typeof(T), tcpSource, storeSteam);
        }

        protected bool Connection(string streamName, string processName, Session? session, Rendezvous.RemoteExporterEndpoint source, Pipeline p, bool storeSteam)
        {
            var importer = source.ToRemoteImporter(p);
            if (!importer.Connected.WaitOne())
            {
                Log($"Failed to connect to {streamName}");
                return false;
            }
            foreach (var streamInfo in importer.Importer.AvailableStreams)
            {
                // This is on hold as it constraint more the use of the rendezVousPipeline system.
                //if (!Configuration.TopicsTypes.ContainsKey(streamInfo.Name) || streamName != streamInfo.Name)
                //    continue;
                Log($"\tStream {streamName}");
                var storeName = GetStoreName(streamName, processName, session);
                Type type = Type.GetType(streamInfo.TypeName);
                typeof(ConnectorsAndStoresCreator).GetMethod("CreateConnectorAndStore").MakeGenericMethod(type).Invoke(this, [streamInfo.Name, $"{storeName.Item2}-{streamInfo.Name}", session, p, type, typeof(Importer).GetMethod("OpenStream").MakeGenericMethod(type).Invoke(importer.Importer, [streamInfo.Name, null, null]), storeSteam]);
                return true;
            }
            return false;
        }

        private void Initialize(RendezVousPipelineConfiguration? configuration, string name = nameof(RendezVousPipeline), string? rendezVousServerAddress = null, LogStatus? log = null)
        {
            Configuration = configuration ?? new RendezVousPipelineConfiguration();
            rendezvousProcessesToAddWhenActive = new List<Rendezvous.Process>();
            processNames = new List<string>();
            if (rendezVousServerAddress == null)
                rendezvousRelay = rendezVous = new RendezvousServer(this.Configuration.RendezVousPort);
            else
                rendezvousRelay = rendezVous = new RendezvousClient(rendezVousServerAddress, this.Configuration.RendezVousPort);
            isStarted = isPipelineRunning = false;
        }
    }
}
