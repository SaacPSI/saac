// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Remoting;
    using SAAC.PipelineServices;

    /// <summary>
    /// Component that connects to a remote Kinect Azure device via rendezvous and stores data locally.
    /// Extends KinectAzureRemoteConnector with automatic storage capabilities.
    /// </summary>
    public class KinectAzureRemoteComponent : KinectAzureRemoteConnector
    {
        private readonly RendezVousPipeline server;
        private Session? session;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectAzureRemoteComponent"/> class.
        /// </summary>
        /// <param name="server">The rendezvous pipeline server.</param>
        /// <param name="configuration">Optional configuration for the remote connection.</param>
        /// <param name="name">The name of the component.</param>
        public KinectAzureRemoteComponent(RendezVousPipeline server, KinectAzureRemoteConnectorConfiguration? configuration = null, string name = nameof(KinectAzureRemoteComponent))
            : base(null, configuration, name, server.Log)
        {
            this.server = server;
            this.server.AddConnectingProcess(this.Configuration.RendezVousApplicationName, this.GenerateProcess());
        }

        /// <summary>
        /// Establishes a connection to a remote stream and optionally stores it locally.
        /// </summary>
        /// <typeparam name="T">The type of data in the stream.</typeparam>
        /// <param name="name">The name of the stream.</param>
        /// <param name="remoteImporter">The remote importer for the stream.</param>
        /// <returns>The emitter for the connected stream, or null if connection failed.</returns>
        protected override Emitter<T>? Connection<T>(string name, RemoteImporter remoteImporter)
        {
            Emitter<T>? stream = base.Connection<T>(name, remoteImporter);
            if (stream != null)
            {
                var storeName = this.server.GetStoreName(name, this.Configuration.RendezVousApplicationName, this.session);
                this.server.CreateConnectorAndStore(storeName.Item1, storeName.Item2, this.session, this.pipeline, stream.Type, stream, !this.server.Configuration.NotStoredTopics.Contains(name));
            }

            return stream;
        }

        /// <summary>
        /// Processes a rendezvous process event, creating a subpipeline and session for data storage.
        /// </summary>
        /// <param name="p">The rendezvous process to handle.</param>
        protected override void Process(Rendezvous.Process p)
        {
            if (p.Name == this.Configuration.RendezVousApplicationName)
            {
                this.session = this.server.CreateOrGetSessionFromMode(this.Configuration.RendezVousApplicationName);
                this.pipeline = this.server.GetOrCreateSubpipeline(p.Name);
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
