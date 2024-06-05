namespace SAAC.Ollama
{
    public class OllamaConectorConfiguration
    {
        public Uri OllamaAddress { get; set; } = new Uri("http://10.44.193.232:11434");
        public string Model { get; set; } = "llama2";
    }
}