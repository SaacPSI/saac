// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    using Microsoft.Psi;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Interface for complex data transformers in pipeline services.
    /// </summary>
    public interface IComplexTransformer
    {
        /// <summary>
        /// Creates connections for the transformer's outputs.
        /// Called inside rendezVousPipeline.CreateConnectorAndStore() for each output.
        /// </summary>
        /// <param name="name">The name of the connection.</param>
        /// <param name="storeName">The name of the store.</param>
        /// <param name="session">The session to use.</param>
        /// <param name="p">The pipeline.</param>
        /// <param name="storeSteam">Whether to store the stream.</param>
        /// <param name="rendezVousPipeline">The rendezvous pipeline instance.</param>
        void CreateConnections(string name, string storeName, Session? session, Pipeline p, bool storeSteam, RendezVousPipeline rendezVousPipeline);
    }
}
