using Microsoft.Psi;
using Microsoft.Psi.Data;

namespace SAAC.RendezVousPipelineServices
{
    public class DatasetLoader
    {
        public Dictionary<string, Dictionary<string, PsiImporter>> Stores { get; private set; }
        public Dictionary<string, Dictionary<string, ConnectorInfo>> Connectors { get; private set; }

        private Pipeline pipeline;

        public DatasetLoader(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            Stores = new Dictionary<string, Dictionary<string, PsiImporter>>();
            Connectors = new Dictionary<string, Dictionary<string, ConnectorInfo>>();
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
                Connectors[session.Name].Add(streamMetadata.Name, new ConnectorInfo(streamMetadata.Name, session.Name, streamMetadata.StoreName, Type.GetType(streamMetadata.TypeName), store.OpenDynamicStream(streamMetadata.Name)));
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
