using Microsoft.Psi;
using SAAC;
using SAAC.RendezVousPipelineServices;

namespace RendezVousPipelineServices
{
    public class ReplayPipeline : DatasetPipeline
    {
        public enum ReplayType { FullSpeed, RealTime, IntervalFullSpeed, IntervalRealTime };

        public ReplayPipelineConfiguration Configuration { get; private set; }

        private DatasetLoader loader;

        public ReplayPipeline(ReplayPipelineConfiguration configuration, string name = nameof(ReplayPipeline), LogStatus? log = null)
            : base(configuration, name, log)
        {
            Configuration = configuration ?? new ReplayPipelineConfiguration();
            loader = new DatasetLoader(Pipeline, Connectors, $"{name}-Loader");
            if(Dataset == null)
                throw new ArgumentNullException(nameof(Dataset));
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
            return loader.Load(Dataset, sessionName);
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
    }
}
