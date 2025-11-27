using Newtonsoft.Json;
using System.Net.Http;

namespace SAAC.RemoteConnectors
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
        public UnrealActionRequest(string _object, string path, string functionName = "", string parameters = "")
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
}
