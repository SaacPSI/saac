// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using Microsoft.Psi.Components;
    using System;
    using System.Net;
    using System.Threading;
    using System.Net.WebSockets;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Component that handle the connection of websockets.
    /// </summary>
    public class WebSocketsManager : ISourceComponent, IDisposable
    {
        private bool isServer;
        private bool isRestrictedToSecure;
        protected HttpListener httpListener;
        protected Thread listeningThread;
        protected CancellationTokenSource token;
        private Dictionary<string, Dictionary<string, WebSocket>> websocketByClients { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketsManager"/> class.
        /// </summary>
        /// <param name="restrictToSecure">Boolean to force the use of ssl websocket.</param>
        /// <param name="prefixAddress">The address to listen to.</param>
        public WebSocketsManager(bool isServer, List<string> prefixAddress, bool restrictToSecure = false)
        {
            this.isServer = isServer;
            this.isRestrictedToSecure = restrictToSecure;
            this.token = new CancellationTokenSource();
            this.websocketByClients = new Dictionary<string, Dictionary<string, WebSocket>>();
            this.OnNewWebSocketConnectedHandler = null;

            if (isServer)
                InitialiseHTTPListener(prefixAddress);
        }

        /// <summary>
        /// Gets or sets the handler of WebSockets connection event.
        /// </summary>
        public EventHandler<(string, string)>? OnNewWebSocketConnectedHandler { get; set; }

        /// <summary>
        /// Create WebSocketSource, to retrieve message from an existing the Websocket.
        /// </summary>
        /// <typeparam name="T">The type of the messages.</typeparam>
        /// <param name="pipeline">The pipeline to run the \psi components.</param>
        /// <param name="deserializer">the deserializer of incoming data.</param>
        /// <param name="remoteName">The hostname of client.</param>
        /// <param name="topic">The topic of the websocket.</param>
        /// <param name="name">The name of the component.</param>
        /// <returns>The created WebSocketSource if the Websocket exist.</returns>
        public WebSocketSource<T>? CreateWebsocketSource<T>(Pipeline pipeline, Serialization.IFormatDeserializer deserializer, string remoteName, string topic, bool useSourceTime = true, string name = nameof(WebSocketSource<T>), int port = 8080)
        {
            if (!GetWebsocket(remoteName, topic, out WebSocket websocket))
                if (!CreateWebsocket(remoteName, port, topic, out websocket))
                    return null;
            return new WebSocketSource<T>(pipeline, websocket, deserializer, useSourceTime, 0, name);
        }

        /// <summary>
        /// Created WebSocketWriter, to send message from a new the Websocket.
        /// </summary>
        /// <typeparam name="T">The type of the messages.</typeparam>
        /// <param name="pipeline">The pipeline to run the \psi components.</param>
        /// <param name="serializer">the serializer of outgoing data.</param>
        /// <param name="remoteName">The hostname of client.</param>
        /// <param name="topic">The topic of the websocket.</param>
        /// <param name="name">The name of the component.</param>
        /// <param name="port">The port for the websocket.</param>
        /// <returns>The created WebsocketWriter if the Websocket exist.</returns>
        public WebSocketWriter<T>? CreateWebsocketWriter<T>(Pipeline pipeline, Serialization.IFormatSerializer serializer, string remoteName, string topic, string name = nameof(WebSocketSource<T>), int port = 8080)
        {
            if (!GetWebsocket(remoteName, topic, out WebSocket websocket))
                if (!CreateWebsocket(remoteName, port, topic, out websocket))
                    return null;
            return new WebSocketWriter<T>(pipeline, websocket, serializer, name);
        }

        /// <summary>
        /// Create WebSocketSource, to retrieve message from a new the Websocket.
        /// </summary>
        /// <typeparam name="T">The type of the messages.</typeparam>
        /// <param name="pipeline">The pipeline to run the \psi components.</param>
        /// <param name="deserializer">the deserializer of incoming data.</param>
        /// <param name="remoteName">The hostname of client.</param>
        /// <param name="topic">The topic of the websocket.</param>
        /// <param name="name">The name of the component.</param>
        /// <returns>The created WebSocketSource if the Websocket exist.</returns>
        public WebSocketSource<T>? ConnectWebsocketSource<T>(Pipeline pipeline, Serialization.IFormatDeserializer deserializer, string remoteName, string topic, bool useSourceTime, string name = nameof(WebSocketSource<T>))
        {
            if (!GetWebsocket(remoteName, topic, out WebSocket websocket))
                return null;
            return new WebSocketSource<T>(pipeline, websocket, deserializer, useSourceTime, 0, name);
        }

        /// <summary>
        /// Created WebSocketWriter, to send message from an existing the Websocket.
        /// </summary>
        /// <typeparam name="T">The type of the messages.</typeparam>
        /// <param name="pipeline">The pipeline to run the \psi components.</param>
        /// <param name="serializer">the serializer of outgoing data.</param>
        /// <param name="remoteName">The hostname of client.</param>
        /// <param name="topic">The topic of the websocket.</param>
        /// <param name="name">The name of the component.</param>
        /// <returns>The created WebsocketWriter if the Websocket exist.</returns>
        public WebSocketWriter<T>? ConnectWebsocketWriter<T>(Pipeline pipeline, Serialization.IFormatSerializer serializer, string remoteName, string topic, string name = nameof(WebSocketSource<T>))
        {
            if (!GetWebsocket(remoteName, topic, out WebSocket websocket))
                return null;
            return new WebSocketWriter<T>(pipeline, websocket, serializer, name);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (isServer)
            {
                this.token.Cancel();
                this.httpListener.Stop();
                this.listeningThread.Join();
            }
            foreach (var client in this.websocketByClients)
                foreach (var ws in client.Value)
                    if (ws.Value.State == WebSocketState.Open)
                        ws.Value.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None);
            this.websocketByClients.Clear();
        }

        /// <inheritdoc/>
        public virtual void Start(Action<DateTime> notifyCompletionTime)
        {
            if (isServer)
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
            Dispose();
            notifyCompleted();
        }

        protected bool GetWebsocket(string remoteName, string topic, out WebSocket webSocket)
        {
            webSocket = default;
            if (!this.websocketByClients.ContainsKey(remoteName))
                return false;
            if (!this.websocketByClients[remoteName].ContainsKey(topic))
                return false;
            webSocket = this.websocketByClients[remoteName][topic];
            return true;
        }

        protected virtual bool CreateWebsocket(string remoteName, int port, string topic, out WebSocket webSocket)
        {
            try
            {
                var websocket = new ClientWebSocket();
                CancellationTokenSource cts = new CancellationTokenSource();

                if (isRestrictedToSecure)
                {
                    websocket.Options.ClientCertificates = new System.Security.Cryptography.X509Certificates.X509CertificateCollection();
                    websocket.ConnectAsync(new Uri($"wss://{remoteName}:{port}/ws/{topic}"), cts.Token);
                }
                else 
                    websocket.ConnectAsync(new Uri($"ws://{remoteName}:{port}/ws/{topic}"), cts.Token);
                RegisterWebSocket(remoteName, topic, websocket);
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

        protected virtual async void ProcessContexts()
        {
            while (!this.token.IsCancellationRequested)
            {
                var result = await this.httpListener.GetContextAsync();
                if (result != null)
                {
                    AcceptWebsocketClients(result);
                }
            }
        }

        protected bool RegisterWebSocket(string remoteName, string topic, WebSocket webSocket)
        {
            lock (this.websocketByClients)
            {
                if (!this.websocketByClients.ContainsKey(remoteName))
                    this.websocketByClients[remoteName] = new Dictionary<string, WebSocket>();
                if (this.websocketByClients[remoteName].ContainsKey(topic))
                    return false;
                this.websocketByClients[remoteName][topic] = webSocket;
                return true;
            }
        }

        protected void InitialiseHTTPListener(List<string> address)
        {
            this.httpListener = new HttpListener();
            foreach (var addr in address)
                this.httpListener.Prefixes.Add(addr);
        }

        protected virtual async void AcceptWebsocketClients(HttpListenerContext context)
        {
            if (context.Request.IsWebSocketRequest && (!isRestrictedToSecure || context.Request.IsSecureConnection))
            {
                var wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
                WebSocket webSocket = wsContext.WebSocket;
                string topic = context.Request.Url.AbsolutePath.Substring(4); //removing "/ws/"
                if (this.RegisterWebSocket(this.GetNameForHost(context.Request.Url), topic, webSocket))
                    this.OnNewWebSocketConnectedHandler?.Invoke(this, (this.GetNameForHost(context.Request.Url), topic));
            }
        }

        private string GetNameForHost(Uri hostUri)
        {
            if (hostUri.Query.Contains("?name="))
            {
                return hostUri.Query.Substring(6);
            }
            return hostUri.Host;
        }
    }
}
