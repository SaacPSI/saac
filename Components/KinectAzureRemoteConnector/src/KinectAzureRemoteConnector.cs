// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    using Microsoft.Azure.Kinect.Sensor;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Remoting;

    /// <summary>
    /// Component to be used in parallel with KinectAzureRemoteApp, it automatically connects and sorts the streams with the application.
    /// See KinectAzureRemoteConnectorConfiguration class for details.
    /// </summary>
    public class KinectAzureRemoteConnector
    {
        /// <summary>
        /// Parent pipeline that the component is part of, used for creating emitters and managing component lifecycle.
        /// </summary>
        protected Pipeline pipeline;

        private readonly LogStatus logStatus;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectAzureRemoteConnector"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration for the connector.</param>
        /// <param name="name">The name of the component.</param>
        /// <param name="log">Optional logging delegate.</param>
        public KinectAzureRemoteConnector(Pipeline parent, KinectAzureRemoteConnectorConfiguration? configuration = null, string name = nameof(KinectAzureRemoteConnector), LogStatus? log = null)
        {
            this.pipeline = parent;
            this.logStatus = log ?? ((logMessage) => { Console.WriteLine(logMessage); });
            this.Configuration = configuration ?? new KinectAzureRemoteConnectorConfiguration();
            this.OutColorImage = null;
            this.OutDepthImage = null;
            this.OutBodies = null;
            this.OutDepthDeviceCalibrationInfo = null;
            this.OutAudio = null;
            this.OutIMU = null;
            this.OutInfraredImage = null;
        }

        /// <summary>
        /// Gets the emitter of color image.
        /// </summary>
        public Emitter<Shared<EncodedImage>>? OutColorImage { get; private set; }

        /// <summary>
        /// Gets the emitter of infrared image.
        /// </summary>
        public Emitter<Shared<EncodedImage>>? OutInfraredImage { get; private set; }

        /// <summary>
        /// Gets the emitter of depth image.
        /// </summary>
        public Emitter<Shared<EncodedDepthImage>>? OutDepthImage { get; private set; }

        /// <summary>
        /// Gets the emitter of bodies.
        /// </summary>
        public Emitter<List<AzureKinectBody>>? OutBodies { get; private set; }

        /// <summary>
        /// Gets the emitter of depth calibration.
        /// </summary>
        public Emitter<Microsoft.Psi.Calibration.IDepthDeviceCalibrationInfo>? OutDepthDeviceCalibrationInfo { get; private set; }

        /// <summary>
        /// Gets the emitter of audio.
        /// </summary>
        public Emitter<AudioBuffer>? OutAudio { get; private set; }

        /// <summary>
        /// Gets the emitter of IMU data.
        /// </summary>
        public Emitter<ImuSample>? OutIMU { get; private set; }

        /// <summary>
        /// Gets the configuration for the connector.
        /// </summary>
        public KinectAzureRemoteConnectorConfiguration Configuration { get; private set; }

        /// <summary>
        /// Establishes a connection to a remote stream.
        /// </summary>
        /// <typeparam name="T">The type of data in the stream.</typeparam>
        /// <param name="name">The name of the stream.</param>
        /// <param name="remoteImporter">The remote importer for the stream.</param>
        /// <returns>The emitter for the connected stream, or null if connection failed.</returns>
        protected virtual Emitter<T>? Connection<T>(string name, RemoteImporter remoteImporter)
        {
            if (remoteImporter.Connected.WaitOne() == false)
            {
                this.logStatus(this.Configuration.RendezVousApplicationName + " failed to connect stream " + name);
                return null;
            }

            this.logStatus(this.Configuration.RendezVousApplicationName + " stream " + name + " connected.");
            var stream = remoteImporter.Importer.OpenStream<T>(name).Out;
            if (this.Configuration.Debug)
            {
                stream.Do((m, e) => { this.logStatus($"Message received on {name} @{e.OriginatingTime} : {m}"); });
            }

            return stream;
        }

        /// <summary>
        /// Generates an event handler for processing rendezvous processes.
        /// </summary>
        /// <returns>An event handler for rendezvous process events.</returns>
        public EventHandler<Rendezvous.Process> GenerateProcess()
        {
            return (_, p) =>
            {
                this.Process(p);
            };
        }

        /// <summary>
        /// Processes a rendezvous process event, connecting to available streams.
        /// </summary>
        /// <param name="p">The rendezvous process to handle.</param>
        protected virtual void Process(Rendezvous.Process p)
        {
            if (p.Name == this.Configuration.RendezVousApplicationName)
            {
                foreach (var endpoint in p.Endpoints)
                {
                    if (endpoint is Rendezvous.RemoteExporterEndpoint remoteExporterEndpoint)
                    {
                        var remoteImporter = remoteExporterEndpoint.ToRemoteImporter(this.pipeline);
                        foreach (var stream in remoteExporterEndpoint.Streams)
                        {
                            if (stream.StreamName.Contains("Audio"))
                            {
                                this.OutAudio = this.Connection<AudioBuffer>(stream.StreamName, remoteImporter);
                                break;
                            }

                            if (stream.StreamName.Contains("Bodies"))
                            {
                                this.OutBodies = this.Connection<List<AzureKinectBody>>(stream.StreamName, remoteImporter);
                                break;
                            }

                            if (stream.StreamName.Contains("Calibration"))
                            {
                                this.OutDepthDeviceCalibrationInfo = this.Connection<Microsoft.Psi.Calibration.IDepthDeviceCalibrationInfo>(stream.StreamName, remoteImporter);
                                break;
                            }
                            else if (stream.StreamName.Contains("RGB"))
                            {
                                this.OutColorImage = this.Connection<Shared<EncodedImage>>(stream.StreamName, remoteImporter);
                                break;
                            }
                            else if (stream.StreamName.Contains("Infrared"))
                            {
                                this.OutInfraredImage = this.Connection<Shared<EncodedImage>>(stream.StreamName, remoteImporter);
                                break;
                            }
                            else if (stream.StreamName.Contains("Depth"))
                            {
                                this.OutDepthImage = this.Connection<Shared<EncodedDepthImage>>(stream.StreamName, remoteImporter);
                                break;
                            }
                            else if (stream.StreamName.Contains("IMU"))
                            {
                                this.OutIMU = this.Connection<ImuSample>(stream.StreamName, remoteImporter);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
