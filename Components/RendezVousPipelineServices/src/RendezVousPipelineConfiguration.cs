
namespace SAAC.RendezVousPipelineServices
{
    public class RendezVousPipelineConfiguration
    {
        public string RendezVousHost = "localhost";
        public int RendezVousPort = 13331;
        public int ClockPort = 11510;
        public string ClockProcessName = "ClockSynch";
        public bool Diagnostics = false;
        public bool Debug = false;
        public string DatasetPath = "";
        public string DatasetName = "";
        public string SessionName = "";
        public Dictionary<string, Type> TopicsTypes = new Dictionary<string, Type>();
        public Dictionary<Type, IPsiFormat> TypesSerializers = new Dictionary<Type, IPsiFormat>();
    }
}
