// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Remoting;
    using SAAC.PipelineServices;

    /// <summary>
    /// Component for streaming Kinect sensor data over a rendezvous-based network.
    /// </summary>
    public class KinectRemoteStreamsComponent
    {
        private readonly RendezVousPipeline server;
        private readonly string name;
        private Pipeline pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectRemoteStreamsComponent"/> class.
        /// </summary>
        /// <param name="server">The rendezvous pipeline server.</param>
        /// <param name="configuration">Optional configuration for the Kinect streams.</param>
        /// <param name="localStorage">Whether to store data locally.</param>
        /// <param name="name">The name of the component.</param>
        public KinectRemoteStreamsComponent(RendezVousPipeline server, KinectRemoteStreamsComponentConfiguration? configuration = null, bool localStorage = true, string name = nameof(KinectRemoteStreamsComponent))
        {
            this.server = server;
            this.name = name;
            this.LocalStorage = localStorage;
            this.Configuration = configuration ?? new KinectRemoteStreamsComponentConfiguration();
        }

        /// <summary>
        /// Gets the configuration for the Kinect remote streams.
        /// </summary>
        public KinectRemoteStreamsComponentConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets a value indicating whether data should be stored locally.
        /// </summary>
        public bool LocalStorage { get; private set; }

        /// <summary>
        /// Gets the Kinect sensor instance.
        /// </summary>
        public KinectSensor? Sensor { get; private set; }

        /// <summary>
        /// Generates a rendezvous process with configured stream exporters.
        /// Creates remote exporters for audio, bodies, color, RGBD, depth, infrared, long exposure infrared, color-to-camera mapping, and calibration based on configuration.
        /// </summary>
        /// <returns>A configured rendezvous process with all enabled stream endpoints.</returns>
        public Rendezvous.Process GenerateProcess()
        {
            int portCount = this.Configuration.StartingPort + 1;
            this.pipeline = this.server.GetOrCreateSubpipeline(this.name);
            this.Sensor = new KinectSensor(this.pipeline, this.Configuration);
            var session = this.server.CreateOrGetSessionFromMode(this.Configuration.RendezVousApplicationName);

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();
            if (this.Configuration.OutputAudio == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_Audio";
                RemoteExporter soundExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                soundExporter.Exporter.Write(this.Sensor.Audio, streamName);
                exporters.Add(soundExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, this.Sensor.Audio.GetType(), this.Sensor.Audio, this.LocalStorage);
            }

            if (this.Configuration.OutputBodies == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_Bodies";
                RemoteExporter skeletonExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                skeletonExporter.Exporter.Write(this.Sensor.Bodies, streamName);
                exporters.Add(skeletonExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, this.Sensor.Bodies.GetType(), this.Sensor.Bodies, this.LocalStorage);
            }

            if (this.Configuration.OutputColor == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_RGB";
                RemoteExporter imageExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                var compressed = this.Sensor.ColorImage.EncodeJpeg(this.Configuration.EncodingVideoLevel);
                imageExporter.Exporter.Write(compressed, streamName);
                exporters.Add(imageExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, compressed.GetType(), compressed.Out, this.LocalStorage);
            }

            if (this.Configuration.OutputRGBD == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_RGBD";
                RemoteExporter imageExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                var compressed = this.Sensor.RGBDImage.EncodeJpeg(this.Configuration.EncodingVideoLevel);
                imageExporter.Exporter.Write(compressed, streamName);
                exporters.Add(imageExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, compressed.GetType(), compressed.Out, this.LocalStorage);
            }

            if (this.Configuration.OutputDepth == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_Depth";
                RemoteExporter depthExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                var compressed = this.Sensor.DepthImage.EncodePng();
                depthExporter.Exporter.Write(compressed, streamName);
                exporters.Add(depthExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, compressed.GetType(), compressed.Out, this.LocalStorage);
            }

            if (this.Configuration.OutputInfrared == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_Infrared";
                RemoteExporter depthExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                var compressed = this.Sensor.InfraredImage.EncodeJpeg(this.Configuration.EncodingVideoLevel);
                depthExporter.Exporter.Write(compressed, streamName);
                exporters.Add(depthExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, compressed.GetType(), compressed.Out, this.LocalStorage);
            }

            if (this.Configuration.OutputLongExposureInfrared == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_LongExposureInfrared";
                RemoteExporter depthExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                var compressed = this.Sensor.LongExposureInfraredImage.EncodeJpeg(this.Configuration.EncodingVideoLevel);
                depthExporter.Exporter.Write(compressed, streamName);
                exporters.Add(depthExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, compressed.GetType(), compressed.Out, this.LocalStorage);
            }

            if (this.Configuration.OutputColorToCameraMapping == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_ColorToCameraMapper";
                RemoteExporter depthCalibrationExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                depthCalibrationExporter.Exporter.Write(this.Sensor.ColorToCameraMapper, streamName);
                exporters.Add(depthCalibrationExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, this.Sensor.ColorToCameraMapper.GetType(), this.Sensor.ColorToCameraMapper, this.LocalStorage);
            }

            if (this.Configuration.OutputCalibration == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_Calibration";
                RemoteExporter imuExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                imuExporter.Exporter.Write(this.Sensor.DepthDeviceCalibrationInfo, streamName);
                exporters.Add(imuExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, this.Sensor.DepthDeviceCalibrationInfo.GetType(), this.Sensor.DepthDeviceCalibrationInfo, this.LocalStorage);
            }

            this.server.TriggerNewProcessEvent(this.Configuration.RendezVousApplicationName);
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
