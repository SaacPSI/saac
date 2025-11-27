using Microsoft.Psi;
using Microsoft.Psi.Components;
using OllamaSharp;

namespace SAAC.Ollama
{
    public class OllamaConnector : IConsumerProducer<string, string>
    { 
        public Receiver<string> In { get; }

        public Emitter<string> Out { get; }

        private OllamaConectorConfiguration Configuration;
        private OllamaApiClient Ollama;
        private Chat Chat;
        private string name;
        private bool isChat;

        public OllamaConnector(Pipeline parent, OllamaConectorConfiguration configuration, bool isChatting, string name = nameof(OllamaConnector))
        {
            isChat = isChatting;
            this.name = name;
            Configuration = configuration ?? new OllamaConectorConfiguration();
            In = parent.CreateReceiver<string>(parent, Process, $"{name}-In");
            Out = parent.CreateEmitter<string>(parent, $"{name}-Out");

            Ollama = new OllamaApiClient(Configuration.OllamaAddress);
            Ollama.SelectedModel = Configuration.Model;
            Chat = new Chat(Ollama);
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        static public List<string> GetAvailableModel(Uri address)
        {
            var ollama = new OllamaApiClient(address);
            List<string> availableModels = new List<string>();
            var result = ollama.ListLocalModelsAsync().Result;
            foreach (var model in result)
                availableModels.Add(model.Name);
            return availableModels;
        }

        private void Process(string message, Envelope envelope) 
        {
            if (isChat)
                _ = Task.Run(() => StreamChatToOllama(message, envelope).ConfigureAwait(true));
            else
                _ = Task.Run(() => StreamSingleToOllama(message, envelope).ConfigureAwait(true));
        }
        private async Task<DateTime> StreamChatToOllama(string message, Envelope envelope)
        {
            try
            {
                string response = "";
                await foreach (var answerToken in Chat.SendAsync(message))
                    response += answerToken;
                Out.Post(response, envelope.OriginatingTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return envelope.OriginatingTime;
        }

        private async Task<DateTime> StreamSingleToOllama(string message, Envelope envelope)
        {
            try 
            {
                string response = "";
                await foreach (var answerToken in Ollama.GenerateAsync(message))
                    response += answerToken;
                Out.Post(response, envelope.OriginatingTime);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message); 
            }
            return envelope.OriginatingTime;
        }
    }
}