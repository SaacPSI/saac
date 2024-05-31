
using Microsoft.Psi.Interop.Serialization;
using System.IO;

namespace SAAC.RendezVousPipelineServices
{
    public class ClockSynchConfiguration
    {
        public int ClockPort = 11510;
        public string ClockProcessName = "ClockSynch";
    }

    public class RendezVousPipelineConfiguration
    {
        public string RendezVousHost = "localhost";
        public int RendezVousPort = 13331;
        public ClockSynchConfiguration? ClockConfiguration = new ClockSynchConfiguration();
        public bool Diagnostics = false;
        public bool Debug = false;
        public bool AutomaticPipelineRun = false;
        public bool UniqueSession = false;
        public string DatasetPath = "";
        public string DatasetName = "";
        public string SessionName = "";
        public Dictionary<string, Type> TopicsTypes = new Dictionary<string, Type>();
        public Dictionary<string, Type> Transformers = new Dictionary<string, Type>();
        public Dictionary<Type, IPsiFormat> TypesSerializers = new Dictionary<Type, IPsiFormat>();
        public List<string> NotStoredTopics = new List<string>();

        public RendezVousPipelineConfiguration(bool addRegularSerializers = true) 
        {
            if (addRegularSerializers)
            {
                TypesSerializers.Add(typeof(bool), new PsiFormatBoolean());
                TypesSerializers.Add(typeof(char), new PsiFormatChar());
                TypesSerializers.Add(typeof(int), new PsiFormatInt());
                TypesSerializers.Add(typeof(string), new PsiFormatString());
                TypesSerializers.Add(typeof(byte[]), new PsiFormatBytes());
                TypesSerializers.Add(typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>), new PsiFormatPositionAndOrientation());
            }
        }
    }
}
