using Microsoft.Psi.Data;
using Microsoft.Psi;

namespace SAAC.PipelineServices
{
    public interface IComplexTransformer
    {
        // Calls inside rendezVousPipeline.CreateConnectorAndStore() for each outputs
        public void CreateConnections(string name, string storeName, Session? session, Pipeline p, bool storeSteam, RendezVousPipeline rendezVousPipeline);
    }
}
