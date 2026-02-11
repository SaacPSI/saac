// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    using System.Net.Http;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Unreal communication component class throught HTTP request.
    /// See UnrealRemoteConnectorConfiguration and UnrealActionRequest class for details.
    /// </summary>
    public class UnrealRemoteConnector : IConsumerProducer<UnrealActionRequest, UnrealActionRequest>
    {
        private UnrealRemoteConnectorConfiguration configuration;
        private HttpClient client;
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnrealRemoteConnector"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="name">The name of the component.</param>
        public UnrealRemoteConnector(Pipeline parent, UnrealRemoteConnectorConfiguration? configuration = null, string name = nameof(UnrealRemoteConnector))
        {
            this.name = name;
            this.configuration = configuration ?? new UnrealRemoteConnectorConfiguration();
            this.client = new HttpClient();

            this.Out = parent.CreateEmitter<UnrealActionRequest>(parent, $"{name}-Out");
            this.In = parent.CreateReceiver<UnrealActionRequest>(parent, this.Process, $"{name}-In");
        }

        /// <summary>
        /// Gets the emitter of unreal request with response included.
        /// </summary>
        public Emitter<UnrealActionRequest> Out { get; private set; }

        /// <summary>
        /// Gets the receiver in case it receives actions from another component in the pipeline.
        /// </summary>
        public Receiver<UnrealActionRequest> In { get; private set; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Sends an Unreal action request.
        /// </summary>
        /// <param name="request">The request to send.</param>
        public void Send(UnrealActionRequest request)
        {
            switch (request.Method)
            {
                case UnrealActionRequest.EMethod.POST:
                    request.Response = this.client.PostAsync(this.configuration.Address, request.ToHttpContent()).Result.Content.ReadAsStringAsync().Result;
                    break;

                case UnrealActionRequest.EMethod.PUT:
                    request.Response = this.client.PutAsync(this.configuration.Address, request.ToStringContent()).Result.Content.ReadAsStringAsync().Result;
                    break;

                case UnrealActionRequest.EMethod.GET:
                    request.Response = this.client.GetStringAsync(this.configuration.Address + request.Path + request.Object).Result;
                    break;
            }

            this.Out.Post(request, DateTime.UtcNow);
        }

        private void Process(UnrealActionRequest request, Envelope envelope)
        {
            this.Send(request);
        }
    }
}
