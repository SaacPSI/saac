using Microsoft.Psi;

namespace SAAC.RendezVousPipelineServices
{
    public class RebootableSubPipeline : Subpipeline
    {
        public RebootableSubPipeline(Pipeline parent, string name)
            : base(parent, name)
        {}

        public Dictionary<string, Dictionary<string, object>> GetComponentsData() 
        {
            Dictionary<string, Dictionary<string, object>> data = new Dictionary<string, Dictionary<string, object>>();
            foreach (var component in RebootableExtensions.GetElementsOfType<IRebootingComponent>(this))
                data.Add(component.ToString(), component.StoreData());
            return data;
        }

       public void RestoreComponentsData(Dictionary<string, Dictionary<string, object>> data)
       {        
            foreach (var component in RebootableExtensions.GetElementsOfType<IRebootingComponent>(this))
                if(data.ContainsKey(component.ToString()))
                    component.RestoreData(data[component.ToString()]);
       }
    }
}
