// <copyright file="IndexStore.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using Microsoft.Psi;
using Microsoft.Psi.Data;
using SAAC.PipelineServices;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Where the indicator components persist their streams.
    ///
    /// The components used to require a <see cref="DatasetPipeline"/> and to hard code the
    /// session and the store they wrote into, which made them impossible to instantiate in a
    /// plain pipeline or in a test. The destination is now an optional object: a component
    /// built without it computes exactly the same and simply stores nothing.
    /// </summary>
    public class IndexStore
    {
        /// <summary>Session the components have always written into.</summary>
        public const string DefaultSessionName = "RawDataPipelineProcess.000";

        /// <summary>Store the components have always written into.</summary>
        public const string DefaultStoreName = "LiveVisualization";

        private readonly DatasetPipeline server;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexStore"/> class.
        /// </summary>
        /// <param name="server">Dataset pipeline owning the connectors and the stores.</param>
        /// <param name="sessionName">Name of the session receiving the streams.</param>
        /// <param name="storeName">Name of the store receiving the streams.</param>
        public IndexStore(DatasetPipeline server, string sessionName = DefaultSessionName, string storeName = DefaultStoreName)
        {
            this.server = server ?? throw new System.ArgumentNullException(nameof(server));
            this.SessionName = string.IsNullOrEmpty(sessionName) ? DefaultSessionName : sessionName;
            this.StoreName = string.IsNullOrEmpty(storeName) ? DefaultStoreName : storeName;
            this.Session = server.GetSession(this.SessionName);
        }

        /// <summary>Gets the name of the session receiving the streams.</summary>
        public string SessionName { get; }

        /// <summary>Gets the name of the store receiving the streams.</summary>
        public string StoreName { get; }

        /// <summary>
        /// Gets the session, or null when the dataset does not contain it. A connector is
        /// still created in that case, but nothing is written on disk.
        /// </summary>
        public Session? Session { get; }

        /// <summary>Declares a stream as a connector and writes it into the store.</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="pipeline">Pipeline hosting the stream.</param>
        /// <param name="streamName">Name of the stream in the store.</param>
        /// <param name="stream">The stream.</param>
        public void Write<T>(Pipeline pipeline, string streamName, IProducer<T> stream)
            => this.server.CreateConnectorAndStore(streamName, this.StoreName, this.Session, pipeline, typeof(T), stream, true);
    }
}
