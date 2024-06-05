using Microsoft.Psi;
using Microsoft.Psi.Components;
using System.Net.Http;

namespace SAAC.RemoteConnectors
{
    /// <summary>
    /// Unreal communication component class throught HTTP request.
    /// See UnrealRemoteConnectorConfiguration and UnrealActionRequest class for details.
    /// </summary>
    public class UnrealRemoteConnector : IConsumerProducer<UnrealActionRequest, UnrealActionRequest>
    {
        /// <summary>
        /// Emitter of unreal request with response included.
        /// </summary>
        public Emitter<UnrealActionRequest> Out { get; private set; }

        /// <summary>
        /// Reciever in case it receives actions from another component in the pipeline.
        /// </summary>
        public Receiver<UnrealActionRequest> In { get; private set; }

        private UnrealRemoteConnectorConfiguration configuration;
        private HttpClient client;
        private string name;

        public UnrealRemoteConnector(Pipeline parent, UnrealRemoteConnectorConfiguration? configuration = null, string name = nameof(UnrealRemoteConnector))
        {
            this.name = name;
            this.configuration = configuration ?? new UnrealRemoteConnectorConfiguration();
            client = new HttpClient();

            Out = parent.CreateEmitter<UnrealActionRequest>(parent, $"{name}-Out");
            In = parent.CreateReceiver<UnrealActionRequest>(parent, Process, $"{name}-In");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(UnrealActionRequest request, Envelope envelope)
        {
            Send(request);
        }

        public void Send(UnrealActionRequest request)
        {
            switch (request.Method)
            {
                case UnrealActionRequest.EMethod.POST:
                    request.Response = client.PostAsync(configuration.Address, request.ToHttpContent()).Result.Content.ReadAsStringAsync().Result;
                    break;

                case UnrealActionRequest.EMethod.PUT:
                    request.Response = client.PutAsync(configuration.Address, request.ToStringContent()).Result.Content.ReadAsStringAsync().Result;
                    break;

                case UnrealActionRequest.EMethod.GET:
                    request.Response = client.GetStringAsync(configuration.Address + request.Path + request.Object).Result;
                    break;
            }
            Out.Post(request, DateTime.UtcNow);
        }
    }
}
