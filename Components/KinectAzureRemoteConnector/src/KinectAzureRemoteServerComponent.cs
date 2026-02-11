// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    using Microsoft.Psi;
    using Microsoft.Psi.Interop.Rendezvous;

    /// <summary>
    /// Component that hosts a rendezvous server for streaming Kinect Azure data.
    /// Extends KinectAzureRemoteStreams with server hosting capabilities and ISourceComponent implementation.
    /// </summary>
    public class KinectAzureRemoteServerComponent : KinectAzureRemoteStreams
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KinectAzureRemoteServerComponent"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">Optional configuration for the Kinect Azure streams.</param>
        /// <param name="name">The name of the component.</param>
        public KinectAzureRemoteServerComponent(Pipeline pipeline, KinectAzureRemoteStreamsConfiguration? configuration = null, string name = nameof(KinectAzureRemoteServerComponent))
            : base(pipeline, configuration, name)
        {
            this.OutConnectionError = pipeline.CreateEmitter<int>(this, "ConnectionError");
        }

        /// <summary>
        /// Gets the rendezvous server instance.
        /// </summary>
        public RendezvousServer? Server { get; private set; }

        /// <summary>
        /// Gets the emitter for connection error codes.
        /// </summary>
        public Emitter<int> OutConnectionError { get; private set; }

        /// <summary>
        /// Starts the rendezvous server and the Kinect Azure sensor.
        /// </summary>
        /// <param name="notifyCompletionTime">Delegate to notify completion time.</param>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.Server = new RendezvousServer(this.Configuration.StartingPort);
            this.Server.Rendezvous.TryAddProcess(this.GenerateProcess());
            this.Server.Error += (s, e) => { this.OutConnectionError.Post(e.HResult, this.parentPipeline.GetCurrentTime()); };
            this.Server.Start();
            this.Sensor.Start(notifyCompletionTime);
            notifyCompletionTime.Invoke(this.parentPipeline.GetCurrentTime());
        }

        /// <summary>
        /// Stops the rendezvous server and the Kinect Azure sensor.
        /// </summary>
        /// <param name="finalOriginatingTime">The final originating time.</param>
        /// <param name="notifyCompleted">Delegate to notify completion.</param>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (this.Server != null)
            {
                this.Server.Stop();
                this.Server.Rendezvous.TryRemoveProcess(this.Configuration.RendezVousApplicationName);
                this.Server.Dispose();
            }

            if (this.Sensor != null)
            {
                this.Sensor.Stop(finalOriginatingTime, notifyCompleted);
                this.Sensor.Dispose();
            }

            notifyCompleted.Invoke();
        }
    }
}
