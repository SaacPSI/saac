﻿using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace SAAC.Ollama
{
    public class OllamaConnector : IConsumerProducer<string, string>
    { 
        public Receiver<string> In { get; private set; }

        public Emitter<string> Out { get; private set; }

        private OllamaConectorConfiguration Configuration;
        private OllamaApiClient Ollama;
        private ConversationContextWithResponse? Context;
        private string name;

        public OllamaConnector(Pipeline parent, OllamaConectorConfiguration configuration, string name = nameof(OllamaConnector))
        {
            this.name = name;
            Configuration = configuration ?? new OllamaConectorConfiguration();
            In = parent.CreateReceiver<string>(parent, Process, $"{name}-In");
            Out = parent.CreateEmitter<string>(parent, $"{name}-Out");

            Ollama = new OllamaApiClient(Configuration.OllamaAddress);
            Context = null;
            LoadOllamaModel(Configuration.Model);
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(string message, Envelope envelope) 
        {
            _ = Task.Run(() => StreamOllama(message, envelope).ConfigureAwait(true));
        }

        private async Task<DateTime> StreamOllama(string message, Envelope envelope)
        {
            try 
            {
                Context = await Ollama.GetCompletion(message, Configuration.Model, Context);
                Out.Post(Context.Response, envelope.OriginatingTime);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message); 
            }
            return envelope.OriginatingTime;
        }

        private async Task<DateTime> LoadOllamaModel(string model)
        {
            await Ollama.PullModel(model, status => Console.WriteLine($"({status.Percent}%) {status.Status}"));
            return Out.Pipeline.GetCurrentTime();
        }
    }
}