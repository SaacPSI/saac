// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    /// <summary>
    /// Configuration settings for a rendezvous-based distributed pipeline.
    /// </summary>
    public class RendezVousPipelineConfiguration : DatasetPipelineConfiguration
    {
        /// <summary>
        /// Gets or sets the rendezvous server host address.
        /// </summary>
        public string RendezVousHost { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the rendezvous server port.
        /// </summary>
        public int RendezVousPort { get; set; } = 13331;

        /// <summary>
        /// Gets or sets the diagnostics port.
        /// </summary>
        public int DiagnosticPort { get; set; } = 11512;

        /// <summary>
        /// Gets or sets the command port.
        /// </summary>
        public int CommandPort { get; set; } = 11511;

        /// <summary>
        /// Gets or sets the clock synchronization port.
        /// </summary>
        public int ClockPort { get; set; } = 11510;

        /// <summary>
        /// Gets or sets a value indicating whether to record incoming processes.
        /// </summary>
        public bool RecordIncomingProcess { get; set; } = true;

        /// <summary>
        /// Gets or sets the mapping of topic names to their data types.
        /// </summary>
        public Dictionary<string, Type> TopicsTypes = new Dictionary<string, Type>();

        /// <summary>
        /// Gets or sets the mapping of transformer names to their types.
        /// </summary>
        public Dictionary<string, Type> Transformers = new Dictionary<string, Type>();

        /// <summary>
        /// Gets or sets the mapping of data types to their serializers.
        /// </summary>
        public Dictionary<Type, IPsiFormat> TypesSerializers = new Dictionary<Type, IPsiFormat>();

        /// <summary>
        /// Gets or sets the list of topics that should not be stored.
        /// </summary>
        public List<string> NotStoredTopics = new List<string>();

        /// <summary>
        /// Gets or sets the command delegate for processing commands.
        /// </summary>
        public Helpers.PipeToMessage<(RendezVousPipeline.Command, string)>.Do? CommandDelegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="RendezVousPipelineConfiguration"/> class.
        /// </summary>
        /// <param name="addRegularSerializers">Whether to add default serializers for common types.</param>
        public RendezVousPipelineConfiguration(bool addRegularSerializers = true)
        {
            this.CommandDelegate = null;
            if (addRegularSerializers)
            {
                this.TypesSerializers.Add(typeof(bool), new PsiFormatBoolean());
                this.TypesSerializers.Add(typeof(char), new PsiFormatChar());
                this.TypesSerializers.Add(typeof(int), new PsiFormatInteger());
                this.TypesSerializers.Add(typeof(string), new PsiFormatString());
                this.TypesSerializers.Add(typeof(byte[]), new PsiFormatBytes());
                this.TypesSerializers.Add(typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>), new PsiFormatTupleOfVector());
                this.TypesSerializers.Add(typeof(System.Numerics.Matrix4x4), new PsiFormatMatrix4x4());
            }
        }

        /// <summary>
        /// Adds a topic with its format and optional transformer.
        /// </summary>
        /// <param name="topic">The topic name.</param>
        /// <param name="type">The data type.</param>
        /// <param name="format">The serialization format.</param>
        /// <param name="transformer">Optional transformer type.</param>
        public void AddTopicFormatAndTransformer(string topic, Type type, IPsiFormat format, Type? transformer = null)
        {
            if (!this.TopicsTypes.ContainsKey(topic))
            {
                this.TopicsTypes.Add(topic, type);
            }

            if (!this.TypesSerializers.ContainsKey(type))
            {
                this.TypesSerializers.Add(type, format);
            }

            if (transformer != null && !this.Transformers.ContainsKey(topic))
            {
                this.Transformers.Add(topic, transformer);
            }
        }
    }
}
