using Microsoft.Psi.Components;
using Microsoft.Psi;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Imaging;

namespace RemoteConnectors
{
    public class KinectAzureRemoteConnectorComponent : KinectAzureRemoteConnector
    {
        private RendezvousClient? Client;
        public Emitter<int> OutConnectionError { get; private set; }
        public bool WaitForConnection { get; private set; }

        public KinectAzureRemoteConnectorComponent(Pipeline pipeline, KinectAzureRemoteConnectorConfiguration? configuration = null, bool waitForConnection = true)
            : base(pipeline, configuration)
        {
            OutConnectionError = pipeline.CreateEmitter<int>(this, "ConnectionError");
            WaitForConnection = waitForConnection;
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            Client = new RendezvousClient(Configuration.RendezVousServerAddress, (int)Configuration.RendezVousServerPort);
            Client.Rendezvous.ProcessAdded += GenerateProcess();
            Client.Error += (s, e) => { OutConnectionError.Post(e.HResult, ParentPipeline.GetCurrentTime()); };
            Client.Start();
            if (WaitForConnection && !Client.Connected.WaitOne())
            {
                throw new Exception("Error while connecting to server at " + Configuration.RendezVousServerAddress);
            }
            notifyCompletionTime.Invoke(ParentPipeline.GetCurrentTime());
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (Client != null)
            {
                Client.Stop();
                Client.Rendezvous.TryRemoveProcess(Configuration.RendezVousApplicationName);
                Client.Dispose();
            }
            notifyCompleted.Invoke();
        }
    }
}
