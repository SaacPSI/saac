using Microsoft.Psi.Components;
using Microsoft.Psi;
using Microsoft.Psi.Interop.Rendezvous;

namespace RemoteConnectors
{
    public class KinectAzureRemoteConnectorComponent : KinectAzureRemoteConnector, ISourceComponent
    {
        private RendezvousClient? Client;

        public KinectAzureRemoteConnectorComponent(Pipeline parent, KinectAzureRemoteConnectorConfiguration? configuration = null)
            : base(parent, configuration)
        { }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            Client = new RendezvousClient(Configuration.RendezVousServerAddress, (int)Configuration.RendezVousServerPort);
            Client.Rendezvous.ProcessAdded += GenerateProcess();
            Client.Start();
            if (!Client.Connected.WaitOne())
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
