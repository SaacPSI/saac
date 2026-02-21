// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Remoting;
    using SAAC.Nuitrack;
    using SAAC.PipelineServices;

    /// <summary>
    /// Component for streaming Nuitrack sensor data over a rendezvous-based network.
    /// </summary>
    public class NuitrackRemoteStreamsComponent
    {
        private readonly RendezVousPipeline server;
        private readonly string name;
        private Pipeline pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="NuitrackRemoteStreamsComponent"/> class.
        /// </summary>
        /// <param name="server">The rendezvous pipeline server.</param>
        /// <param name="configuration">Optional configuration for the Nuitrack streams.</param>
        /// <param name="localStorage">Whether to store data locally.</param>
        /// <param name="name">The name of the component.</param>
        public NuitrackRemoteStreamsComponent(RendezVousPipeline server, NuitrackRemoteStreamsConfiguration? configuration = null, bool localStorage = true, string name = nameof(NuitrackRemoteStreamsComponent))
        {
            this.server = server;
            this.name = name;
            this.LocalStorage = localStorage;
            this.Configuration = configuration ?? new NuitrackRemoteStreamsConfiguration();
        }

        /// <summary>
        /// Gets the configuration for the Nuitrack remote streams.
        /// </summary>
        public NuitrackRemoteStreamsConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets a value indicating whether data should be stored locally.
        /// </summary>
        public bool LocalStorage { get; private set; }

        /// <summary>
        /// Gets the Nuitrack sensor instance.
        /// </summary>
        public NuitrackSensor? Sensor { get; private set; }

        /// <summary>
        /// Generates a rendezvous process with configured stream exporters.
        /// Creates remote exporters for skeleton tracking, color image, depth image, hand tracking, user tracking, and gesture recognition based on configuration.
        /// </summary>
        /// <returns>A configured rendezvous process with all enabled stream endpoints.</returns>
        public Rendezvous.Process GenerateProcess()
        {
            int portCount = this.Configuration.StartingPort + 1;
            this.pipeline = this.server.GetOrCreateSubpipeline(this.name);
            this.Sensor = new NuitrackSensor(this.pipeline, this.Configuration);
            var session = this.server.CreateOrGetSessionFromMode(this.Configuration.RendezVousApplicationName);
            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();
            if (this.Configuration.OutputSkeletonTracking == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_Bodies";
                RemoteExporter skeletonExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                skeletonExporter.Exporter.Write(this.Sensor.OutBodies, streamName);
                exporters.Add(skeletonExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, this.Sensor.OutBodies.GetType(), this.Sensor.OutBodies, this.LocalStorage);
            }

            if (this.Configuration.OutputColor == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_RGB";
                RemoteExporter imageExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                var compressed = this.Sensor.OutColorImage.EncodeJpeg(this.Configuration.EncodingVideoLevel);
                imageExporter.Exporter.Write(compressed, streamName);
                exporters.Add(imageExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, compressed.GetType(), compressed, this.LocalStorage);
            }

            if (this.Configuration.OutputDepth == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_Depth";
                RemoteExporter depthExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                var compressed = this.Sensor.OutDepthImage.EncodePng();
                depthExporter.Exporter.Write(compressed, streamName);
                exporters.Add(depthExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, compressed.GetType(), compressed, this.LocalStorage);
            }

            if (this.Configuration.OutputHandTracking == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_Hands";
                RemoteExporter handsExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                handsExporter.Exporter.Write(this.Sensor.OutHands, streamName);
                exporters.Add(handsExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, this.Sensor.OutHands.GetType(), this.Sensor.OutHands, this.LocalStorage);
            }

            if (this.Configuration.OutputUserTracking == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_Users";
                RemoteExporter usersExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                usersExporter.Exporter.Write(this.Sensor.OutUsers, streamName);
                exporters.Add(usersExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, this.Sensor.OutUsers.GetType(), this.Sensor.OutUsers, this.LocalStorage);
            }

            if (this.Configuration.OutputGestureRecognizer == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_Gestures";
                RemoteExporter gesturesExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                gesturesExporter.Exporter.Write(this.Sensor.OutGestures, streamName);
                exporters.Add(gesturesExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, this.Sensor.OutGestures.GetType(), this.Sensor.OutGestures, this.LocalStorage);
            }

            return new Rendezvous.Process(this.Configuration.RendezVousApplicationName, exporters, "Version1.0");
        }

        /// <summary>
        /// Starts the pipeline asynchronously.
        /// </summary>
        public void RunAsync()
        {
            this.pipeline.RunAsync();
        }

        /// <summary>
        /// Disposes the pipeline and releases all resources.
        /// </summary>
        public void Dispose()
        {
            this.pipeline.Dispose();
        }
    }
}
