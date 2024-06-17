using Microsoft.Psi.Data;
using Microsoft.Psi;

namespace SAAC.RendezVousPipelineServices
{
    public interface IComplexTransfromer
    {
        // Calls inside rendezVousPipeline.CreateConnectorAndStore() for each outputs
        public void CreateConnections(string name, string storeName, Session? session, Pipeline p, bool storeSteam, RendezVousPipeline rendezVousPipeline);
    }
}
