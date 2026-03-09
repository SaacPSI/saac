namespace CASPERAnalysis
{
    /// <summary>
    /// Represents a topic configuration from experiment.json
    /// </summary>
    public class TopicConfig
    {
        public string topic { get; set; } = "";
        public string type { get; set; } = "";
        public string classFormat { get; set; } = "";
        public string streamToStore { get; set; } = "";
    }
}

