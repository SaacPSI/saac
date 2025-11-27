// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using System.Diagnostics;
    using System.Net.WebSockets;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Component that serializes and writes messages to a remote websocket.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class WebSocketWriter<T> : IConsumer<T>, IDisposable
    {
        private readonly IFormatSerializer serializer;
        private readonly string name;
        private WebSocket websocket;
        private System.Threading.CancellationTokenSource token;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketWriter{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="websocket">The websocket to send data to.</param>
        /// <param name="serializer">The serializer to use to serialize messages.</param>
        /// <param name="name">An optional name for the component.</param>
        public WebSocketWriter(Pipeline pipeline, WebSocket websocket, IFormatSerializer serializer, string name = nameof(WebSocketWriter<T>))
        {
            this.In = pipeline.CreateReceiver<T>(this, this.Receive, nameof(this.In));
            this.name = name;
            this.serializer = serializer;
            this.websocket = websocket;
            this.token = new System.Threading.CancellationTokenSource();
        }

        /// <inheritdoc/>
        public Receiver<T> In { get; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.token.Cancel();
            this.token.Dispose();
            if (this.websocket.CloseStatus == WebSocketCloseStatus.Empty)
            {
                this.websocket.Dispose();
            }
        }

        /// <summary>
        /// Handling message to websocket.
        /// </summary>
        /// <param name="message">The data to be sent.</param>
        /// <param name="envelope">The enveloppe of the message.</param>
        protected virtual void Receive(T message, Envelope envelope)
        {
            (var bytes, int offset, int count) = this.serializer.SerializeMessage(message, envelope.OriginatingTime);

            try
            {
                if (this.websocket.State == WebSocketState.Open)
                {
                    ArraySegment<byte> counter = new ArraySegment<byte>(BitConverter.GetBytes(count));
                    ArraySegment<byte> buffer = new ArraySegment<byte>(bytes);
                    this.websocket.SendAsync(counter, WebSocketMessageType.Binary, false, this.token.Token);
                    this.websocket.SendAsync(buffer, WebSocketMessageType.Binary, true, this.token.Token);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"WebsocketWriter {name} Exception: {ex.Message}");
            }
        }
    }
}
