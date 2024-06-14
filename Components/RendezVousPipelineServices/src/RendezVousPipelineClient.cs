using Microsoft.Psi.Interop.Rendezvous;

namespace SAAC.RendezVousPipelineServices
{
    public class RendezVousPipelineClient : RendezVousPipeline
    {
        private RendezvousClient client;

        public RendezVousPipelineClient(string serverAddress, RendezVousPipelineConfiguration? configuration, string name = nameof(RendezVousPipelineServer), LogStatus? log = null)
            : base(configuration, name, log)
        {
            rendezvousRelay = client = new RendezvousClient(serverAddress, this.Configuration.RendezVousPort);
        }

        protected override void StartRendezVous()
        {
            client.Start();
            log("Client connected!");
        }

        protected override void StopRendezVous()
        {
            client.Stop();
        }
    }
}
