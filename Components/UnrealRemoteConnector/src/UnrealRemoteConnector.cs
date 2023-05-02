using Microsoft.Psi;
using Microsoft.Psi.Components;
using Newtonsoft.Json;
using System.Net.Http;

namespace RemoteConnectors
{
    public class UnrealActionRequest
    {      
        /// <summary>
        /// Enum for injection solution.
        /// </summary>
        public enum EMethod { GET, PUT, POST };

        /// <summary>
        /// Get or set the injection method.
        /// </summary>
        public EMethod Method { get; set; } = EMethod.PUT;

        /// <summary>
        /// Get or set the object to get or perform the action.
        /// </summary>
        public string Object { get; set; }

        /// <summary>
        /// Get or set the path of the object to get or perform the action.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the function (to be) called.
        /// </summary>
        public string FunctionName { get; set; } = "";

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        public string Parameters { get; set; } = "";

        /// <summary>
        /// Gets or sets the reponse.
        /// </summary>
        public string Response { get; set; } = "";

        /// <summary>
        /// Constructors.
        /// </summary>
        public UnrealActionRequest(string _object, string path, string functionName="", string parameters="") 
        {
            Object = _object;
            Path = path;
            FunctionName = functionName;
            Parameters = parameters;
        }

        public HttpContent ToHttpContent()
        {
            var values = new Dictionary<string, string>
            {
                { "objectPath", Path + Object },
                { "functionName", FunctionName },
                { "parameters", Parameters }
            };
            HttpContent content = new FormUrlEncodedContent(values); 
            content.Headers.ContentType.MediaType = "application/json";
            content.Headers.ContentType.CharSet = "";
            return content;
        }

        public StringContent ToStringContent()
        {
            StringContent content = new StringContent(JsonConvert.SerializeObject(new JSONObject(Path + Object, FunctionName, Parameters)));
            content.Headers.ContentType.MediaType = "application/json";
            content.Headers.ContentType.CharSet = "";
            return content; 
        }

        private class JSONObject
        {
            public string objectPath;
            public string functionName;
            public string parameters;

            public JSONObject(string objectPath, string functionName, string parameters)
            {
                this.objectPath = objectPath;
                this.functionName = functionName;
                this.parameters = parameters;
            }
        }
    }


    public class UnrealRemoteConnector : Subpipeline
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

        private UnrealRemoteConnectorConfiguration Configuration;
        private HttpClient Client;

        public UnrealRemoteConnector(Pipeline parent, UnrealRemoteConnectorConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
          : base(parent, name, defaultDeliveryPolicy)
        {
            Configuration = configuration ?? new UnrealRemoteConnectorConfiguration();
            Client = new HttpClient();

            OutActionRequest = CreateEmitter<UnrealActionRequest>(parent, nameof(OutActionRequest));
            InActionRequestConnector = CreateInputConnectorFrom<UnrealActionRequest>(parent, nameof(InActionRequestConnector));

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
