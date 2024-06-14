using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi;
namespace SAAC.RendezVousPipelineServices
{
    public class RendezVousPipelineServer : RendezVousPipeline
    {
        private RendezvousServer server;

        public RendezVousPipelineServer(RendezVousPipelineConfiguration? configuration, string name = nameof(RendezVousPipelineServer), LogStatus? log = null)
            : base(configuration, name, log)
        {
            rendezvousRelay = server = new RendezvousServer(this.Configuration.RendezVousPort);
        }

        protected override void StartRendezVous()
        {
            server.Start();
            log("Server started!");
        }

        protected override void StopRendezVous()
        {
            server.Stop();
        }
    }
}
