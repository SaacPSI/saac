using Microsoft.Psi;
using Microsoft.Psi.Data;

namespace SAAC.PipelineServices
{
    public class ReplayPipeline : DatasetPipeline
    {
        public enum ReplayType { FullSpeed, RealTime, IntervalFullSpeed, IntervalRealTime };

        public ReplayPipelineConfiguration Configuration { get; private set; }

        private DatasetLoader loader;
        private SortedSet<string> ReadOnlyStores;

        public ReplayPipeline(ReplayPipelineConfiguration configuration, string name = nameof(ReplayPipeline), LogStatus? log = null)
            : base(configuration, name, log)
        {
            Configuration = configuration ?? new ReplayPipelineConfiguration();
            loader = new DatasetLoader(Pipeline, Connectors, $"{name}-Loader");
            ReadOnlyStores = new SortedSet<string>();
            if (Dataset == null)
                throw new ArgumentNullException(nameof(Dataset));
            else if(Configuration.DatasetBackup)
            {
                var filename = Dataset.Filename;
                Dataset.SaveAs(Dataset.Filename.Insert(Dataset.Filename.Length - 4, "_backup"));
                Dataset.Filename = filename;
            }
        }

        public override void AddNewProcessEvent(EventHandler<(string, Dictionary<string, Dictionary<string, ConnectorInfo>>)> handler)
        {
            base.AddNewProcessEvent(handler);
            loader.AddNewProcessEvent(handler);
        }

        public override void AddRemoveProcessEvent(EventHandler<string> handler)
        {
            base.AddRemoveProcessEvent(handler);
            loader.AddRemoveProcessEvent(handler);
        }

        public bool LoadDatasetAndConnectors(string? sessionName = null)
        {
            if (loader.Load(Dataset, sessionName))
            {
                this.Connectors = loader.Connectors;
                foreach (var session in Connectors)
                    foreach (var connectorPair in session.Value)
                        ReadOnlyStores.Add(connectorPair.Value.StoreName);
                return true;
            }
            return false;
        }

        protected override void RunAsync()
        {
            switch(Configuration.ReplayType)
            {
                case ReplayType.FullSpeed:
                    Pipeline.RunAsync(ReplayDescriptor.ReplayAll);
                    break;
                case ReplayType.RealTime:
                    Pipeline.RunAsync(ReplayDescriptor.ReplayAllRealTime);
                    break;
                case ReplayType.IntervalFullSpeed:
                    Pipeline.RunAsync(new ReplayDescriptor(Configuration.ReplayInterval, false)); 
                    break;
                case ReplayType.IntervalRealTime:
                    Pipeline.RunAsync(new ReplayDescriptor(Configuration.ReplayInterval, true));
                    break;
            }
        }

        public override void CreateStore<T>(Pipeline pipeline, Session session, string streamName, string storeName, IProducer<T> source)
        {
            if (ReadOnlyStores.Contains(storeName))
                throw new InvalidOperationException("Trying to write a Store that is readonly");
            base.CreateStore(pipeline, session, streamName, storeName, source);
        }

        public override (string, string) GetStoreName(string streamName, string processName, Session? session)
        {
            var names = base.GetStoreName(streamName, processName, session);
            if (ReadOnlyStores.Contains(names.Item2))
            {
                Log($"ReplayPipeline - GetStoreName : {names.Item2} already exist as Store Importer, switching name to {names.Item2}_{name}.");
                names.Item2 = $"{names.Item2}_{name}";
            }
            return names;
        }
    }
}
