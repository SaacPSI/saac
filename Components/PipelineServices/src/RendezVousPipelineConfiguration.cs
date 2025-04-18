﻿namespace SAAC.PipelineServices
{
    public class RendezVousPipelineConfiguration : DatasetPipelineConfiguration
    {
        public string RendezVousHost = "localhost";
        public int RendezVousPort = 13331;
        public int DiagnosticPort = 11512;
        public int CommandPort = 11511;
        public int ClockPort = 11510;
        public bool RecordIncomingProcess = true;
        public Dictionary<string, Type> TopicsTypes = new Dictionary<string, Type>();
        public Dictionary<string, Type> Transformers = new Dictionary<string, Type>();
        public Dictionary<Type, IPsiFormat> TypesSerializers = new Dictionary<Type, IPsiFormat>();
        public List<string> NotStoredTopics = new List<string>();
        public Helpers.PipeToMessage<(RendezVousPipeline.Command, string)>.Do? CommandDelegate;

        public RendezVousPipelineConfiguration(bool addRegularSerializers = true) 
        {
            CommandDelegate = null;
            if (addRegularSerializers)
            {
                TypesSerializers.Add(typeof(bool), new PsiFormatBoolean());
                TypesSerializers.Add(typeof(char), new PsiFormatChar());
                TypesSerializers.Add(typeof(int), new PsiFormatInteger());
                TypesSerializers.Add(typeof(string), new PsiFormatString());
                TypesSerializers.Add(typeof(byte[]), new PsiFormatBytes());
                TypesSerializers.Add(typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>), new PsiFormatTupleOfVector());
                TypesSerializers.Add(typeof(System.Numerics.Matrix4x4), new PsiFormatMatrix4x4());
            }
        }

        public void AddTopicFormatAndTransformer(string topic, Type type, IPsiFormat format, Type? transformer = null)
        {
            if (!TopicsTypes.ContainsKey(topic))
                TopicsTypes.Add(topic, type);
            if (!TypesSerializers.ContainsKey(type))
                TypesSerializers.Add(type, format);
            if (transformer != null && !Transformers.ContainsKey(topic))
                Transformers.Add(topic, transformer);
        }
    }
}
