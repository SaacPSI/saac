using Microsoft.Psi;

namespace SAAC.RendezVousPipelineServices
{
    public class RebooterRendezVousPipeline : RendezVousPipeline
    {
        private Dictionary<string, Dictionary<string, Dictionary<string, object>>> memory;

        public RebooterRendezVousPipeline(RendezVousPipelineConfiguration? configuration, string name = nameof(RebooterRendezVousPipeline), string? rendezVousServerAddress = null, LogStatus? log = null) 
            : base(configuration, name, rendezVousServerAddress, log)
        {
            memory = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
        }

        public RebootableSubPipeline CreateRebootableSubpipeline(string name = "SaaCSubpipeline")
        {
            RebootableSubPipeline p = new RebootableSubPipeline(base.pipeline, name);
            p.PipelineCompleted += SubPipelineCompleted;
            p.PipelineRun += SubPipelineRun; 
            return p;
        }

        private void SubPipelineRun(object sender, PipelineRunEventArgs e)
        {
            RebootableSubPipeline? p = sender as RebootableSubPipeline;
            if (p == null)
                return;
            if (memory.ContainsKey(p.Name))
                p.RestoreComponentsData(memory[p.Name]);
        }

        private void SubPipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            RebootableSubPipeline? p = sender as RebootableSubPipeline;
            if (p == null)
                return;
            memory.Add(p.Name, p.GetComponentsData());
        }
    }
}
