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

    /// <summary>
    /// Component for streaming Kinect Azure sensor data over a network without local storage.
    /// </summary>
    public class KinectAzureRemoteStreams
    {
        /// <summary>
        /// Parent //psi pipeline that the component is part of, used for creating emitters and managing component lifecycle.
        /// </summary>
        protected Pipeline parentPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectAzureRemoteStreams"/> class.
        /// </summary>
        /// <param name="pipeline">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration for the Kinect Azure streams.</param>
        /// <param name="name">The name of the component.</param>
        public KinectAzureRemoteStreams(Pipeline pipeline, KinectAzureRemoteStreamsConfiguration? configuration = null, string name = nameof(KinectAzureRemoteStreams))
        {
            this.parentPipeline = pipeline;
            this.Configuration = configuration ?? new KinectAzureRemoteStreamsConfiguration();
        }

        /// <summary>
        /// Gets the configuration for the Kinect Azure streams.
        /// </summary>
        public KinectAzureRemoteStreamsConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets the Azure Kinect sensor instance.
        /// </summary>
        public AzureKinectSensor? Sensor { get; private set; }

        /// <summary>
        /// Generates a rendezvous process with configured stream exporters.
        /// Creates remote exporters for audio, bodies, color, infrared, depth, calibration, and IMU based on configuration.
        /// </summary>
        /// <returns>A configured rendezvous process with all enabled stream endpoints.</returns>
        public Rendezvous.Process GenerateProcess()
        {
            int portCount = this.Configuration.StartingPort + 1;

            if (this.Configuration.OutputBodies == true)
            {
                this.Configuration.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            }

            this.Sensor = new AzureKinectSensor(this.parentPipeline, this.Configuration);

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();
            if (this.Configuration.OutputAudio == true)
            {
                AudioCaptureConfiguration audioCaptureConfig = new AudioCaptureConfiguration();
                AudioCapture audioCapture = new AudioCapture(this.parentPipeline, audioCaptureConfig);
                int index = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevices().ToList().FindIndex(value => { return value.Contains("Azure"); });
                audioCaptureConfig.DeviceName = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevices().ElementAt(index);
                RemoteExporter soundExporter = new RemoteExporter(this.parentPipeline, portCount++, this.Configuration.ConnectionType);
                soundExporter.Exporter.Write(audioCapture.Out, $"{this.Configuration.RendezVousApplicationName}_Audio");
                exporters.Add(soundExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
            }

            if (this.Configuration.OutputBodies == true)
            {
                RemoteExporter skeletonExporter = new RemoteExporter(this.parentPipeline, portCount++, this.Configuration.ConnectionType);
                skeletonExporter.Exporter.Write(this.Sensor.Bodies, $"{this.Configuration.RendezVousApplicationName}_Bodies");
                exporters.Add(skeletonExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
            }

            if (this.Configuration.OutputColor == true)
            {
                RemoteExporter imageExporter = new RemoteExporter(this.parentPipeline, portCount++, this.Configuration.ConnectionType);
                imageExporter.Exporter.Write(this.Sensor.ColorImage.EncodeJpeg(this.Configuration.EncodingVideoLevel), $"{this.Configuration.RendezVousApplicationName}_RGB");
                exporters.Add(imageExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
            }

            if (this.Configuration.OutputInfrared == true)
            {
                RemoteExporter imageExporter = new RemoteExporter(this.parentPipeline, portCount++, this.Configuration.ConnectionType);
                imageExporter.Exporter.Write(this.Sensor.InfraredImage.EncodeJpeg(this.Configuration.EncodingVideoLevel), $"{this.Configuration.RendezVousApplicationName}_Infrared");
                exporters.Add(imageExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
            }

            if (this.Configuration.OutputDepth == true)
            {
                RemoteExporter depthExporter = new RemoteExporter(this.parentPipeline, portCount++, this.Configuration.ConnectionType);
                depthExporter.Exporter.Write(this.Sensor.DepthImage.EncodePng(), $"{this.Configuration.RendezVousApplicationName}_Depth");
                exporters.Add(depthExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
            }

            if (this.Configuration.OutputCalibration == true)
            {
                RemoteExporter depthCalibrationExporter = new RemoteExporter(this.parentPipeline, portCount++, this.Configuration.ConnectionType);
                depthCalibrationExporter.Exporter.Write(this.Sensor.DepthDeviceCalibrationInfo, $"{this.Configuration.RendezVousApplicationName}_Calibration");
                exporters.Add(depthCalibrationExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
            }

            if (this.Configuration.OutputImu == true)
            {
                RemoteExporter imuExporter = new RemoteExporter(this.parentPipeline, portCount++, this.Configuration.ConnectionType);
                imuExporter.Exporter.Write(this.Sensor.Imu, $"{this.Configuration.RendezVousApplicationName}_IMU");
                exporters.Add(imuExporter.ToRendezvousEndpoint(this.Configuration.IpToUse));
            }

            return new Rendezvous.Process(this.Configuration.RendezVousApplicationName, exporters, "Version1.0");
        }

        /// <summary>
        /// Runs the parent pipeline asynchronously.
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
