using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Remoting;
using SAAC.RemoteConnectors;
using SAAC.PipelineServices;
using Microsoft.Psi.Interop.Rendezvous;

namespace SAAC.KinectAzureRemoteServices
{
    public class KinectAzureRemoteComponent : KinectAzureRemoteConnector
    {
        protected RendezVousPipeline server;
        protected Session? session;

        public KinectAzureRemoteComponent(RendezVousPipeline server, KinectAzureRemoteConnectorConfiguration? configuration = null, string name = nameof(KinectAzureRemoteComponent))
            : base(null, configuration, name, server.Log)
        {
            this.server = server;
            this.server.AddConnectingProcess(Configuration.RendezVousApplicationName, GenerateProcess());
        }

        protected override Emitter<T>? Connection<T>(string name, RemoteImporter remoteImporter)
        {
            Emitter<T>? stream = base.Connection<T>(name, remoteImporter);
            if (stream != null)
            {
                var storeName = server.GetStoreName(name, Configuration.RendezVousApplicationName, session);
                server.CreateConnectorAndStore(storeName.Item1, storeName.Item2, session, base.pipeline, stream.Type, stream, !server.Configuration.NotStoredTopics.Contains(name));
            }
            return stream;
        }

        protected override void Process(Rendezvous.Process p)
        {
            if (p.Name == Configuration.RendezVousApplicationName)
            {
                session = server.CreateOrGetSessionFromMode(Configuration.RendezVousApplicationName);
                this.pipeline = this.server.CreateSubpipeline(p.Name);
                base.Process(p);
                if (this.server.Configuration.AutomaticPipelineRun)
                {
                    this.pipeline.RunAsync();
                    this.server.Log($"SubPipeline {p.Name} started.");
                    this.server.TriggerNewProcessEvent(p.Name);
                }
            }
        }
    }
}
