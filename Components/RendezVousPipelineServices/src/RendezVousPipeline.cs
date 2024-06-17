using Microsoft.Psi.Data;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Interop.Serialization;
using Microsoft.Psi.Remoting;
using Microsoft.Psi;
using static Microsoft.Psi.Interop.Rendezvous.Rendezvous;
using System.IO;
using Microsoft.Psi.Interop.Transport;
using Microsoft.Psi.Diagnostics;

// USING https://github.com/SaacPSI/psi/ branch 'Pipeline' version of Psi.Runtime package

namespace SAAC.RendezVousPipelineServices
{
    public abstract class RendezVousPipeline
    {
        public const string ClockSynchProcessName = "ClockSynch";
        public const string DiagnosticsProcessName = "Diagnostics";
        public const string CommandProcessName = "Command";

        public enum SessionNamingMode { Unique, Increment, Overwrite };
        public enum StoreMode { Independant, Process, Dictionnary };
        public enum DiagnosticsMode { Off, Store, Export };
        public enum Command { Run, Stop, Restart, Close, Status };

        public Dataset? Dataset { get; private set; }
        public Dictionary<string, Dictionary<string, ConnectorInfo>> Connectors { get; private set; }
        public Dictionary<string, Dictionary<string, PsiExporter>> Stores { get; private set; }
        public RendezVousPipelineConfiguration Configuration { get; private set; }
        public EventHandler<(string, Dictionary<string, Dictionary<string, ConnectorInfo>>)>? NewProcess;
        public delegate void LogStatus(string log);
        public Emitter<(Command, string)> CommandEmitter { get; private set; }
        public delegate void OnCommandReceive(string process, (Command, string) command);

        protected List<string> processNames;
        protected LogStatus log;
        protected Pipeline pipeline;
        protected bool isStarted;
        protected bool isPipelineRunning;
        protected string name;
        protected PsiFormatIntString commandFormat;
        protected RendezvousRelay? rendezvousRelay;

        public RendezVousPipeline(RendezVousPipelineConfiguration? configuration, string name = nameof(RendezVousPipeline), LogStatus? log = null)
        {
            this.name = name;
            this.Configuration = configuration ?? new RendezVousPipelineConfiguration();
            this.log = log ?? ((log) => { Console.WriteLine(log); });
            pipeline = Pipeline.Create(enableDiagnostics: this.Configuration.Diagnostics != DiagnosticsMode.Off);
            Connectors = new Dictionary<string, Dictionary<string, ConnectorInfo>>();
            Stores = new Dictionary<string, Dictionary<string, PsiExporter>>();
            commandFormat = new PsiFormatIntString();
            CommandEmitter = pipeline.CreateEmitter<(Command, string)>(this, $"{name}-CommandEmitter");
            rendezvousRelay = null;
            processNames = new List<string>();
            if (this.Configuration.AutomaticPipelineRun && this.Configuration.ClockPort == 0)
                throw new Exception("It is not possible to have AutomaticPipelineRun without ClockServer.");
            if (this.Configuration.DatasetName.Length > 4)
            {
                if (File.Exists(this.Configuration.DatasetPath + this.Configuration.DatasetName))
                    Dataset = Dataset.Load(this.Configuration.DatasetPath + this.Configuration.DatasetName);
                else
                    Dataset = new Dataset(this.Configuration.DatasetName, this.Configuration.DatasetPath + this.Configuration.DatasetName);
            }
            else
                Dataset = null;
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
                pipeline.RunAsync();
                isPipelineRunning = true;
            }
            catch(Exception ex) 
            {
                log($"{ex.Message}\n{ex.InnerException}");
            }
            return isPipelineRunning;
        }

        public void Start()
        {
            if (isStarted)
                return;
            if (StartRendezVousRelay())
                return;
            if (this.Configuration.AutomaticPipelineRun)
                RunPipeline();
            isStarted = true;
        }

        public void Stop()
        {
            if (!isStarted)
                return;
            StopRendezVous();
            if (isPipelineRunning)
                pipeline.Dispose();
            Dataset?.Save();
            isStarted = isPipelineRunning = false;
        }

