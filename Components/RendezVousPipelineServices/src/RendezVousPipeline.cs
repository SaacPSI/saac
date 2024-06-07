using Microsoft.Psi.Data;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Interop.Serialization;
using Microsoft.Psi.Remoting;
using Microsoft.Psi;
using static Microsoft.Psi.Interop.Rendezvous.Rendezvous;
using System.IO;
using static Microsoft.Psi.Calibration.LevenbergMarquardt;

// USING https://github.com/SaacPSI/psi/ branch 'Pipeline' version of Psi.Runtime package

namespace SAAC.RendezVousPipelineServices
{
    public class RendezVousPipeline
    {
        public Dataset Dataset { get; private set; }
        public Dictionary<string, Dictionary<string, ConnectorInfo>> Connectors { get; private set; }
        public Dictionary<string, Dictionary<string, PsiExporter>> Stores { get; private set; }
        public RendezVousPipelineConfiguration Configuration { get; private set; }
        public EventHandler<(string, Dictionary<string, Dictionary<string, ConnectorInfo>>)>? NewProcess;
        public delegate void LogStatus(string log);

        protected LogStatus log;
        protected Pipeline pipeline;

        private RendezvousServer server;
        private bool isStarted;
        private bool isPipelineRunning;
        private bool isClockServer;

        public RendezVousPipeline(RendezVousPipelineConfiguration? configuration, LogStatus? log = null)
        {
            this.Configuration = configuration ?? new RendezVousPipelineConfiguration();
            this.log = log ?? ((log) => { Console.WriteLine(log); });
            pipeline = Pipeline.Create(enableDiagnostics: this.Configuration.Diagnostics);
            server = new RendezvousServer(this.Configuration.RendezVousPort);
            Connectors = new Dictionary<string, Dictionary<string, ConnectorInfo>>();
            Stores = new Dictionary<string, Dictionary<string, PsiExporter>>();
            isClockServer = this.Configuration.ClockConfiguration != null && this.Configuration.ClockConfiguration.ClockPort != 0;
            if (this.Configuration.AutomaticPipelineRun && !isClockServer)
                throw new Exception("It is not possible to have AutomaticPipelineRun without ClockServer.");
            if (File.Exists(this.Configuration.DatasetPath + this.Configuration.DatasetName))
                Dataset = Dataset.Load(this.Configuration.DatasetPath + this.Configuration.DatasetName);
            else
                Dataset = new Dataset(this.Configuration.DatasetName, this.Configuration.DatasetPath + this.Configuration.DatasetName);
            isStarted = isPipelineRunning = false;
        }

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
            if(Configuration.Diagnostics)
                CreateStore(pipeline, Dataset.AddEmptySession(Configuration.SessionName + "_Diagnostics"), "Diagnostics", "Diagnostics", pipeline.Diagnostics);
            if (this.Configuration.AutomaticPipelineRun)
                RunPipeline();
            if (isClockServer)
            {
                var remoteClock = new RemoteClockExporter(Configuration.ClockConfiguration.ClockPort);
                server.Rendezvous.TryAddProcess(new Rendezvous.Process(Configuration.ClockConfiguration.ClockProcessName, new[] { remoteClock.ToRendezvousEndpoint(Configuration.RendezVousHost) }));
            }
            server.Rendezvous.ProcessAdded += AddedProcess;
            server.Error += (s, e) => { log(e.Message); log(e.HResult.ToString()); };
            server.Start();
            log("server started!");
            isStarted = true;
        }

        public void Stop()
        {
            if (!isStarted)
                return;
            server.Stop();
            if (isPipelineRunning)
                pipeline.Dispose();
            if (Dataset.HasUnsavedChanges)
                Dataset.Save();
            isStarted = isPipelineRunning = false;
        }

        public Session? GetSession(string sessionName)
        {
            foreach (var session in Dataset.Sessions)
                if (session != null && session.Name == sessionName)
                    return session;
            return null;
        }

        public Session CreateOrGetSession(string sessionName)
        {
            foreach (var session in Dataset.Sessions)
                if (session != null && session.Name == sessionName)
                    return session;
            return Dataset.AddEmptySession(sessionName);
        }

