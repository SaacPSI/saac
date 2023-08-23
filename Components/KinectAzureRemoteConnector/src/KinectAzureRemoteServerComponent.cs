using Microsoft.Psi;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Components;

namespace RemoteConnectors
{
    public class KinectAzureRemoteServerComponent : KinectAzureRemoteServer, ISourceComponent
    {
        private RendezvousServer? Server;

        public KinectAzureRemoteServerComponent(Pipeline pipeline, KinectAzureRemoteServerConfiguration? configuration = null)
            : base(pipeline, configuration)
        {
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            Server = new RendezvousServer(Configuration.RendezVousPort);
            Server.Rendezvous.TryAddProcess(GenerateProcess());
            Server.Start();
            notifyCompletionTime.Invoke(ParentPipeline.GetCurrentTime());
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (Server != null)
            {
                Server.Stop();
                Server.Rendezvous.TryRemoveProcess(Configuration.RendezVousApplicationName);
                Server.Dispose();
            }
            if (Sensor != null)
            {
                Sensor.Stop(finalOriginatingTime, notifyCompleted);
                Sensor.Dispose();
            }
            notifyCompleted.Invoke();
        }
    }
}
