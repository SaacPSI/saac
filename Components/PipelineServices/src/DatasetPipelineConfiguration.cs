
namespace SAAC.PipelineServices
{
    public class DatasetPipelineConfiguration
    {
        public DatasetPipeline.DiagnosticsMode Diagnostics = DatasetPipeline.DiagnosticsMode.Off;
        public bool Debug = false;
        public bool AutomaticPipelineRun = false;
        public DatasetPipeline.SessionNamingMode SessionMode = DatasetPipeline.SessionNamingMode.Increment;
        public DatasetPipeline.StoreMode StoreMode = DatasetPipeline.StoreMode.Independant;
        public string DatasetPath = "";
        public string DatasetName = "";
        public string SessionName = "";
        public Dictionary<string, string> StreamToStore = new Dictionary<string, string>();
    }
}
