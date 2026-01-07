// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.WebSockets;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Component that read and deserialise messages from a remote websocket.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class WebSocketSource<T> : IProducer<T>, ISourceComponent, IDisposable
    {
        private readonly IFormatDeserializer deserializer;
        private readonly string name;
        private WebSocket websocket;
        private System.Threading.CancellationTokenSource token;
        private Thread receiveThread;
        private int bufferSize;
        private bool useSourceOriginatingTime = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketSource{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="websocket">The websocket to recieve data from.</param>
        /// <param name="deserializer">The deserializer to use to serialize messages.</param>
        /// <param name="useSourceOriginatingTime">If the component use the incoming time or not.</param>
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
            if(!this.token.IsCancellationRequested)
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
            Dispose();
            notifyCompleted();
        }

        /// <summary>
        /// Handling message from websocket.
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
                    Trace.WriteLine($"WebsocketSource {name} Exception: {ex.Message}");
                }
            }
        }
    }
}
