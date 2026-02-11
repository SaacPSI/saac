// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    using Microsoft.Psi;
    using Microsoft.Psi.Interop.Rendezvous;

    /// <summary>
    /// Component that connects to a remote Kinect Azure device via a rendezvous client.
    /// Extends KinectAzureRemoteConnector with client connection management and ISourceComponent implementation.
    /// </summary>
    public class KinectAzureRemoteConnectorComponent : KinectAzureRemoteConnector
    {
        private RendezvousClient? client;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectAzureRemoteConnectorComponent"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">Optional configuration for the remote connection.</param>
        /// <param name="name">The name of the component.</param>
        /// <param name="waitForConnection">Whether to wait for connection to be established before proceeding.</param>
        /// <param name="log">Optional logging delegate.</param>
        public KinectAzureRemoteConnectorComponent(Pipeline pipeline, KinectAzureRemoteConnectorConfiguration? configuration = null, string name = nameof(KinectAzureRemoteConnectorComponent), bool waitForConnection = true, LogStatus? log = null)
            : base(pipeline, configuration, name, log)
        {
            this.OutConnectionError = pipeline.CreateEmitter<int>(this, "ConnectionError");
            this.WaitForConnection = waitForConnection;
        }

        /// <summary>
        /// Gets the emitter for connection error codes.
        /// </summary>
        public Emitter<int> OutConnectionError { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to wait for connection before proceeding.
        /// </summary>
        public bool WaitForConnection { get; private set; }

        /// <summary>
        /// Starts the component and establishes connection to the rendezvous server.
        /// </summary>
        /// <param name="notifyCompletionTime">Delegate to notify completion time.</param>
        /// <exception cref="Exception">Thrown when connection to server fails and WaitForConnection is true.</exception>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.client = new RendezvousClient(this.Configuration.RendezVousServerAddress, (int)this.Configuration.RendezVousServerPort);
            this.client.Rendezvous.ProcessAdded += this.GenerateProcess();
            this.client.Error += (s, e) => { this.OutConnectionError.Post(e.HResult, this.pipeline.GetCurrentTime()); };
            this.client.Start();
            if (this.WaitForConnection && !this.client.Connected.WaitOne())
            {
                throw new Exception("Error while connecting to server at " + this.Configuration.RendezVousServerAddress);
            }

            notifyCompletionTime.Invoke(this.pipeline.GetCurrentTime());
        }

        /// <summary>
        /// Stops the component and disconnects from the rendezvous server.
        /// </summary>
        /// <param name="finalOriginatingTime">The final originating time.</param>
        /// <param name="notifyCompleted">Delegate to notify completion.</param>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (this.client != null)
            {
                this.client.Stop();
                this.client.Rendezvous.TryRemoveProcess(this.Configuration.RendezVousApplicationName);
                this.client.Dispose();
            }

            notifyCompleted.Invoke();
        }
    }
}
