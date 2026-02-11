// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    using System.Collections.Generic;
    using System.Net.Http;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents an action request to be sent to Unreal Engine via HTTP.
    /// See UnrealRemoteConnectorConfiguration and UnrealRemoteConnector class for details.
    /// </summary>
    public class UnrealActionRequest
    {
        /// <summary>
        /// Enum for HTTP method used in the request.
        /// </summary>
        public enum EMethod
        {
            /// <summary>
            /// HTTP GET method.
            /// </summary>
            GET,

            /// <summary>
            /// HTTP PUT method.
            /// </summary>
            PUT,

            /// <summary>
            /// HTTP POST method.
            /// </summary>
            POST,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnrealActionRequest"/> class.
        /// </summary>
        /// <param name="object">The target object name.</param>
        /// <param name="path">The path to the object.</param>
        /// <param name="functionName">The function name to call.</param>
        /// <param name="parameters">The parameters for the function.</param>
        public UnrealActionRequest(string @object, string path, string functionName = "", string parameters = "")
        {
            this.Object = @object;
            this.Path = path;
            this.FunctionName = functionName;
            this.Parameters = parameters;
        }

        /// <summary>
        /// Gets or sets the HTTP method to use for the request.
        /// </summary>
        public EMethod Method { get; set; } = EMethod.PUT;

        /// <summary>
        /// Gets or sets the target object name.
        /// </summary>
        public string Object { get; set; }

        /// <summary>
        /// Gets or sets the path to the object.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the function name to call.
        /// </summary>
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameters for the function call.
        /// </summary>
        public string Parameters { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the response from the request.
        /// </summary>
        public string Response { get; set; } = string.Empty;

        /// <summary>
        /// Converts this request to form URL encoded HTTP content.
        /// </summary>
        /// <returns>The HTTP content for form submission.</returns>
        public HttpContent ToHttpContent()
        {
            var values = new Dictionary<string, string>
            {
                { "objectPath", this.Path + this.Object },
                { "functionName", this.FunctionName },
                { "parameters", this.Parameters },
            };
            HttpContent content = new FormUrlEncodedContent(values);
            content.Headers.ContentType.MediaType = "application/json";
            content.Headers.ContentType.CharSet = string.Empty;
            return content;
        }

        /// <summary>
        /// Converts this request to JSON string content.
        /// </summary>
        /// <returns>The string content with JSON serialization.</returns>
        public StringContent ToStringContent()
        {
            StringContent content = new StringContent(JsonConvert.SerializeObject(new JSONObject(this.Path + this.Object, this.FunctionName, this.Parameters)));
            content.Headers.ContentType.MediaType = "application/json";
            content.Headers.ContentType.CharSet = string.Empty;
            return content;
        }

        private class JSONObject
        {
            public string ObjectPath;
            public string FunctionName;
            public string Parameters;

            public JSONObject(string objectPath, string functionName, string parameters)
            {
                this.ObjectPath = objectPath;
                this.FunctionName = functionName;
                this.Parameters = parameters;
            }
        }
    }
}
