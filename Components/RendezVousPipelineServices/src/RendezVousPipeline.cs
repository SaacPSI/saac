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
        public Dictionary<string, ConnectorInfo> Connectors { get; private set; }
        public EventHandler<(string, Dictionary<string, ConnectorInfo>)>? NewProcess;
        public delegate void LogStatus(string log);

        protected LogStatus log;
        protected Pipeline pipeline;
        protected RendezVousPipelineConfiguration configuration;
        private RendezvousServer server;
        private bool isStarted;
        private bool isPipelineRunning;

        public RendezVousPipeline(RendezVousPipelineConfiguration? configuration, LogStatus? log = null)
        {
            this.configuration = configuration ?? new RendezVousPipelineConfiguration();
            this.log = log ?? ((log) => { Console.WriteLine(log); });
            pipeline = Pipeline.Create(enableDiagnostics: this.configuration.Diagnostics);
            server = new RendezvousServer(this.configuration.RendezVousPort);
            Connectors = new Dictionary<string, ConnectorInfo>();
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
            var remoteClock = new RemoteClockExporter(configuration.ClockPort);
            server.Rendezvous.TryAddProcess(new Rendezvous.Process(configuration.ClockProcessName, new[] { remoteClock.ToRendezvousEndpoint(configuration.RendezVousHost) }));
            server.Rendezvous.ProcessAdded += AddedProcess;
            server.Error += (s, e) => { log(e.Message); log(e.HResult.ToString()); };
            server.Start();
            log("server started!");
            isStarted = true;
            pipeline.RunAsync();
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
            Subpipeline questSubPipeline = new Subpipeline(pipeline, process.Name);
            Session session = Dataset.AddEmptySession(configuration.SessionName + process.Name);
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint)
                {
                    TcpSourceEndpoint? source = endpoint as TcpSourceEndpoint;
                    if (source == null)
                        return;
                    foreach (var stream in endpoint.Streams)
                    {
                        log($"\tStream {stream.StreamName}");
                        if (configuration.TopicsTypes.ContainsKey(stream.StreamName))
                        {
                            Type type = configuration.TopicsTypes[stream.StreamName];
                            if (!configuration.TypesSerializers.ContainsKey(type))
                                throw new Exception($"Missing serializer of type {type} in configuration.");
                            Connection(stream.StreamName, session, source, questSubPipeline, configuration.TypesSerializers[type].GetFormat());
                        }
                    }
                }
            }
            questSubPipeline.RunAsync();
            Dataset.Save();
            NewProcess?.Invoke(this, (process.Name, Connectors));
        }

        private void Connection<T>(string name, Session session, TcpSourceEndpoint source, Pipeline p, Format<T> deserializer)
        {
            string sourceName = $"{session.Name}-{name}";
            var tcpSource = source.ToTcpSource<T>(p, deserializer, null, true, sourceName);
            if(configuration.Debug)
                tcpSource.Do((d, e) => { log($"Recieve {sourceName} data @{e} : {d}"); });
            Connectors.Add(sourceName, new ConnectorInfo(name, session.Name, typeof(T), tcpSource));
            CreateStore(p, session, name, tcpSource);
        }

        private void CreateStore<T>(Pipeline pipeline, Session session, string name, IProducer<T> source)
        {
            var store = PsiStore.Create(pipeline, name, $"{configuration.DatasetPath}/{session.Name}/");
            store.Write(source, name);
            session.AddPartitionFromPsiStoreAsync(name, $"{configuration.DatasetPath}/{session.Name}/");
        }
    }
}
