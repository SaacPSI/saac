// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    using Microsoft.Psi;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Loads datasets and creates connectors for stored data streams.
    /// </summary>
    public class DatasetLoader : ConnectorsManager
    {
        /// <summary>
        /// Gets or sets the dictionary of PSI importers organized by session and store name.
        /// </summary>
        public Dictionary<string, Dictionary<string, PsiImporter>> Stores { get; protected set; }

        /// <summary>
        /// The pipeline used for loading data.
        /// </summary>
        protected Pipeline? pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatasetLoader"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to use for loading.</param>
        /// <param name="connectors">Optional dictionary of existing connectors.</param>
        /// <param name="name">The name of the loader.</param>
        public DatasetLoader(Pipeline pipeline, Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null, string name = nameof(DatasetLoader))
            : base(connectors)
        {
            this.pipeline = pipeline;
            this.Stores = new Dictionary<string, Dictionary<string, PsiImporter>>();
        }

        /// <summary>
        /// Disposes resources used by this loader.
        /// </summary>
        public void Dispose()
        {
            base.Dispose();
            this.Stores = null;
        }

        /// <summary>
        /// Loads a dataset from the specified path.
        /// </summary>
        /// <param name="dataset">The path to the dataset.</param>
        /// <param name="sessionName">Optional session name to filter by.</param>
        /// <returns>True if loading succeeded; otherwise false.</returns>
        public bool Load(string dataset, string? sessionName = null)
        {
            return this.Load(Dataset.Load(dataset), sessionName);
        }

        /// <summary>
        /// Loads a dataset and creates connectors for all its streams.
        /// </summary>
        /// <param name="dataset">The dataset to load.</param>
        /// <param name="sessionName">Optional session name to filter by.</param>
        /// <returns>True if loading succeeded; otherwise false.</returns>
        public bool Load(Dataset dataset, string? sessionName = null)
        {
            bool isGood = true;
            foreach (Session session in dataset.Sessions)
            {
                if (sessionName != null && session.Name != sessionName)
                {
                    continue;
                }

                foreach (var partition in session.Partitions)
                {
                    foreach (var streamMetadata in partition.AvailableStreams)
                    {
                        isGood &= this.LoadStoreAndCreateConnector(session, partition, streamMetadata);
                    }
                }

                this.TriggerNewProcessEvent(session.Name);
            }

            return isGood;
        }

        private bool LoadStoreAndCreateConnector(Session session, IPartition partition, IStreamMetadata streamMetadata)
        {
            try
            {
                PsiImporter store;
                if (!this.Stores.ContainsKey(session.Name))
                {
                    this.Stores.Add(session.Name, new Dictionary<string, PsiImporter>());
                }

                if (!this.Stores[session.Name].ContainsKey(streamMetadata.StoreName))
                {
                    store = PsiStore.Open(this.pipeline, streamMetadata.StoreName, streamMetadata.StorePath);
                    this.Stores[session.Name].Add(streamMetadata.StoreName, store);
                }
                else
                {
                    store = this.Stores[session.Name][streamMetadata.StoreName];
                }

                if (!this.Connectors.ContainsKey(streamMetadata.StoreName))
                {
                    this.Connectors.Add(streamMetadata.StoreName, new Dictionary<string, ConnectorInfo>());
                }

                Type producedType = Type.GetType(streamMetadata.TypeName);
                this.Connectors[streamMetadata.StoreName].Add(streamMetadata.Name, new ConnectorInfo(streamMetadata.Name, session.Name, streamMetadata.StoreName, producedType, typeof(PsiImporter).GetMethod("OpenStream").MakeGenericMethod(producedType).Invoke(
                    store,
                    [streamMetadata.Name, null, null])));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}\n{ex.InnerException}");
                return false;
            }

            return true;
        }
    }
}
