﻿using Microsoft.Psi.Data;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Interop.Serialization;
using Microsoft.Psi.Remoting;
using Microsoft.Psi;
using static Microsoft.Psi.Interop.Rendezvous.Rendezvous;
using static Microsoft.Psi.Interop.Rendezvous.Operators;
using Microsoft.Psi.Diagnostics;
using Microsoft.Psi.Components;

// USING https://github.com/SaacPSI/psi/ branch 'Pipeline' version of Psi.Runtime package

namespace SAAC.PipelineServices
{
    public class RendezVousPipeline : DatasetPipeline, ISourceComponent
    {
        public const string ClockSynchProcessName = "ClockSynch";
        public const string DiagnosticsProcessName = "Diagnostics";
        public const string CommandProcessName = "Command";

        public enum Command { Initialize, Run, Stop, Restart, Close, Status };

        public RendezVousPipelineConfiguration Configuration { get; private set; }
        public Emitter<(Command, string)> CommandEmitter { get; private set; }
        public delegate void OnCommandReceive(string process, (Command, string) command);

        protected List<string> processNames; 
        protected bool isStarted;
        protected PsiFormatCommand commandFormat;
        protected RendezvousRelay rendezvousRelay;
        protected dynamic rendezVous;

        private Helpers.PipeToMessage<(Command, string)> p2m;

        public RendezVousPipeline(RendezVousPipelineConfiguration? configuration, string name = nameof(RendezVousPipeline), string? rendezVousServerAddress = null, LogStatus? log = null, Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null)
            : base(configuration, name, log)
        {
            Configuration = configuration ?? new RendezVousPipelineConfiguration();
            commandFormat = new PsiFormatCommand();
            CommandEmitter = Pipeline.CreateEmitter<(Command, string)>(this, $"{name}-CommandEmitter");
            processNames = new List<string>();
            if (rendezVousServerAddress == null)
                rendezvousRelay = rendezVous = new RendezvousServer(this.Configuration.RendezVousPort);
            else
                rendezvousRelay = rendezVous = new RendezvousClient(rendezVousServerAddress, this.Configuration.RendezVousPort);
            isStarted = isPipelineRunning = false;
        }

        public void Start()
        {
            if (isStarted)
                return;
            if (!StartRendezVousRelay())
                return;
            if (this.Configuration.AutomaticPipelineRun && this.Configuration.ClockPort != 0)
                RunPipeline();
            isStarted = true;
        }

        public override void Stop()
        {
            if (!isStarted)
                return;
            rendezVous.Stop();
            base.Stop();
            isStarted = false;
        }

        public bool AddConnectingProcess(string name, EventHandler<Process> eventProcess)
        {
            if (rendezvousRelay == null)
                return false;
            rendezvousRelay.Rendezvous.ProcessAdded += eventProcess;
            processNames.Add(name);
            return true;
        }

        public bool AddProcess(Process newProcess)
        {
            if (rendezvousRelay == null)
                return false;
            processNames.Add(newProcess.Name);
            if (!rendezvousRelay.Rendezvous.TryAddProcess(newProcess))
            {
                processNames.Remove(newProcess.Name);
                Log($"Failed to AddProcess {newProcess.Name}");
                return false;
            }
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

        protected bool StartRendezVousRelay()
        {
            if (rendezvousRelay == null)
                return false;
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
            if (Configuration.ClockPort != 0)
            {
                var remoteClock = new RemoteClockExporter(Configuration.ClockPort);
                AddProcess(new Rendezvous.Process(ClockSynchProcessName, [remoteClock.ToRendezvousEndpoint(Configuration.RendezVousHost)]));
            }
            if (Configuration.CommandPort != 0)
            {
                TcpWriterMulti<(Command, string)> writer = new TcpWriterMulti<(Command, string)>(Pipeline, Configuration.CommandPort, commandFormat.GetFormat(), CommandProcessName);
                CommandEmitter.PipeTo(writer.In);
                AddProcess(new Rendezvous.Process($"{name}-{CommandProcessName}", [writer.ToRendezvousEndpoint(Configuration.RendezVousHost, CommandProcessName)]));
            }
            rendezvousRelay.Rendezvous.ProcessAdded += ProcessAdded;
            rendezvousRelay.Rendezvous.ProcessRemoved += RendezvousProcessRemoved;
            rendezvousRelay.Error += (s, e) => { Log(e.Message); Log(e.HResult.ToString()); };
            rendezVous.Start();
            Log("RendezVous started!");
            return true;
        }

        protected void ProcessAdded(object? sender, Process process)
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
                switch (process.Name)
                {
                    case ClockSynchProcessName:
                        if (Configuration.ClockPort == 0)
                            ProcessAddedClock(process);
                        break;
                    default:
                        if(Configuration.RecordIncomingProcess)
                            ProcessAddedData(process);
                        break;
                }
        }

