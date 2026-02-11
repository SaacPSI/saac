// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    using Microsoft.Psi;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Abstract base class for creating connectors and stores in pipeline services.
    /// </summary>
    public abstract class ConnectorsAndStoresCreator : ConnectorsManager
    {
        /// <summary>
        /// Gets or sets the dictionary of stores organized by session and store name.
        /// </summary>
        public Dictionary<string, Dictionary<string, PsiExporter>> Stores { get; protected set; }

        /// <summary>
        /// Gets or sets the file path where stores are saved.
        /// </summary>
        public string StorePath { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorsAndStoresCreator"/> class.
        /// </summary>
        /// <param name="storePath">The path where stores will be saved.</param>
        /// <param name="connectors">Optional dictionary of existing connectors.</param>
        /// <param name="name">The name of the creator.</param>
        public ConnectorsAndStoresCreator(string storePath = "", Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null, string name = nameof(ConnectorsAndStoresCreator))
            : base(connectors, name)
        {
            this.StorePath = storePath;
            this.Stores = new Dictionary<string, Dictionary<string, PsiExporter>>();
        }

        /// <summary>
        /// Disposes resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            base.Dispose();
            this.Stores = null;
        }

        /// <summary>
        /// Creates a connector and optionally a store for a stream.
        /// </summary>
        /// <typeparam name="T">The type of data in the stream.</typeparam>
        /// <param name="streamName">The name of the stream.</param>
        /// <param name="storeName">The name of the store.</param>
        /// <param name="session">The session to use.</param>
        /// <param name="p">The pipeline.</param>
        /// <param name="type">The data type.</param>
        /// <param name="stream">The producer stream.</param>
        /// <param name="storeSteam">Whether to store the stream.</param>
        public void CreateConnectorAndStore<T>(string streamName, string storeName, Session? session, Pipeline p, Type type, IProducer<T> stream, bool storeSteam = true)
        {
            lock (this)
            {
                this.CreateConnector(streamName, storeName, session, type, stream);
                if (storeSteam && session != null)
                {
                    this.CreateStore(p, session, streamName, storeName, stream);
                }
            }
        }

        /// <summary>
        /// Creates a store for the specified stream.
        /// </summary>
        /// <typeparam name="T">The type of data in the stream.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="session">The session.</param>
        /// <param name="streamName">The stream name.</param>
        /// <param name="storeName">The store name.</param>
        /// <param name="source">The source producer.</param>
        public virtual void CreateStore<T>(Pipeline pipeline, Session session, string streamName, string storeName, IProducer<T> source)
        {
            PsiExporter store = this.GetOrCreateStore(pipeline, session, storeName);
            store.Write(source, streamName);
        }

        /// <summary>
        /// Gets an existing store or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="session">The session.</param>
        /// <param name="storeName">The store name.</param>
        /// <returns>The PSI exporter store.</returns>
        public virtual PsiExporter GetOrCreateStore(Pipeline pipeline, Session session, string storeName)
        {
            if (this.Stores.ContainsKey(session.Name) && this.Stores[session.Name].ContainsKey(storeName))
            {
                return this.Stores[session.Name][storeName];
            }
            else
            {
                PsiExporter store = PsiStore.Create(pipeline, storeName, $"{this.StorePath}/{session.Name}/");
                session.AddPartitionFromPsiStoreAsync(storeName, $"{this.StorePath}/{session.Name}/");
                if (!this.Stores.ContainsKey(session.Name))
                {
                    this.Stores.Add(session.Name, new Dictionary<string, PsiExporter>());
                }

                this.Stores[session.Name].Add(storeName, store);
                return store;
            }
        }
    }
}
