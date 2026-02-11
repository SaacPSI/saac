// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using System.Diagnostics;
    using System.Net.WebSockets;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Component that reads and deserializes messages from a remote WebSocket.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class WebSocketSource<T> : IProducer<T>, ISourceComponent, IDisposable
    {
        private readonly IFormatDeserializer deserializer;
        private readonly string name;
        private readonly int bufferSize;
        private readonly bool useSourceOriginatingTime = false;
        private WebSocket websocket;
        private CancellationTokenSource token;
        private Thread receiveThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketSource{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="websocket">The WebSocket to receive data from.</param>
        /// <param name="deserializer">The deserializer to use to deserialize messages.</param>
        /// <param name="useSourceOriginatingTime">If true, use the incoming message timestamp; otherwise use current pipeline time.</param>
        /// <param name="bufferSize">The buffer size for receiving data (0 for automatic).</param>
        /// <param name="name">An optional name for the component.</param>
        public WebSocketSource(Pipeline pipeline, WebSocket websocket, IFormatDeserializer deserializer, bool useSourceOriginatingTime = true, int bufferSize = 0, string name = nameof(WebSocketSource<T>))
        {
            this.Out = pipeline.CreateEmitter<T>(this, nameof(this.Out));
            this.name = name;
            this.deserializer = deserializer;
            this.websocket = websocket;
            this.token = new System.Threading.CancellationTokenSource();
            this.useSourceOriginatingTime = useSourceOriginatingTime;
            if (typeof(T) == typeof(string))
            {
                this.bufferSize = 1024;
            }
            else
            {
                this.bufferSize = bufferSize > 0 ? bufferSize : (Marshal.SizeOf(typeof(T)) + Marshal.SizeOf(typeof(DateTime))) * 2;
            }
        }

        /// <inheritdoc/>
        public Emitter<T> Out { get; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!this.token.IsCancellationRequested)
            {
                this.token.Cancel();
                this.receiveThread.Join();
                this.token.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);
            this.receiveThread = new Thread(new ThreadStart(this.Receive));
            this.receiveThread.Start();
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.Dispose();
            notifyCompleted();
        }

        /// <summary>
        /// Handles incoming messages from the WebSocket.
        /// </summary>
        protected virtual async void Receive()
        {
            while (!this.token.IsCancellationRequested)
            {
                byte[] bytes = new byte[this.bufferSize];
                ArraySegment<byte> buffer = new ArraySegment<byte>(bytes);
                try
                {
                    var result = await this.websocket.ReceiveAsync(buffer, this.token.Token);
                    if (result.EndOfMessage)
                    {
                        var data = this.deserializer.DeserializeMessage(buffer.Array, 0, result.Count);
                        this.Out.Post(data.Message, this.useSourceOriginatingTime ? data.OriginatingTime : this.Out.Pipeline.GetCurrentTime());
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"WebsocketSource {this.name} Exception: {ex.Message}");
                }
            }
        }
    }
}
