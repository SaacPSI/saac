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

    /// <summary>
    /// Component for streaming Nuitrack sensor data over a rendezvous-based network without local storage.
    /// </summary>
    public class NuitrackRemoteStreams
    {
        private readonly Pipeline parentPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="NuitrackRemoteStreams"/> class.
        /// </summary>
        /// <param name="pipeline">The parent pipeline to use for streaming.</param>
        /// <param name="configuration">Optional configuration for the Nuitrack streams.</param>
        /// <param name="name">The name of the component.</param>
        public NuitrackRemoteStreams(Pipeline pipeline, NuitrackRemoteStreamsConfiguration? configuration = null, string name = nameof(NuitrackRemoteStreams))
        {
            this.parentPipeline = pipeline;
            this.Configuration = configuration ?? new NuitrackRemoteStreamsConfiguration();
        }

        /// <summary>
        /// Gets the configuration for the Nuitrack remote streams.
        /// </summary>
        public NuitrackRemoteStreamsConfiguration Configuration { get; private set; }

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

            this.Sensor = new NuitrackSensor(this.parentPipeline, this.Configuration);

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();
            if (this.Configuration.OutputSkeletonTracking == true)
            {
                RemoteExporter skeletonExporter = new RemoteExporter(this.parentPipeline, portCount++, this.Configuration.ConnectionType);
                skeletonExporter.Exporter.Write(this.Sensor.OutBodies, $"{this.Configuration.RendezVousApplicationName}_Bodies");
                exporters.Add(skeletonExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
            }

            if (this.Configuration.OutputColor == true)
            {
                RemoteExporter imageExporter = new RemoteExporter(this.parentPipeline, portCount++, this.Configuration.ConnectionType);
                imageExporter.Exporter.Write(this.Sensor.OutColorImage.EncodeJpeg(this.Configuration.EncodingVideoLevel), $"{this.Configuration.RendezVousApplicationName}_RGB");
                exporters.Add(imageExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
            }

            if (this.Configuration.OutputDepth == true)
            {
                RemoteExporter depthExporter = new RemoteExporter(this.parentPipeline, portCount++, this.Configuration.ConnectionType);
                depthExporter.Exporter.Write(this.Sensor.OutDepthImage.EncodePng(), $"{this.Configuration.RendezVousApplicationName}_Depth");
                exporters.Add(depthExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
            }

            if (this.Configuration.OutputHandTracking == true)
            {
                RemoteExporter handsExporter = new RemoteExporter(this.parentPipeline, portCount++, this.Configuration.ConnectionType);
                handsExporter.Exporter.Write(this.Sensor.OutHands, $"{this.Configuration.RendezVousApplicationName}_Hands");
                exporters.Add(handsExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
            }

            if (this.Configuration.OutputUserTracking == true)
            {
                RemoteExporter usersExporter = new RemoteExporter(this.parentPipeline, portCount++, this.Configuration.ConnectionType);
                usersExporter.Exporter.Write(this.Sensor.OutUsers, $"{this.Configuration.RendezVousApplicationName}_Users");
                exporters.Add(usersExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
            }

            if (this.Configuration.OutputGestureRecognizer == true)
            {
                RemoteExporter gesturesExporter = new RemoteExporter(this.parentPipeline, portCount++, this.Configuration.ConnectionType);
                gesturesExporter.Exporter.Write(this.Sensor.OutGestures, $"{this.Configuration.RendezVousApplicationName}_Gestures");
                exporters.Add(gesturesExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
            }

            return new Rendezvous.Process(this.Configuration.RendezVousApplicationName, exporters, "Version1.0");
        }

        /// <summary>
        /// Starts the parent pipeline asynchronously.
        /// </summary>
        public void RunAsync()
        {
            this.parentPipeline.RunAsync();
        }

        /// <summary>
        /// Disposes the parent pipeline and releases all resources.
        /// </summary>
        public void Dispose()
        {
            this.parentPipeline.Dispose();
        }
    }
}
