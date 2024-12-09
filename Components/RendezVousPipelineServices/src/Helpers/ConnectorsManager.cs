namespace SAAC.RendezVousPipelineServices
{
    public abstract class ConnectorsManager
    {
        public Dictionary<string, Dictionary<string, ConnectorInfo>> Connectors { get; internal set; }
        public EventHandler<(string, Dictionary<string, Dictionary<string, ConnectorInfo>>)>? NewProcess;
        public EventHandler<string>? RemovedProcess;

        protected string name;

        public ConnectorsManager(Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null, string name = nameof(ConnectorsManager))
        {
            this.name = name;
            Connectors = connectors ?? new Dictionary<string, Dictionary<string, ConnectorInfo>>();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;
    }
}
