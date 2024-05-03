using Microsoft.Psi.Data;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Interop.Serialization;
using Microsoft.Psi.Remoting;
using Microsoft.Psi;
using static Microsoft.Psi.Interop.Rendezvous.Rendezvous;
using System.IO;

// USING https://github.com/SaacPSI/psi/ branch 'Pipeline' version of Psi.Runtime package

namespace SAAC.RendezVousPipelineServices
{
    public class RendezVousPipeline
    {
        public Dataset Dataset { get; private set; }
        public Dictionary<string, Dictionary<string, ConnectorInfo>> Connectors { get; private set; }
        public EventHandler<(string, Dictionary<string, Dictionary<string, ConnectorInfo>>)>? NewProcess;
        public delegate void LogStatus(string log);

        protected LogStatus log;
        protected Pipeline pipeline;
        protected RendezVousPipelineConfiguration configuration;
        private RendezvousServer server;
        private bool isStarted;
        private bool isPipelineRunning;
        private bool isClockServer;

        public RendezVousPipeline(RendezVousPipelineConfiguration? configuration, LogStatus? log = null)
        {
            this.configuration = configuration ?? new RendezVousPipelineConfiguration();
            this.log = log ?? ((log) => { Console.WriteLine(log); });
            pipeline = Pipeline.Create(enableDiagnostics: this.configuration.Diagnostics);
            server = new RendezvousServer(this.configuration.RendezVousPort);
            Connectors = new Dictionary<string, Dictionary<string, ConnectorInfo>>();
            isClockServer = this.configuration.ClockConfiguration != null && this.configuration.ClockConfiguration.ClockPort != 0;
            if (this.configuration.AutomaticPipelineRun && !isClockServer)
                throw new Exception("It is not possible to have AutomaticPipelineRun without ClockServer.");
            if (File.Exists(this.configuration.DatasetPath + this.configuration.DatasetName))
                Dataset = Dataset.Load(this.configuration.DatasetPath + this.configuration.DatasetName);
            else
                Dataset = new Dataset(this.configuration.DatasetName, this.configuration.DatasetPath + this.configuration.DatasetName);
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
            if(configuration.Diagnostics)
                CreateStore(pipeline, Dataset.AddEmptySession(configuration.SessionName + "_Diagnostics"), "Diagnostics", pipeline.Diagnostics);
            if (this.configuration.AutomaticPipelineRun)
                RunPipeline();
            if (isClockServer)
            {
                var remoteClock = new RemoteClockExporter(configuration.ClockConfiguration.ClockPort);
                server.Rendezvous.TryAddProcess(new Rendezvous.Process(configuration.ClockConfiguration.ClockProcessName, new[] { remoteClock.ToRendezvousEndpoint(configuration.RendezVousHost) }));
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

        public Subpipeline CreateSubpipeline(string name = "SaaCSubpipeline")
        {
            return new Subpipeline(pipeline, name);
        }

        private void AddedProcess(object? sender, Process process)
        {
            log($"Process {process.Name}");
            if (isClockServer && process.Name == configuration.ClockConfiguration?.ClockProcessName)
                return;
            int elementAdded=0;
            Subpipeline processSubPipeline = new Subpipeline(pipeline, process.Name);
            Session session;
            if (configuration.UniqueSession)
                session = CreateNewSession(configuration.SessionName);
            else
                session = CreateNewSession(configuration.SessionName + process.Name);
            foreach (var endpoint in process.Endpoints)
            {
                if (isClockServer == false && process.Name == configuration.ClockConfiguration?.ClockProcessName && endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockEndpoint)
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
                        if (configuration.UniqueSession)
                            streamName = $"{process.Name}-{stream.StreamName}";
                        else
                            streamName = stream.StreamName;
                        if (configuration.TopicsTypes.ContainsKey(stream.StreamName))
                        {
                            Type type = configuration.TopicsTypes[stream.StreamName];
                            if (!configuration.TypesSerializers.ContainsKey(type))
                                throw new Exception($"Missing serializer of type {type} in configuration.");
                            Connection(streamName, session, source, processSubPipeline, configuration.TypesSerializers[type].GetFormat());
                            elementAdded++;
                        }
                    }
                }
            }
            log($"Process {process.Name} sources added : {elementAdded}");
            if (elementAdded == 0)
            {
                processSubPipeline.Dispose();
                Dataset.RemoveSession(session);
                return;
            }
            if (this.configuration.AutomaticPipelineRun)
            {
                processSubPipeline.RunAsync();
                log($"SubPipeline {process.Name} started.");
            }
            Dataset.Save();
            NewProcess?.Invoke(this, (process.Name, Connectors));
        }

        private void Connection<T>(string name, Session session, TcpSourceEndpoint source, Pipeline p, Format<T> deserializer)
        {
            string sourceName = $"{session.Name}-{name}";
            var tcpSource = source.ToTcpSource<T>(p, deserializer, null, true, sourceName);
            if(configuration.Debug)
                tcpSource.Do((d, e) => { log($"Recieve {sourceName} data @{e} : {d}"); });
            if (!Connectors.ContainsKey(session.Name))
                Connectors.Add(session.Name, new Dictionary<string, ConnectorInfo>());
            Connectors[session.Name].Add(name, new ConnectorInfo(name, session.Name, typeof(T), tcpSource));
            CreateStore(p, session, name, tcpSource);
        }

        private void CreateStore<T>(Pipeline pipeline, Session session, string name, IProducer<T> source)
        {
            var store = PsiStore.Create(pipeline, name, $"{configuration.DatasetPath}/{session.Name}/");
            store.Write(source, name);
            session.AddPartitionFromPsiStoreAsync(name, $"{configuration.DatasetPath}/{session.Name}/");
        }

        private Session CreateNewSession(string sessionName)
        {
            foreach (var session in Dataset.Sessions)
                if (session != null && session.Name == sessionName)
                    return session;
            return Dataset.AddEmptySession(sessionName);
        }
    }
}
