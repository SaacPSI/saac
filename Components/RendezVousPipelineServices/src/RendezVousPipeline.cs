using Microsoft.Psi.Data;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Interop.Serialization;
using Microsoft.Psi.Remoting;
using Microsoft.Psi;
using static Microsoft.Psi.Interop.Rendezvous.Rendezvous;
using static Microsoft.Psi.Interop.Rendezvous.Operators;
using System.IO;
using Microsoft.Psi.Interop.Transport;
using Microsoft.Psi.Diagnostics;
using Microsoft.Psi.Components;

// USING https://github.com/SaacPSI/psi/ branch 'Pipeline' version of Psi.Runtime package

namespace SAAC.RendezVousPipelineServices
{
    public class RendezVousPipeline : ConnectorsAndStoresCreator, ISourceComponent
    {
        public const string ClockSynchProcessName = "ClockSynch";
        public const string DiagnosticsProcessName = "Diagnostics";
        public const string CommandProcessName = "Command";

        public enum SessionNamingMode { Unique, Increment, Overwrite };
        public enum StoreMode { Independant, Process, Dictionnary };
        public enum DiagnosticsMode { Off, Store, Export };
        public enum Command { Initialize, Run, Stop, Restart, Close, Status };

        public Dataset? Dataset { get; private set; }
        public Pipeline Pipeline { get; private set; }
        public RendezVousPipelineConfiguration Configuration { get; private set; }
        public EventHandler<(string, Dictionary<string, Dictionary<string, ConnectorInfo>>)>? NewProcess;
        public EventHandler<string>? RemovedProcess;
        public LogStatus Log;
        public Emitter<(Command, string)> CommandEmitter { get; private set; }
        public delegate void OnCommandReceive(string process, (Command, string) command);

        protected List<string> processNames; 
        protected bool isStarted;
        protected bool isPipelineRunning;
        protected string name;
        protected PsiFormatCommand commandFormat;
        protected RendezvousRelay rendezvousRelay;
        protected dynamic rendezVous;

        private Helpers.PipeToMessage<(Command, string)> p2m;

        public RendezVousPipeline(RendezVousPipelineConfiguration? configuration, string name = nameof(RendezVousPipeline), string? rendezVousServerAddress = null, LogStatus? log = null)
        {
            this.name = name;
            Configuration = configuration ?? new RendezVousPipelineConfiguration();
            this.Log = log ?? ((log) => { Console.WriteLine(log); });
            commandFormat = new PsiFormatCommand();
            Pipeline = Pipeline.Create(enableDiagnostics: configuration?.Diagnostics != DiagnosticsMode.Off);
            CommandEmitter = Pipeline.CreateEmitter<(Command, string)>(this, $"{name}-CommandEmitter");
            processNames = new List<string>();
            if (this.Configuration.DatasetName.Length > 4)
            {
                if (File.Exists(this.Configuration.DatasetPath + this.Configuration.DatasetName))
                    Dataset = Dataset.Load(this.Configuration.DatasetPath + this.Configuration.DatasetName, true);
                else
                { 
                    Dataset = new Dataset(this.Configuration.DatasetName, this.Configuration.DatasetPath + this.Configuration.DatasetName, true);
                    Dataset.Save(); // throw exception here if the path is not correct
                }
                StorePath = this.Configuration.DatasetPath;
            }
            else
                Dataset = null;

            if (rendezVousServerAddress == null)
                rendezvousRelay = rendezVous = new RendezvousServer(this.Configuration.RendezVousPort);
            else
                rendezvousRelay = rendezVous = new RendezvousClient(rendezVousServerAddress, this.Configuration.RendezVousPort);
            isStarted = isPipelineRunning = false;
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        public bool RunPipeline()
        {
            if (isPipelineRunning)
                return true;
            try
            {
                Pipeline.RunAsync();
                isPipelineRunning = true;
            }
            catch(Exception ex) 
            {
                Log($"{ex.Message}\n{ex.InnerException}");
            }
            return isPipelineRunning;
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

        public void Stop()
        {
            if (!isStarted)
                return;
            rendezVous.Stop();
            if (isPipelineRunning)
                Pipeline.Dispose();
            Dataset?.Save();
            isStarted = isPipelineRunning = false;
        }

        public Session? GetSession(string sessionName)
        {
            if (Dataset != null)
            {
                if (sessionName.EndsWith("."))
                {
                    Session? sessionTmp = null;
                    foreach (var session in Dataset.Sessions)
                    {
                        if (session != null && session.Name.Contains(sessionName))
                        {
                            if (sessionTmp != null)
                            {
                                if (session.Name.Replace(sessionName, "").CompareTo(sessionTmp.Name.Replace(sessionName, "")) < 0)
                                    continue;
                            }
                            sessionTmp = session;
                        }
                    }
                    return sessionTmp;
                }
                else
                {
                    foreach (var session in Dataset.Sessions)
                    {
                        if (session != null && session.Name == sessionName)
                            return session;
                    }
                }
            }      
            return null;
        }

        public Session? CreateOrGetSession(string sessionName)
        {
            if (Dataset == null)
                return null;
            foreach (var session in Dataset.Sessions)
                if (session != null && session.Name == sessionName)
                    return session;
            return Dataset.AddEmptySession(sessionName);
        }

        public Session? CreateIterativeSession(string sessionName)
        {
            if (Dataset == null)
                return null;
            int iterator = 0;
            foreach (var session in Dataset.Sessions)
                if (session != null && session.Name.Contains(sessionName))
                    iterator++;
            return Dataset.AddEmptySession($"{sessionName}.{iterator:D3}");
        }

        public Session? CreateOrGetSessionFromMode(string sessionName)
        {
            switch (Configuration.SessionMode)
            {
                case SessionNamingMode.Unique:
                    return CreateOrGetSession(Configuration.SessionName);
                case SessionNamingMode.Overwrite:
                    return CreateOrGetSession(Configuration.SessionName + sessionName);
                case SessionNamingMode.Increment:
                default:
                    return CreateIterativeSession(Configuration.SessionName + sessionName);
            }
        }

        public (string, string) GetStoreName(string streamName, string processName, Session? session)
        {
            switch (Configuration.StoreMode)
            {
                case StoreMode.Process:
                    return (streamName, processName);
                case StoreMode.Dictionnary:
                    if (Configuration.StreamToStore.ContainsKey(streamName) && session != null)
                    {
                        string storeName = Configuration.StreamToStore[streamName];
                        if (storeName.Contains("%s"))
                            storeName = storeName.Replace("%s", session.Name);
                        if (storeName.Contains("%p"))
                        {
                            storeName = storeName.Replace("%p", processName);
                            return (streamName, storeName);
                        }
                        return ($"{processName}-{streamName}", storeName);
                    }
                    goto default;
                default:
                case StoreMode.Independant:
                    return (streamName, $"{processName}-{streamName}");
            }
        }

        public Pipeline CreateSubpipeline(string name = "SaaCSubpipeline")
        {
            return Microsoft.Psi.Pipeline.CreateSynchedPipeline(Pipeline, name);
        }

        public void TriggerNewProcessEvent(string name)
        {
            NewProcess?.Invoke(this, (name, Connectors));
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
                processNames.Remove(processName);
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
            var subpipeline = Pipeline.GetElementsOfType<Subpipeline>().Find(x => x.Name == e.Name);
            subpipeline?.Dispose();
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
