using Microsoft.Psi;
using Microsoft.Psi.Components;
using System.Net.Http;

namespace RemoteConnectors
{
    /// <summary>
    /// Unreal communication component class throught HTTP request.
    /// See UnrealRemoteConnectorConfiguration and UnrealActionRequest class for details.
    /// </summary>
    public class UnrealRemoteConnector : IProducer<UnrealActionRequest>
    {
        /// <summary>
        /// Emitter of unreal request with response included.
        /// </summary>
        public Emitter<UnrealActionRequest> OutActionRequest { get; private set; }

        /// <summary>
        /// Connector in case it receives actions from another component in the pipeline.
        /// </summary>
        protected Connector<UnrealActionRequest> InActionRequestConnector;
        public Receiver<UnrealActionRequest> InActionRequest => InActionRequestConnector.In;

        public Emitter<UnrealActionRequest> Out => OutActionRequest;

        private UnrealRemoteConnectorConfiguration Configuration;
        private HttpClient Client;

        public UnrealRemoteConnector(Pipeline parent, UnrealRemoteConnectorConfiguration? configuration = null)
        {
            Configuration = configuration ?? new UnrealRemoteConnectorConfiguration();
            Client = new HttpClient();

            OutActionRequest = parent.CreateEmitter<UnrealActionRequest>(this, nameof(OutActionRequest));
            InActionRequestConnector = parent.CreateConnector<UnrealActionRequest>(nameof(InActionRequestConnector));

            InActionRequestConnector.Do(Process);
        }

        private void Process(UnrealActionRequest request, Envelope envelope)
        {
            Send(request);
        }

        public void Send(UnrealActionRequest request)
        {
            switch (request.Method)
            {
                case UnrealActionRequest.EMethod.POST:
                    request.Response = Client.PostAsync(Configuration.Address, request.ToHttpContent()).Result.Content.ReadAsStringAsync().Result;
                    break;

                case UnrealActionRequest.EMethod.PUT:
                    request.Response = Client.PutAsync(Configuration.Address, request.ToStringContent()).Result.Content.ReadAsStringAsync().Result;
                    break;

                case UnrealActionRequest.EMethod.GET:
                    request.Response = Client.GetStringAsync(Configuration.Address + request.Path + request.Object).Result;
                    break;
            }
            OutActionRequest.Post(request, DateTime.UtcNow);
        }
    }
}