        protected void ProcessAddedClock(Process process)
        {
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockEndpoint) 
                { 
                    remoteClockEndpoint.ToRemoteClockImporter(Pipeline);
                    if (this.Configuration.AutomaticPipelineRun)
                        RunPipeline();
                    return;
                }
            }
        }

        protected void ProcessAddedCommand(Process process)
        {
            if (Configuration.CommandDelegate == null)
                return;
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint)
                {
                    TcpSourceEndpoint? source = endpoint as TcpSourceEndpoint;
                    if (source == null)
                        continue;
                    foreach (var stream in endpoint.Streams)
                    {
                        if (stream.StreamName == CommandProcessName)
                        {
                            Subpipeline commandSubPipeline = new Subpipeline(Pipeline, process.Name);
                            var tcpSource = Microsoft.Psi.Interop.Rendezvous.Operators.ToTcpSource<(Command, string)>(source, commandSubPipeline, commandFormat.GetFormat(), null, true, stream.StreamName);
                            p2m = new Helpers.PipeToMessage<(Command, string)>(commandSubPipeline, Configuration.CommandDelegate, process.Name, $"p2m-{process.Name}");
                            Microsoft.Psi.Operators.PipeTo(tcpSource.Out, p2m.In);
                            if (this.Configuration.AutomaticPipelineRun)
                            {
                                commandSubPipeline.RunAsync();
                                Log($"SubPipeline {process.Name} started.");
                            }
                            return;
                        }
                    }
                }
            }
        }

        protected void ProcessAddedDiagnotics(Process process)
        {
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.RemoteExporterEndpoint)
                {
                    RemoteExporterEndpoint? source = endpoint as RemoteExporterEndpoint;
                    if (source == null)
                        continue;
                    foreach (var stream in source.Streams)
                    {
                        if (stream.GetType() == typeof(PipelineDiagnostics))
                        {
                            Subpipeline processSubPipeline = new Subpipeline(Pipeline, process.Name);
                            Connection(stream.StreamName, DiagnosticsProcessName, CreateOrGetSession(Configuration.SessionName + "_Diagnostics"), source, processSubPipeline, true);
                            if (this.Configuration.AutomaticPipelineRun)
                            {
                                processSubPipeline.RunAsync();
                                Log($"SubPipeline {process.Name} started.");
                            }
                            return;
                        }
                    }
                }
            }
        }

        protected void ProcessAddedData(Process process)
        {
            int elementAdded = 0;
            Subpipeline processSubPipeline = new Subpipeline(Pipeline, process.Name);
            Session? session = CreateOrGetSessionFromMode(process.Name);
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint)
                {
                    TcpSourceEndpoint? source = endpoint as TcpSourceEndpoint;
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
                    RemoteExporterEndpoint? source = endpoint as RemoteExporterEndpoint;
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
            else if (this.Configuration.AutomaticPipelineRun)
            {
                processSubPipeline.RunAsync();
                Log($"SubPipeline {process.Name} started.");
                TriggerNewProcessEvent(process.Name);
            }
            //Dataset?.Save();
        }

        private void RendezvousProcessRemoved(object sender, Process e)
        {
            Pipeline subpipeline = subpipelines.Find(x => x.Name == e.Name);
            if (subpipeline != default(Pipeline))
            {
                subpipeline.Dispose();
                Connectors.Remove(e.Name);
            }
            RemovedProcess?.Invoke(this, e.Name);
        }

        protected void Connection<T>(string streamName, string processName, Session? session, TcpSourceEndpoint source, Pipeline p, bool storeSteam, Format<T> deserializer, Type? transformerType)
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

        protected bool Connection(string streamName, string processName, Session? session, RemoteExporterEndpoint source, Pipeline p, bool storeSteam)
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

        void ISourceComponent.Start(Action<DateTime> notifyCompletionTime)
        {}

        void ISourceComponent.Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            notifyCompleted();
        }
    }
}
