using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Remoting;
using SAAC.RemoteConnectors;
using SAAC.RendezVousPipelineServices;

namespace SAAC.KinectAzureRemoteServices
{
    public class KinectAzureRemoteComponent : KinectAzureRemoteConnector
    {
        protected RendezVousPipeline server;

        public KinectAzureRemoteComponent(RendezVousPipeline server, Pipeline parent, KinectAzureRemoteConnectorConfiguration? configuration = null, string name = nameof(KinectAzureRemoteComponent), LogStatus? log = null)
            : base(parent, configuration, name, log)
        {
            this.server = server;
            this.server.AddConnectingProcess(Configuration.RendezVousApplicationName, GenerateProcess());
        }

        protected override Emitter<T>? Connection<T>(string name, RemoteImporter remoteImporter)
        {
            Emitter<T>? stream = base.Connection<T>(name, remoteImporter);
            if(stream != null)
            {
                Session? session = server.CreateOrGetSessionFromMode(Configuration.RendezVousApplicationName);
                var storeName = server.GetStoreName(name, Configuration.RendezVousApplicationName, session);
                server.CreateConnectorAndStore(storeName.Item1, storeName.Item2, session, base.pipeline, stream.Type, stream, !server.Configuration.NotStoredTopics.Contains(name));
            }
            return stream;
        }
    }
}
