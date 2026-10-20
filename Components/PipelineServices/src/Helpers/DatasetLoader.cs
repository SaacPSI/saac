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
                    this.RegisterOverriddenTypes(store, partition);
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

                Type producedType = this.ResolveType(streamMetadata.TypeName);
                if (producedType == null)
                {
                    return false;
                }

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

        private readonly Dictionary<string, Type> TypeNameOverrides = new()
        {
            { "FusionDll.PieceStatus", typeof(SAAC.PsiFormats.PieceStatus) },

            // { "FusionDll.ObjectGazeEvent", typeof(SAAC.PsiFormats.ObjectGazeEvent) },

            // The raw streams of the sessions recorded with the SerializableClass assembly,
            // read with the PsiFormats types that have the same fields. The types they
            // contain are listed too: a store keeps the name of each of them.
            { "SerializableClass.PieceStatus", typeof(SAAC.PsiFormats.PieceStatus) },
            { "SerializableClass.ObjectGazeEvent", typeof(SAAC.PsiFormats.ObjectGazeEvent) },
            { "SerializableClass.IDs", typeof(SAAC.PsiFormats.IDs) },
            { "SerializableClass.State", typeof(SAAC.PsiFormats.State) },
            { "SerializableClass.Location", typeof(SAAC.PsiFormats.Location) },
        };

        // Namespaces whose overridden names are also mapped inside the stores that use them.
        private readonly HashSet<string> NamespacesMappedInStores = new() { "SerializableClass" };

        /// <summary>
        /// A store keeps the assembly qualified name of its types. Before any stream of the
        /// store is opened, the recorded names that have an override are mapped onto the
        /// replacing types, so that the messages are read as these types.
        /// </summary>
        private void RegisterOverriddenTypes(PsiImporter store, IPartition partition)
        {
            foreach (string persistedTypeName in partition.AvailableStreams.Select(stream => stream.TypeName).Distinct())
            {
                // "SerializableClass.PieceStatus" and ", SerializableClass, Version=1.0.0.0, ..."
                int separator = persistedTypeName.IndexOf(',');
                if (separator < 0 || Type.GetType(persistedTypeName) != null)
                {
                    continue;
                }

                string fullName = persistedTypeName.Substring(0, separator).Trim();
                string assembly = persistedTypeName.Substring(separator);
                int lastDot = fullName.LastIndexOf('.');
                if (lastDot < 0 || !this.TypeNameOverrides.ContainsKey(fullName) || !this.NamespacesMappedInStores.Contains(fullName.Substring(0, lastDot)))
                {
                    continue;
                }

                string prefix = fullName.Substring(0, lastDot + 1);
                foreach (var entry in this.TypeNameOverrides.Where(entry => entry.Key.StartsWith(prefix)))
                {
                    try
                    {
                        store.Serializers.Register(entry.Value, entry.Key + assembly);
                    }
                    catch (Exception)
                    {
                        // Already mapped for this store by another stream.
                    }
                }
            }
        }

        private Type ResolveType(string persistedTypeName)
        {
            var t = Type.GetType(persistedTypeName);
            if (t != null)
            {
                return t;
            }

            var fullName = persistedTypeName.Split(',')[0].Trim();   // "FusionDll.PieceStatus"
            if (this.TypeNameOverrides.TryGetValue(fullName, out var mapped))
            {
                return mapped;
            }

            Console.WriteLine($"DatasetLoader : type non résolu '{persistedTypeName}'");
            return null;
        }
    }
}