        public void CreateConnectorAndStore<T>(string name, string storeName, Session session, Pipeline p, Type type, IProducer<T> stream, bool storeSteam)
        {
            if (!Connectors.ContainsKey(session.Name))
                Connectors.Add(session.Name, new Dictionary<string, ConnectorInfo>());
            Connectors[session.Name].Add(name, new ConnectorInfo(name, session.Name, type, stream));
            if (storeSteam)
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

        private void AddedProcess(object? sender, Process process)
        {
            log($"Process {process.Name}");
            if (isClockServer && process.Name == Configuration.ClockConfiguration?.ClockProcessName)
                return;
            int elementAdded=0;
            Subpipeline processSubPipeline = new Subpipeline(pipeline, process.Name);
            Session session;
            if (Configuration.UniqueSession)
                session = CreateOrGetSession(Configuration.SessionName);
            else
                session = CreateOrGetSession(Configuration.SessionName + process.Name);
            foreach (var endpoint in process.Endpoints)
            {
                if (isClockServer == false && process.Name == Configuration.ClockConfiguration?.ClockProcessName && endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockEndpoint)
                {
                    var remoteClockImporter = remoteClockEndpoint.ToRemoteClockImporter(pipeline);
                }
                if (endpoint is Rendezvous.TcpSourceEndpoint)
                {
                    TcpSourceEndpoint? source = endpoint as TcpSourceEndpoint;
                    if (source == null)
                        continue;
                    foreach (var stream in endpoint.Streams)
                    {
                        log($"\tStream {stream.StreamName}");
                        string streamName;
                        if (Configuration.UniqueSession)
                            streamName = $"{process.Name}-{stream.StreamName}";
                        else
                            streamName = stream.StreamName;
                        if (Configuration.TopicsTypes.ContainsKey(stream.StreamName))
                        {
                            Type type = Configuration.TopicsTypes[stream.StreamName];
                            if (!Configuration.TypesSerializers.ContainsKey(type))
                                throw new Exception($"Missing serializer of type {type} in configuration.");
                            Connection(streamName, session, source, processSubPipeline, !this.Configuration.NotStoredTopics.Contains(stream.StreamName), Configuration.TypesSerializers[type].GetFormat(), Configuration.Transformers.ContainsKey(stream.StreamName) ? Configuration.Transformers[stream.StreamName] : null);
                            elementAdded++;
                        }
                    }
                }
                else if (endpoint is Rendezvous.RemoteExporterEndpoint)
                {
                    RemoteExporterEndpoint? source = endpoint as RemoteExporterEndpoint;
                    if (source == null)
                        continue;
                    foreach (var stream in endpoint.Streams)
                    {
                        log($"\tStream {stream.StreamName}");
                        string streamName;
                        if (Configuration.UniqueSession)
                            streamName = $"{process.Name}-{stream.StreamName}";
                        else
                            streamName = stream.StreamName;
                        elementAdded += Connection(streamName, session, source, processSubPipeline, !this.Configuration.NotStoredTopics.Contains(stream.StreamName)) ? 1 : 0;
                    }
                }
            }
            log($"Process {process.Name} sources added : {elementAdded}");
            if (elementAdded == 0)
            {
                processSubPipeline.Dispose();
                if(session.Partitions.Count() == 0)
                    Dataset.RemoveSession(session);
                return;
            }
            if (this.Configuration.AutomaticPipelineRun)
            {
                processSubPipeline.RunAsync();
                log($"SubPipeline {process.Name} started.");
            }
            Dataset.Save();
            TriggerNewProcessEvent(process.Name);
        }

        private void Connection<T>(string name, Session session, TcpSourceEndpoint source, Pipeline p, bool storeSteam, Format<T> deserializer, Type? transformerType)
        {
            string sourceName = $"{session.Name}-{name}";
            var tcpSource = source.ToTcpSource<T>(p, deserializer, null, true, sourceName);
            if (Configuration.Debug)
                tcpSource.Do((d, e) => { log($"Recieve {sourceName} data @{e} : {d}"); });
            if (!Connectors.ContainsKey(session.Name))
                Connectors.Add(session.Name, new Dictionary<string, ConnectorInfo>());
            if (transformerType != null)
            {
                dynamic transformer = Activator.CreateInstance(transformerType, [p, $"{sourceName}_transformer" ]);
                Microsoft.Psi.Operators.PipeTo(tcpSource.Out, transformer.In);
                CreateConnectorAndStore(name, sourceName, session, p, transformer.Out.Type, transformer, storeSteam);
            }
            else
                CreateConnectorAndStore(name, sourceName, session, p, typeof(T), tcpSource, storeSteam);
        }

        private bool Connection(string name, Session session, RemoteExporterEndpoint source, Pipeline p, bool storeSteam)
        {
            string sourceName = $"{session.Name}-{name}";
            var importer = source.ToRemoteImporter(p);
            if (!importer.Connected.WaitOne())
            {
                log($"Failed to connect to {sourceName}");
                return false;
            }
            foreach (var streamInfo in importer.Importer.AvailableStreams)
            {
                Type type = Type.GetType(streamInfo.TypeName);
                var stream = importer.Importer.OpenDynamicStream(streamInfo.Name);
                if (Configuration.Debug)
                    stream.Do((d, e) => { log($"Recieve {sourceName}-{streamInfo.Name} data @{e} : {d}"); });
                CreateConnectorAndStore(streamInfo.Name, $"{sourceName}-{streamInfo.Name}", session, p, type, stream, storeSteam);  
            } 
            return true;
        }
    }
}
