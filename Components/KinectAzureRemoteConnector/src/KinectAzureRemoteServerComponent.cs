using Microsoft.Psi;
using Microsoft.Psi.Interop.Rendezvous;

namespace SAAC.RemoteConnectors
{
    public class KinectAzureRemoteServerComponent : KinectAzureRemoteStreams
    {
        public RendezvousServer? Server { get; private set; }
        public Emitter<int> OutConnectionError { get; private set; }

        public KinectAzureRemoteServerComponent(Pipeline pipeline, KinectAzureRemoteStreamsConfiguration? configuration = null, string name = nameof(KinectAzureRemoteServerComponent))
            : base(pipeline, configuration, name)
        {
            OutConnectionError = pipeline.CreateEmitter<int>(this, "ConnectionError");
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            Server = new RendezvousServer(Configuration.StartingPort);
            Server.Rendezvous.TryAddProcess(GenerateProcess());
            Server.Error += (s, e) => { OutConnectionError.Post(e.HResult, ParentPipeline.GetCurrentTime()); };
            Server.Start();
            Sensor.Start(notifyCompletionTime);
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
