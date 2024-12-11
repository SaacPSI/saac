using Microsoft.Psi;
using Microsoft.Psi.Data;

namespace SAAC.RendezVousPipelineServices
{
    public class DatasetLoader : ConnectorsManager
    {
        public Dictionary<string, Dictionary<string, PsiImporter>> Stores { get; protected set; }

        protected Pipeline? pipeline;

        public DatasetLoader(Pipeline pipeline, Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null, string name = nameof(DatasetLoader))
            : base(connectors)
        {
            this.pipeline = pipeline;
            Stores = new Dictionary<string, Dictionary<string, PsiImporter>>();
        }

        public bool Load(string dataset, string? sessionName = null)
        {
            return Load(Dataset.Load(dataset), sessionName);
        }

        public bool Load(Dataset dataset, string? sessionName = null)
        {
            bool isGood = true;
            foreach (Session session in dataset.Sessions)
            {
                if(sessionName != null && session.Name != sessionName)
                    continue;
                foreach (var partition in session.Partitions)
                    foreach (var streamMetadata in partition.AvailableStreams)
                        isGood &= LoadStoreAndCreateConnector(session, partition, streamMetadata);
                TriggerNewProcessEvent(session.Name);
            }
            return isGood;
        }

        private bool LoadStoreAndCreateConnector(Session session, IPartition partition, IStreamMetadata streamMetadata)
        {
            try
            {
                PsiImporter store;
                if (!Stores.ContainsKey(session.Name))
                    Stores.Add(session.Name, new Dictionary<string, PsiImporter>());
                if (!Stores[session.Name].ContainsKey(streamMetadata.StoreName))
                {
                    store = PsiStore.Open(pipeline, streamMetadata.StoreName, streamMetadata.StorePath);
                    Stores[session.Name].Add(streamMetadata.StoreName, store);
                }
                else
                    store = Stores[session.Name][streamMetadata.StoreName];
                if (!Connectors.ContainsKey(session.Name))
                    Connectors.Add(session.Name, new Dictionary<string, ConnectorInfo>());
                Type producedType = Type.GetType(streamMetadata.TypeName);
                Connectors[session.Name].Add(streamMetadata.Name, new ConnectorInfo(streamMetadata.Name, session.Name, streamMetadata.StoreName, producedType, typeof(PsiImporter).GetMethod("OpenStream").MakeGenericMethod(producedType).Invoke(store, [streamMetadata.Name, null, null])));
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
