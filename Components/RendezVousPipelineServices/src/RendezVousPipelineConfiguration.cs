
using Microsoft.Psi.Interop.Serialization;
using System.IO;

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

        public RendezVousPipelineConfiguration() 
        {
            TypesSerializers.Add(typeof(bool), new PsiFormatBoolean());
            TypesSerializers.Add(typeof(char), new PsiFormaChar());
            TypesSerializers.Add(typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>), new PsiFormatPositionAndOrientation());
        }
    }
}
