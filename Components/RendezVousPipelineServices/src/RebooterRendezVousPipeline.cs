using Microsoft.Psi;

namespace SAAC.PipelineServices
{
    public class RebooterRendezVousPipeline : RendezVousPipeline
    {
        private Dictionary<string, Dictionary<string, Dictionary<string, object>>> memory;

        public RebooterRendezVousPipeline(RendezVousPipelineConfiguration? configuration, string name = nameof(RebooterRendezVousPipeline), string? rendezVousServerAddress = null, LogStatus? log = null) 
            : base(configuration, name, rendezVousServerAddress, log)
        {
            memory = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
        }

        public Pipeline CreateRebootableSubpipeline(string name = "SaaCSubpipeline")
        {
            Pipeline p = this.CreateSubpipeline(name);
            p.PipelineCompleted += SubPipelineCompleted;
            p.PipelineRun += SubPipelineRun; 
            return p;
        }

        private void SubPipelineRun(object sender, PipelineRunEventArgs e)
        {
            Pipeline? p = sender as Pipeline;
            if (p == null)
                return;
            if (memory.ContainsKey(p.Name))
                foreach (var component in RebootableExtensions.GetElementsOfType<IRebootingComponent>(p))
                    if (memory[p.Name].ContainsKey(component.ToString()))
                        component.RestoreData(memory[p.Name][component.ToString()]);
        }

        private void SubPipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            Pipeline? p = sender as Pipeline;
            if (p == null)
                return;
            Dictionary<string, Dictionary<string, object>> data = new Dictionary<string, Dictionary<string, object>>();
            foreach (var component in RebootableExtensions.GetElementsOfType<IRebootingComponent>(p))
                data.Add(component.ToString(), component.StoreData());
            memory.Add(p.Name, data);
        }
    }
}
