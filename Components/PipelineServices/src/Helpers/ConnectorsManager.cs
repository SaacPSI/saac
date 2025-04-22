using Microsoft.Psi;
using Microsoft.Psi.Data;

namespace SAAC.PipelineServices
{
    public abstract class ConnectorsManager
    {
        public Dictionary<string, Dictionary<string, ConnectorInfo>> Connectors { get; internal set; }
        internal EventHandler<(string, Dictionary<string, Dictionary<string, ConnectorInfo>>)>? NewProcess;
        internal EventHandler<string>? RemovedProcess;

        protected string name;

        public ConnectorsManager(Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null, string name = nameof(ConnectorsManager))
        {
            this.name = name;
            Connectors = connectors ?? new Dictionary<string, Dictionary<string, ConnectorInfo>>();
        }

        public void CreateConnector<T>(string streamName, string storeName, Session? session,Type type, IProducer<T> stream)
        {
            if (!Connectors.ContainsKey(storeName))
                Connectors.Add(storeName, new Dictionary<string, ConnectorInfo>());
            Connectors[storeName].Add(streamName, new ConnectorInfo(streamName, storeName, session == null ? "" : session.Name, type, stream));
        }

        public void TriggerNewProcessEvent(string name)
        {
            NewProcess?.Invoke(this, (name, Connectors));
        }

        public void TriggerRemoveProcessEvent(string name)
        {
            RemovedProcess?.Invoke(this, name);
        }

        public virtual void AddNewProcessEvent(EventHandler<(string, Dictionary<string, Dictionary<string, ConnectorInfo>>)> handler)
        {
            NewProcess += handler;
        }

        public virtual void AddRemoveProcessEvent(EventHandler<string> handler)
        {
            RemovedProcess += handler;
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;
        public virtual void Dispose()
        {
            /*NewEntry = null;
            RemovedEntry = null;*/
            Connectors.Clear();
        }
    }
}
