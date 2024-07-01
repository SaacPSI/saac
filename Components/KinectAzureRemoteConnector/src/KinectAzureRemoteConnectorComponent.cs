using Microsoft.Psi;
using Microsoft.Psi.Interop.Rendezvous;

namespace SAAC.RemoteConnectors
{
    public class KinectAzureRemoteConnectorComponent : KinectAzureRemoteConnector
    {
        private RendezvousClient? Client;
        public Emitter<int> OutConnectionError { get; private set; }
        public bool WaitForConnection { get; private set; }

        public KinectAzureRemoteConnectorComponent(Pipeline pipeline, KinectAzureRemoteConnectorConfiguration? configuration = null, string name = nameof(KinectAzureRemoteConnectorComponent), bool waitForConnection = true, LogStatus? log = null)
            : base(pipeline, configuration, name, log)
        {
            OutConnectionError = pipeline.CreateEmitter<int>(this, "ConnectionError");
            WaitForConnection = waitForConnection;
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            Client = new RendezvousClient(Configuration.RendezVousServerAddress, (int)Configuration.RendezVousServerPort);
            Client.Rendezvous.ProcessAdded += GenerateProcess();
            Client.Error += (s, e) => { OutConnectionError.Post(e.HResult, pipeline.GetCurrentTime()); };
            Client.Start();
            if (WaitForConnection && !Client.Connected.WaitOne())
            {
                throw new Exception("Error while connecting to server at " + Configuration.RendezVousServerAddress);
            }
            notifyCompletionTime.Invoke(pipeline.GetCurrentTime());
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
