﻿using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Azure.Kinect.Sensor;

namespace SAAC.RemoteConnectors
{
    /// <summary>
    /// Component to be used in parallel with KinectAzureRemoteApp, it automatically connect and sort the streams with the application.
    /// See KinectAzureRemoteConnectorConfiguration class for details.
    /// </summary>
    public class KinectAzureRemoteConnector
    {
        /// <summary>
        /// Gets the emitter of color image.
        /// </summary>
        public Emitter<Shared<EncodedImage>>? OutColorImage { get; private set; }

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

        public KinectAzureRemoteConnectorConfiguration Configuration { get; private set; }

        protected Pipeline pipeline;
        protected SAAC.LogStatus logStatus;

        public KinectAzureRemoteConnector(Pipeline parent, KinectAzureRemoteConnectorConfiguration? configuration = null, string name = nameof(KinectAzureRemoteConnector), LogStatus? log = null)
        {
            pipeline = parent;
            logStatus = log ?? ((log) => { Console.WriteLine(log); });
            Configuration = configuration ?? new KinectAzureRemoteConnectorConfiguration();
            OutColorImage = null;
            OutDepthImage = null;
            OutBodies = null;
            OutDepthDeviceCalibrationInfo = null;
            OutAudio = null;
            OutIMU = null;
        }

        protected virtual Emitter<T>? Connection<T>(string name, RemoteImporter remoteImporter)
        {
            if (remoteImporter.Connected.WaitOne() == false)
            {
                logStatus(Configuration.RendezVousApplicationName + " failed to connect stream " + name);
                return null;
            }
            logStatus(Configuration.RendezVousApplicationName + " stream " + name + " connected.");
            var stream = remoteImporter.Importer.OpenStream<T>(name).Out;
            if (Configuration.Debug)
                stream.Do((m, e) => { logStatus($"Message recieved on {name} @{e.OriginatingTime} : {m}"); });
            return stream;
        }

        public EventHandler<Rendezvous.Process> GenerateProcess()
        {
            return (_, p) =>
            {
                Process(p);
            };
        }

        protected virtual void Process(Rendezvous.Process p)
        {
            if (p.Name == Configuration.RendezVousApplicationName)
            {
                foreach (var endpoint in p.Endpoints)
                {
                    if (endpoint is Rendezvous.RemoteExporterEndpoint remoteExporterEndpoint)
                    {
                        var remoteImporter = remoteExporterEndpoint.ToRemoteImporter(pipeline);
                        foreach (var stream in remoteExporterEndpoint.Streams)
                        {
                            if (stream.StreamName.Contains("Audio"))
                            {
                                OutAudio = Connection<AudioBuffer>(stream.StreamName, remoteImporter);
                                break;
                            }
                            if (stream.StreamName.Contains("Bodies"))
                            {
                                OutBodies = Connection<List<AzureKinectBody>>(stream.StreamName, remoteImporter);
                                break;
                            }
                            if (stream.StreamName.Contains("Calibration"))
                            {
                                OutDepthDeviceCalibrationInfo = Connection<Microsoft.Psi.Calibration.IDepthDeviceCalibrationInfo>(stream.StreamName, remoteImporter);
                                break;
                            }
                            else if (stream.StreamName.Contains("RGB"))
                            {
                                OutColorImage = Connection<Shared<EncodedImage>>(stream.StreamName, remoteImporter);
                                break;
                            }
                            else if (stream.StreamName.Contains("Depth"))
                            {
                                OutDepthImage = Connection<Shared<EncodedDepthImage>>(stream.StreamName, remoteImporter);
                                break;
                            }
                            else if (stream.StreamName.Contains("IMU"))
                            {
                                OutIMU = Connection<ImuSample>(stream.StreamName, remoteImporter);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