        public Session? GetSession(string sessionName)
        {
            if (Dataset != null)
                foreach (var session in Dataset.Sessions)
                    if (session != null && session.Name == sessionName)
                        return session;
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

        public void CreateConnectorAndStore<T>(string name, string storeName, Session? session, Pipeline p, Type type, IProducer<T> stream, bool storeSteam)
        {
            if (!Connectors.ContainsKey(storeName))
                Connectors.Add(storeName, new Dictionary<string, ConnectorInfo>());
            Connectors[storeName].Add(name, new ConnectorInfo(name, storeName, session == null ? "" : session.Name, type, stream));
            if (storeSteam && session != null)
                CreateStore(p, session, name, storeName, stream);
        }

        public void CreateStore<T>(Pipeline pipeline, Session session, string name, string storeName, IProducer<T> source)
        {
            if (Stores.ContainsKey(session.Name) && Stores[session.Name].ContainsKey(storeName))
            {
                Stores[session.Name][storeName].Write(source, name);
            }
            else
            {
                PsiExporter store = PsiStore.Create(pipeline, storeName, $"{Configuration.DatasetPath}/{session.Name}/");
                store.Write(source, name);
                session.AddPartitionFromPsiStoreAsync(storeName, $"{Configuration.DatasetPath}/{session.Name}/");
                if (!Stores.ContainsKey(session.Name))
                    Stores.Add(session.Name, new Dictionary<string, PsiExporter>());
                Stores[session.Name].Add(storeName, store);
            }
        }

        public Subpipeline CreateSubpipeline(string name = "SaaCSubpipeline")
        {
            return new Subpipeline(pipeline, name);
        }

        public void TriggerNewProcessEvent(string name)
        {
            NewProcess?.Invoke(this, (name, Connectors));
        }

        public bool AddProcess(Process newProcess)
        {
            if (rendezvousRelay == null)
                return false;
            if (rendezvousRelay.Rendezvous.TryAddProcess(newProcess))
                processNames.Add(newProcess.Name);
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
                        CreateStore(pipeline, Dataset.AddEmptySession(Configuration.SessionName + "_Diagnostics"), name, DiagnosticsProcessName, pipeline.Diagnostics);
                        break;
                    case DiagnosticsMode.Export:
                        var remoteDiagnostics = new RemoteExporter(pipeline, 13332, TransportKind.Tcp);
                        remoteDiagnostics.Exporter.Write(pipeline.Diagnostics, DiagnosticsProcessName);
                        AddProcess(new Rendezvous.Process($"{name}-{DiagnosticsProcessName}", [remoteDiagnostics.ToRendezvousEndpoint(Configuration.RendezVousHost)]));
                        break;
                }
            }
            if (Configuration.ClockPort != 0)
            {
                var remoteClock = new RemoteClockExporter(Configuration.ClockPort);
                AddProcess(new Rendezvous.Process(ClockSynchProcessName, [remoteClock.ToRendezvousEndpoint(Configuration.RendezVousHost)]));
            }
            TcpWriter<(Command, string)> writer = new TcpWriter<(Command, string)>(pipeline, Configuration.CommandPort, commandFormat.GetFormat(), "Command");
            CommandEmitter.PipeTo(writer.In);
            AddProcess(new Rendezvous.Process($"{name}-{CommandProcessName}", [writer.ToRendezvousEndpoint(Configuration.RendezVousHost, "Command")]));
            rendezvousRelay.Rendezvous.ProcessAdded += AddedProcess;
            rendezvousRelay.Error += (s, e) => { log(e.Message); log(e.HResult.ToString()); };
            StartRendezVous();
            return true;
        }

        protected abstract void StartRendezVous();

        protected abstract void StopRendezVous();

        protected void AddedProcess(object? sender, Process process)
        {
            log($"Process {process.Name}");
            if (processNames.Contains(process.Name))
                return;
            if (process.Name.Contains(CommandProcessName))
            {
                if (!process.Name.Contains(name))
                    AddedCommandProcess(process);
                else
                    return;
            }
            else if (process.Name.Contains(DiagnosticsProcessName))
            {
                if (!process.Name.Contains(name))
                    AddedDiagnoticsProcess(process);
                else
                    return;
            } 
            else
                switch (process.Name)
                {
                    case ClockSynchProcessName:
                        if (Configuration.ClockPort == 0)
                            AddedClockProcess(process);
                        break;
                    default:
                        AddedDataProcess(process);
                        break;
                }
        }

        protected void AddedClockProcess(Process process)
        {
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockEndpoint) 
                { 
                    remoteClockEndpoint.ToRemoteClockImporter(pipeline);
                    return;
                }
            }
        }

        protected void AddedCommandProcess(Process process)
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
                            Subpipeline commandSubPipeline = new Subpipeline(pipeline, process.Name);
                            var tcpSource = source.ToTcpSource<(Command, string)>(commandSubPipeline, commandFormat.GetFormat(), null, true, stream.StreamName);
                            Helpers.PipeToMessage<(Command, string)> p2m = new Helpers.PipeToMessage<(Command, string)>(commandSubPipeline, Configuration.CommandDelegate, $"p2m-{process.Name}");
                            Microsoft.Psi.Operators.PipeTo(tcpSource.Out, p2m.In);
                            if (this.Configuration.AutomaticPipelineRun)
                            {
                                commandSubPipeline.RunAsync();
                                log($"SubPipeline {process.Name} started.");
                            }
                            return;
                        }
                    }
                }
            }
        }

        protected void AddedDiagnoticsProcess(Process process)
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
                            Subpipeline processSubPipeline = new Subpipeline(pipeline, process.Name);
                            Connection(stream.StreamName, DiagnosticsProcessName, CreateOrGetSession(Configuration.SessionName + "_Diagnostics"), source, processSubPipeline, true);
                            if (this.Configuration.AutomaticPipelineRun)
                            {
                                processSubPipeline.RunAsync();
                                log($"SubPipeline {process.Name} started.");
                            }
                            return;
                        }
                    }
                }
            }
        }

        protected void AddedDataProcess(Process process)
        {
            int elementAdded = 0;
            Subpipeline processSubPipeline = new Subpipeline(pipeline, process.Name);
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
                        log($"\tStream {stream.StreamName}");
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
                    {
                        log($"\tStream {stream.StreamName}");
                        elementAdded += Connection(stream.StreamName, process.Name, session, source, processSubPipeline, !this.Configuration.NotStoredTopics.Contains(stream.StreamName)) ? 1 : 0;
                    }
                }
            }
            log($"Process {process.Name} sources added : {elementAdded}");
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
                log($"SubPipeline {process.Name} started.");
                TriggerNewProcessEvent(process.Name);
            }
            Dataset?.Save();
        }

        protected void Connection<T>(string streamName, string processName, Session? session, TcpSourceEndpoint source, Pipeline p, bool storeSteam, Format<T> deserializer, Type? transformerType)
        {
            string storeName;
            switch (Configuration.StoreMode)
            {
                case StoreMode.Process:
                    storeName = processName;
                    break;
                case StoreMode.Dictionnary:
                    if (Configuration.StreamToStore.ContainsKey(streamName) && session != null)
                    {
                        storeName = Configuration.StreamToStore[streamName];
                        if (storeName.Contains("%p"))
                            storeName = storeName.Replace("%p", processName);
                        if (storeName.Contains("%s"))
                            storeName = storeName.Replace("%s", session.Name);
                        break;
                    }
                    goto default;
                default:
                case StoreMode.Independant:
                    storeName = $"{processName}-{streamName}";
                    break;
            }
            var tcpSource = source.ToTcpSource<T>(p, deserializer, null, true, storeName);
            if (Configuration.Debug)
                tcpSource.Do((d, e) => { log($"Recieve {storeName} data @{e} : {d}"); });
            if (transformerType != null)
            {
                dynamic transformer = Activator.CreateInstance(transformerType, [p, $"{storeName}_transformer"]);
                Microsoft.Psi.Operators.PipeTo(tcpSource.Out, transformer.In);
                if (transformerType.GetInterfaces().Union([typeof(IComplexTransformer)]).Count() > 0)
                    transformer.CreateConnectionsstreamName(streamName, storeName, session, p, storeSteam, this);
                else
                    CreateConnectorAndStore(streamName, storeName, session, p, transformer.Out.Type, transformer, storeSteam);
            }
            else
                CreateConnectorAndStore(streamName, storeName, session, p, typeof(T), tcpSource, storeSteam);
        }

        protected bool Connection(string streamName, string processName, Session? session, RemoteExporterEndpoint source, Pipeline p, bool storeSteam)
        {
            var importer = source.ToRemoteImporter(p);
            if (!importer.Connected.WaitOne())
            {
                log($"Failed to connect to {streamName}");
                return false;
            }
            foreach (var streamInfo in importer.Importer.AvailableStreams)
            {
                string storeName;
                switch (Configuration.StoreMode)
                {
                    case StoreMode.Process:
                        storeName = processName;
                        break;
                    case StoreMode.Dictionnary:
                        if (Configuration.StreamToStore.ContainsKey(streamName) && session != null)
                        {
                            storeName = Configuration.StreamToStore[streamName];
                            if (storeName.Contains("%p"))
                                storeName = storeName.Replace("%p", processName);
                            if (storeName.Contains("%s"))
                                storeName = storeName.Replace("%s", session.Name);
                            break;
                        }
                        goto default;
                    default:
                    case StoreMode.Independant:
                        storeName = $"{processName}-{streamName}";
                        break;
                }
                Type type = Type.GetType(streamInfo.TypeName);
                var stream = importer.Importer.OpenDynamicStream(streamInfo.Name);
                if (Configuration.Debug)
                    stream.Do((d, e) => { log($"Recieve {storeName}-{streamInfo.Name} data @{e} : {d}"); });
                CreateConnectorAndStore(streamInfo.Name, $"{storeName}-{streamInfo.Name}", session, p, type, stream, storeSteam);  
            } 
            return true;
        }
    }
}
