// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Remoting;
    using SAAC.PipelineServices;

    /// <summary>
    /// Component for streaming Kinect Azure sensor data over a rendezvous pipeline.
    /// </summary>
    public class KinectAzureStreamsComponent
    {
        private readonly RendezVousPipeline server;
        private readonly string name;
        private Pipeline? pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectAzureStreamsComponent"/> class.
        /// </summary>
        /// <param name="server">The rendezvous pipeline server.</param>
        /// <param name="configuration">The configuration for the Kinect Azure streams.</param>
        /// <param name="localStorage">Indicates whether to use local storage.</param>
        /// <param name="name">The name of the component.</param>
        public KinectAzureStreamsComponent(RendezVousPipeline server, KinectAzureRemoteStreamsConfiguration? configuration = null, bool localStorage = true, string name = nameof(KinectAzureStreamsComponent))
        {
            this.server = server;
            this.name = name;
            this.LocalStorage = localStorage;
            this.Configuration = configuration ?? new KinectAzureRemoteStreamsConfiguration();
        }

        /// <summary>
        /// Gets the configuration for the Kinect Azure streams.
        /// </summary>
        public KinectAzureRemoteStreamsConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets a value indicating whether local storage is enabled.
        /// </summary>
        public bool LocalStorage { get; private set; }

        /// <summary>
        /// Gets the Azure Kinect sensor instance.
        /// </summary>
        public AzureKinectSensor? Sensor { get; private set; }

        /// <summary>
        /// Generates the rendezvous process for the Kinect Azure streams.
        /// </summary>
        /// <returns>The rendezvous process containing all configured stream endpoints.</returns>
        public Rendezvous.Process GenerateProcess()
        {
            int portCount = this.Configuration.StartingPort + 1;

            if (this.Configuration.OutputBodies == true)
            {
                this.Configuration.OutputDepth = this.Configuration.OutputInfrared = this.Configuration.OutputCalibration = true;
                this.Configuration.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            }

            this.pipeline = this.server.GetOrCreateSubpipeline(this.name);
            this.Sensor = new AzureKinectSensor(this.pipeline, this.Configuration);
            var session = this.server.CreateOrGetSessionFromMode(this.Configuration.RendezVousApplicationName);

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();
            if (this.Configuration.OutputAudio == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_Audio";
                AudioCaptureConfiguration audioCaptureConfig = new AudioCaptureConfiguration();
                int index = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevices().ToList().FindIndex(value => { return value.Contains("Azure"); });
                audioCaptureConfig.DeviceName = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevices().ElementAt(index);
                AudioCapture audioCapture = new AudioCapture(this.pipeline, audioCaptureConfig);
                RemoteExporter soundExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                soundExporter.Exporter.Write(audioCapture.Out, streamName);
                exporters.Add(soundExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, audioCapture.GetType(), audioCapture.Out, this.LocalStorage);
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

            if (this.Configuration.OutputInfrared == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_Infrared";
                RemoteExporter imageExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                var compressed = this.Sensor.InfraredImage.EncodeJpeg(this.Configuration.EncodingVideoLevel);
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

            if (this.Configuration.OutputCalibration == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_Calibration";
                RemoteExporter depthCalibrationExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                depthCalibrationExporter.Exporter.Write(this.Sensor.DepthDeviceCalibrationInfo, streamName);
                exporters.Add(depthCalibrationExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, this.Sensor.DepthDeviceCalibrationInfo.GetType(), this.Sensor.DepthDeviceCalibrationInfo, this.LocalStorage);
            }

            if (this.Configuration.OutputImu == true)
            {
                string streamName = $"{this.Configuration.RendezVousApplicationName}_IMU";
                RemoteExporter imuExporter = new RemoteExporter(this.pipeline, portCount++, this.Configuration.ConnectionType);
                imuExporter.Exporter.Write(this.Sensor.Imu, streamName);
                exporters.Add(imuExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
                this.server.CreateConnectorAndStore(streamName, $"{this.Configuration.RendezVousApplicationName}-{streamName}", session, this.pipeline, this.Sensor.Imu.GetType(), this.Sensor.Imu, this.LocalStorage);
            }

            this.server.TriggerNewProcessEvent(this.Configuration.RendezVousApplicationName);
            return new Rendezvous.Process(this.Configuration.RendezVousApplicationName, exporters, "Version1.0");
        }

        /// <summary>
        /// Runs the pipeline asynchronously.
        /// </summary>
        public void RunAsync()
        {
            this.pipeline?.RunAsync();
        }

        /// <summary>
        /// Disposes the pipeline resources.
        /// </summary>
        public void Dispose()
        {
            this.pipeline?.Dispose();
        }
    }
}
