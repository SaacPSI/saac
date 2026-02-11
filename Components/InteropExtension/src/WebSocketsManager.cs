// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.WebSockets;
    using System.Threading;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that handles the connection of WebSockets for both server and client modes.
    /// </summary>
    public class WebSocketsManager : ISourceComponent, IDisposable
    {
        /// <summary>
        /// HttpListener instance that manage incoming HTTP requests and upgrades them to WebSocket connections when appropriate.
        /// </summary>
        protected HttpListener httpListener;

        /// <summary>
        /// Token source for managing cancellation of the listening thread when the component is stopped or disposed.
        /// </summary>
        protected CancellationTokenSource token;

        private readonly bool isServer;
        private readonly bool isRestrictedToSecure;
        private Dictionary<string, Dictionary<string, WebSocket>> websocketByClients;
        private Thread listeningThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketsManager"/> class.
        /// </summary>
        /// <param name="isServer">Whether this instance runs as a server.</param>
        /// <param name="prefixAddress">The list of addresses to listen to.</param>
        /// <param name="restrictToSecure">Boolean to force the use of SSL websocket.</param>
        public WebSocketsManager(bool isServer, List<string> prefixAddress, bool restrictToSecure = false)
        {
            this.isServer = isServer;
            this.isRestrictedToSecure = restrictToSecure;
            this.token = new CancellationTokenSource();
            this.websocketByClients = new Dictionary<string, Dictionary<string, WebSocket>>();
            this.OnNewWebSocketConnectedHandler = null;

            if (isServer)
            {
                this.InitialiseHTTPListener(prefixAddress);
            }
        }

        /// <summary>
        /// Gets or sets the handler of WebSockets connection event.
        /// </summary>
        public EventHandler<(string, string, Uri)>? OnNewWebSocketConnectedHandler { get; set; }

        /// <summary>
        /// Creates WebSocketSource to retrieve messages from an existing WebSocket.
        /// </summary>
        /// <typeparam name="T">The type of the messages.</typeparam>
        /// <param name="pipeline">The pipeline to run the PSI components.</param>
        /// <param name="deserializer">The deserializer of incoming data.</param>
        /// <param name="remoteName">The hostname of the client.</param>
        /// <param name="topic">The topic of the WebSocket.</param>
        /// <param name="useSourceTime">Whether to use the source timestamp.</param>
        /// <param name="name">The name of the component.</param>
        /// <param name="port">The port for the WebSocket.</param>
        /// <returns>The created WebSocketSource if the WebSocket exists, otherwise null.</returns>
        public WebSocketSource<T>? CreateWebsocketSource<T>(Pipeline pipeline, Serialization.IFormatDeserializer deserializer, string remoteName, string topic, bool useSourceTime = true, string name = nameof(WebSocketSource<T>), int port = 8080)
        {
            if (!this.GetWebsocket(remoteName, topic, out WebSocket websocket))
            {
                if (!this.CreateWebsocket(remoteName, port, topic, out websocket))
                {
                    return null;
                }
            }

            return new WebSocketSource<T>(pipeline, websocket, deserializer, useSourceTime, 0, name);
        }

        /// <summary>
        /// Creates WebSocketWriter to send messages to a new WebSocket.
        /// </summary>
        /// <typeparam name="T">The type of the messages.</typeparam>
        /// <param name="pipeline">The pipeline to run the PSI components.</param>
        /// <param name="serializer">The serializer of outgoing data.</param>
        /// <param name="remoteName">The hostname of the client.</param>
        /// <param name="topic">The topic of the WebSocket.</param>
        /// <param name="name">The name of the component.</param>
        /// <param name="port">The port for the WebSocket.</param>
        /// <returns>The created WebSocketWriter if the WebSocket exists, otherwise null.</returns>
        public WebSocketWriter<T>? CreateWebsocketWriter<T>(Pipeline pipeline, Serialization.IFormatSerializer serializer, string remoteName, string topic, string name = nameof(WebSocketSource<T>), int port = 8080)
        {
            if (!this.GetWebsocket(remoteName, topic, out WebSocket websocket))
            {
                if (!this.CreateWebsocket(remoteName, port, topic, out websocket))
                {
                    return null;
                }
            }

            return new WebSocketWriter<T>(pipeline, websocket, serializer, name);
        }

        /// <summary>
        /// Creates WebSocketSource to retrieve messages from a new WebSocket.
        /// </summary>
        /// <typeparam name="T">The type of the messages.</typeparam>
        /// <param name="pipeline">The pipeline to run the PSI components.</param>
        /// <param name="deserializer">The deserializer of incoming data.</param>
        /// <param name="remoteName">The hostname of the client.</param>
        /// <param name="topic">The topic of the WebSocket.</param>
        /// <param name="useSourceTime">Whether to use the source timestamp.</param>
        /// <param name="name">The name of the component.</param>
        /// <returns>The created WebSocketSource if the WebSocket exists, otherwise null.</returns>
        public WebSocketSource<T>? ConnectWebsocketSource<T>(Pipeline pipeline, Serialization.IFormatDeserializer deserializer, string remoteName, string topic, bool useSourceTime, string name = nameof(WebSocketSource<T>))
        {
            if (!this.GetWebsocket(remoteName, topic, out WebSocket websocket))
            {
                return null;
            }

            return new WebSocketSource<T>(pipeline, websocket, deserializer, useSourceTime, 0, name);
        }

        /// <summary>
        /// Creates WebSocketWriter to send messages to an existing WebSocket.
        /// </summary>
        /// <typeparam name="T">The type of the messages.</typeparam>
        /// <param name="pipeline">The pipeline to run the PSI components.</param>
        /// <param name="serializer">The serializer of outgoing data.</param>
        /// <param name="remoteName">The hostname of the client.</param>
        /// <param name="topic">The topic of the WebSocket.</param>
        /// <param name="name">The name of the component.</param>
        /// <returns>The created WebSocketWriter if the WebSocket exists, otherwise null.</returns>
        public WebSocketWriter<T>? ConnectWebsocketWriter<T>(Pipeline pipeline, Serialization.IFormatSerializer serializer, string remoteName, string topic, string name = nameof(WebSocketSource<T>))
        {
            if (!this.GetWebsocket(remoteName, topic, out WebSocket websocket))
            {
                return null;
            }

            return new WebSocketWriter<T>(pipeline, websocket, serializer, name);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.isServer)
            {
                this.token.Cancel();
                this.httpListener.Stop();
                this.listeningThread.Join();
            }

            foreach (var client in this.websocketByClients)
            {
                foreach (var ws in client.Value)
                {
                    if (ws.Value.State == WebSocketState.Open)
                    {
                        ws.Value.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None);
                    }
                }
            }

            this.websocketByClients.Clear();
        }

        /// <inheritdoc/>
        public virtual void Start(Action<DateTime> notifyCompletionTime)
        {
            if (this.isServer)
            {
                this.httpListener.Start();
                this.listeningThread = new Thread(this.ProcessContexts);
                this.listeningThread.Start();
            }

            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.Dispose();
            notifyCompleted();
        }

        /// <summary>
        /// Gets an existing WebSocket by remote name and topic.
        /// </summary>
        /// <param name="remoteName">The hostname of the client.</param>
        /// <param name="topic">The topic of the WebSocket.</param>
        /// <param name="webSocket">The WebSocket if found.</param>
        /// <returns>True if the WebSocket was found; otherwise false.</returns>
        protected bool GetWebsocket(string remoteName, string topic, out WebSocket webSocket)
        {
            webSocket = default;
            if (!this.websocketByClients.ContainsKey(remoteName))
            {
                return false;
            }

            if (!this.websocketByClients[remoteName].ContainsKey(topic))
            {
                return false;
            }

            webSocket = this.websocketByClients[remoteName][topic];
            return true;
        }

        /// <summary>
        /// Creates a new WebSocket connection as a client.
        /// </summary>
        /// <param name="remoteName">The hostname of the client.</param>
        /// <param name="port">The port for the WebSocket.</param>
        /// <param name="topic">The topic of the WebSocket.</param>
        /// <param name="webSocket">The created WebSocket if successful.</param>
        /// <returns>True if the WebSocket was created successfully; otherwise false.</returns>
        protected virtual bool CreateWebsocket(string remoteName, int port, string topic, out WebSocket webSocket)
        {
            try
            {
                var websocket = new ClientWebSocket();
                CancellationTokenSource cts = new CancellationTokenSource();

                if (this.isRestrictedToSecure)
                {
                    websocket.Options.ClientCertificates = new System.Security.Cryptography.X509Certificates.X509CertificateCollection();
                    websocket.ConnectAsync(new Uri($"wss://{remoteName}:{port}/ws/{topic}"), cts.Token);
                }
                else
                {
                    websocket.ConnectAsync(new Uri($"ws://{remoteName}:{port}/ws/{topic}"), cts.Token);
                }

                this.RegisterWebSocket(remoteName, topic, websocket);
                webSocket = websocket;
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"WebSocketsManager CreateWebsocket Exception: {ex.Message}");
            }

            webSocket = default;
            return false;
        }

        /// <summary>
        /// Processes incoming HTTP contexts and accepts WebSocket connections.
        /// </summary>
        protected virtual async void ProcessContexts()
        {
            while (!this.token.IsCancellationRequested)
            {
                var result = await this.httpListener.GetContextAsync();
                if (result != null)
                {
                    this.AcceptWebsocketClients(result);
                }
            }
        }

        /// <summary>
        /// Registers a WebSocket for a specific remote name and topic.
        /// </summary>
        /// <param name="remoteName">The hostname of the client.</param>
        /// <param name="topic">The topic of the WebSocket.</param>
        /// <param name="webSocket">The WebSocket to register.</param>
        /// <returns>True if the WebSocket was registered successfully; otherwise false.</returns>
        protected bool RegisterWebSocket(string remoteName, string topic, WebSocket webSocket)
        {
            lock (this.websocketByClients)
            {
                if (!this.websocketByClients.ContainsKey(remoteName))
                {
                    this.websocketByClients[remoteName] = new Dictionary<string, WebSocket>();
                }

                if (this.websocketByClients[remoteName].ContainsKey(topic))
                {
                    return false;
                }

                this.websocketByClients[remoteName][topic] = webSocket;
                return true;
            }
        }

        /// <summary>
        /// Initializes the HTTP listener with the specified addresses.
        /// </summary>
        /// <param name="address">The list of addresses to listen on.</param>
        protected void InitialiseHTTPListener(List<string> address)
        {
            this.httpListener = new HttpListener();
            foreach (var addr in address)
            {
                this.httpListener.Prefixes.Add(addr);
            }
        }

        /// <summary>
        /// Accepts WebSocket client connections from HTTP contexts.
        /// </summary>
        /// <param name="context">The HTTP listener context.</param>
        protected virtual async void AcceptWebsocketClients(HttpListenerContext context)
        {
            if (context.Request.IsWebSocketRequest && (!this.isRestrictedToSecure || context.Request.IsSecureConnection))
            {
                var wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
                WebSocket webSocket = wsContext.WebSocket;
                string topic = context.Request.Url.AbsolutePath.Substring(4); // removing "/ws/"
                if (this.RegisterWebSocket(this.GetNameForHost(context.Request.Url), topic, webSocket))
                {
                    this.OnNewWebSocketConnectedHandler?.Invoke(this, (this.GetNameForHost(context.Request.Url), topic, context.Request.Url));
                }
            }
        }

        /// <summary>
        /// Gets the name for a host from the URI, checking query parameters first.
        /// </summary>
        /// <param name="hostUri">The host URI.</param>
        /// <returns>The name from the query parameter if present, otherwise the host name.</returns>
        private string GetNameForHost(Uri hostUri)
        {
            if (!string.IsNullOrEmpty(hostUri.Query))
            {
                // Parse query string parameters
                var queryParams = System.Web.HttpUtility.ParseQueryString(hostUri.Query);
                var nameParam = queryParams["name"];
                if (!string.IsNullOrEmpty(nameParam))
                {
                    return nameParam;
                }
            }

            return hostUri.Host;
        }
    }
}
