using Microsoft.Psi;
using Microsoft.Psi.Data;
using SAAC;
using SAAC.RendezVousPipelineServices;
using System.Linq;

namespace RendezVousPipelineServices
{
    public class ReplayPipeline : DatasetPipeline
    {
        public enum ReplayType { FullSpeed, RealTime, IntervalFullSpeed, IntervalRealTime };

        public ReplayPipelineConfiguration Configuration { get; private set; }

        private DatasetLoader loader;
        private SortedSet<string> ReadOnlySessionsAndStores;

        public ReplayPipeline(ReplayPipelineConfiguration configuration, string name = nameof(ReplayPipeline), LogStatus? log = null)
            : base(configuration, name, log)
        {
            Configuration = configuration ?? new ReplayPipelineConfiguration();
            loader = new DatasetLoader(Pipeline, Connectors, $"{name}-Loader");
            if (Dataset == null)
                throw new ArgumentNullException(nameof(Dataset));
            else if(Configuration.NewDataset)
                Dataset.SaveAs($@"{StorePath}\{Dataset.Name}_replayed.pds");
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
                if (Configuration.ReadOnlySessionsAndStores)
                { 
                    foreach (var session in Connectors)
                    {
                        foreach (var connectorPair in session.Value)
                        {
                            ReadOnlySessionsAndStores.Add(connectorPair.Value.StoreName);
                            ReadOnlySessionsAndStores.Add(connectorPair.Value.SessionName);
                        }
                    }
                }
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
            if (ReadOnlySessionsAndStores.Contains(storeName) || ReadOnlySessionsAndStores.Contains(session.Name))
                throw new InvalidOperationException("Trying to write a Store or a Session that is readonly");
            base.CreateStore(pipeline, session, streamName, storeName, source);
        }
    }
}
