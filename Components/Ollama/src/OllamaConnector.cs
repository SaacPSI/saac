// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Ollama
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using OllamaSharp;

    /// <summary>
    /// Component that connects to an Ollama server for LLM-based text processing.
    /// </summary>
    public class OllamaConnector : IConsumerProducer<string, string>
    {
        private OllamaConectorConfiguration configuration;
        private OllamaApiClient ollama;
        private Chat chat;
        private string name;
        private bool isChat;

        /// <summary>
        /// Initializes a new instance of the <see cref="OllamaConnector"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="isChatting">Whether to use chat mode.</param>
        /// <param name="name">The name of the component.</param>
        public OllamaConnector(Pipeline parent, OllamaConectorConfiguration configuration, bool isChatting, string name = nameof(OllamaConnector))
        {
            this.isChat = isChatting;
            this.name = name;
            this.configuration = configuration ?? new OllamaConectorConfiguration();
            this.In = parent.CreateReceiver<string>(parent, this.Process, $"{name}-In");
            this.Out = parent.CreateEmitter<string>(parent, $"{name}-Out");

            this.ollama = new OllamaApiClient(this.configuration.OllamaAddress);
            this.ollama.SelectedModel = this.configuration.Model;
            this.chat = new Chat(this.ollama);
        }

        /// <summary>
        /// Gets the input receiver.
        /// </summary>
        public Receiver<string> In { get; }

        /// <summary>
        /// Gets the output emitter.
        /// </summary>
        public Emitter<string> Out { get; }

        /// <summary>
        /// Gets the list of available models from the Ollama server.
        /// </summary>
        /// <param name="address">The Ollama server address.</param>
        /// <returns>List of available model names.</returns>
        public static List<string> GetAvailableModel(Uri address)
        {
            var ollama = new OllamaApiClient(address);
            List<string> availableModels = new List<string>();
            var result = ollama.ListLocalModelsAsync().Result;
            foreach (var model in result)
            {
                availableModels.Add(model.Name);
            }

            return availableModels;
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(string message, Envelope envelope)
        {
            if (this.isChat)
            {
                _ = Task.Run(() => this.StreamChatToOllama(message, envelope).ConfigureAwait(true));
            }
            else
            {
                _ = Task.Run(() => this.StreamSingleToOllama(message, envelope).ConfigureAwait(true));
            }
        }

        private async Task<DateTime> StreamChatToOllama(string message, Envelope envelope)
        {
            try
            {
                string response = string.Empty;
                await foreach (var answerToken in this.chat.SendAsync(message))
                {
                    response += answerToken;
                }

                this.Out.Post(response, envelope.OriginatingTime);
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
                string response = string.Empty;
                await foreach (var answerToken in this.ollama.GenerateAsync(message))
                {
                    response += answerToken;
                }

                this.Out.Post(response, envelope.OriginatingTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return envelope.OriginatingTime;
        }
    }
}
