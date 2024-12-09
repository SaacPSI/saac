using Microsoft.Psi.Data;
using Microsoft.Psi;

namespace SAAC.RendezVousPipelineServices
{
    public class ConnectorsAndStoresCreator : ConnectorsManager
    {
        public Dictionary<string, Dictionary<string, PsiExporter>> Stores { get; protected set; }
        public string StorePath { get; set; }

        public ConnectorsAndStoresCreator(string storePath = "", Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null, string name = nameof(ConnectorsAndStoresCreator))
            : base(connectors, name)
        {
            StorePath = storePath;
            Stores = new Dictionary<string, Dictionary<string, PsiExporter>>();
        }

        public void CreateConnectorAndStore<T>(string streamName, string storeName, Session? session, Pipeline p, Type type, IProducer<T> stream, bool storeSteam = true)
        {
            if (!Connectors.ContainsKey(storeName))
                Connectors.Add(storeName, new Dictionary<string, ConnectorInfo>());
            Connectors[storeName].Add(streamName, new ConnectorInfo(streamName, storeName, session == null ? "" : session.Name, type, stream));
            if (storeSteam && session != null)
                CreateStore(p, session, streamName, storeName, stream);
        }

        public void CreateStore<T>(Pipeline pipeline, Session session, string streamName, string storeName, IProducer<T> source)
        {
            if (Stores.ContainsKey(session.Name) && Stores[session.Name].ContainsKey(storeName))
            {
                Stores[session.Name][storeName].Write(source, streamName);
            }
            else
            {
                PsiExporter store = PsiStore.Create(pipeline, storeName, $"{StorePath}/{session.Name}/");
                store.Write(source, streamName);
                session.AddPartitionFromPsiStoreAsync(storeName, $"{StorePath}/{session.Name}/");
                if (!Stores.ContainsKey(session.Name))
                    Stores.Add(session.Name, new Dictionary<string, PsiExporter>());
                Stores[session.Name].Add(storeName, store);
            }
        }
    }
}
