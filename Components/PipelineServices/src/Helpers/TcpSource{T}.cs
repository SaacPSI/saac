﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace SAAC.PipelineServices.Helpers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Interop.Serialization;

    public static class Operators
    {
        public static TcpSource<T> ToTcpSourceSaac<T>(
           this Rendezvous.TcpSourceEndpoint endpoint,
           Pipeline pipeline,
           IFormatDeserializer deserializer,
           Action<T> deallocator = null,
           bool useSourceOriginatingTimes = true,
           string name = nameof(TcpSource<T>))
           => new(pipeline, endpoint.Host, endpoint.Port, deserializer, deallocator, useSourceOriginatingTimes, name);
    }

    /// <summary>
    /// Component that reads and deserializes messages from a remote server over TCP.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class TcpSource<T> : IProducer<T>, ISourceComponent, IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly string address;
        private readonly int port;
        private readonly Action<T> deallocator;
        private readonly string name;
        private readonly TcpClient client;
        private readonly IFormatDeserializer deserializer;
        private readonly bool useSourceOriginatingTimes;
        private Thread readerThread;
        private Action<DateTime> completed;
        private DateTime endTime = DateTime.MaxValue;
        private byte[] frameBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpSource{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="address">The address of the remote server.</param>
        /// <param name="port">The port on which to connect.</param>
        /// <param name="deserializer">The deserializer to use to deserialize messages.</param>
        /// <param name="deallocator">An optional deallocator for the data.</param>
        /// <param name="useSourceOriginatingTimes">An optional parameter indicating whether to use originating times from the source received over the network or to re-timestamp with the current pipeline time upon receiving.</param>
        /// <param name="name">An optional name for the component.</param>
        public TcpSource(
            Pipeline pipeline,
            string address,
            int port,
            IFormatDeserializer deserializer,
            Action<T> deallocator = null,
            bool useSourceOriginatingTimes = true,
            string name = nameof(TcpSource<T>))
        {
            this.pipeline = pipeline;
            this.client = new TcpClient();
            this.address = address;
            this.port = port;
            this.deserializer = deserializer;
            this.deallocator = deallocator ?? (d =>
            {
                if (d is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            });

            this.useSourceOriginatingTimes = useSourceOriginatingTimes;
            this.name = name;
            this.Out = pipeline.CreateEmitter<T>(this, nameof(this.Out));
        }

        /// <inheritdoc/>
        public Emitter<T> Out { get; }

        /// <inheritdoc/>
        public void Dispose() => this.client.Close();

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.completed = notifyCompletionTime;
            this.readerThread = new Thread(this.ReadFrames);
            this.readerThread.Start();
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.endTime = finalOriginatingTime;

            // ensures that any pending connection attempt is terminated
            this.client.Close();

            this.readerThread.Join();
            notifyCompleted();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Reads a data frame into the frame buffer. Will re-allocate the frame buffer if necessary.
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        private (dynamic, DateTime) ReadNextFrame(BinaryReader binaryReader)
        {
            int frameLength = binaryReader.ReadInt32();

            // ensure that the frame buffer is large enough to accommodate the next frame
            if (this.frameBuffer == null || this.frameBuffer.Length < frameLength)
            {
                this.frameBuffer = new byte[frameLength];
            }

            // read the entire frame into the frame buffer
            int bytesRead = binaryReader.Read(this.frameBuffer, 0, frameLength);
            while (bytesRead < frameLength)
            {
                bytesRead += binaryReader.Read(this.frameBuffer, bytesRead, frameLength - bytesRead);
            }

            // deserialize the frame bytes into (T, DateTime)
            (var data, var originatingTime) = this.deserializer.DeserializeMessage(this.frameBuffer, 0, frameLength);
            Trace.WriteLine($"TcpSource ReadNextFrame {originatingTime}:{this.port}. Retrying ...");
            return this.useSourceOriginatingTimes ? (data, originatingTime) : (data, this.pipeline.GetCurrentTime());
        }

        private void ReadFrames()
        {
            // ensure that we don't read past the end of the pipeline replay descriptor
            this.endTime = this.Out.Pipeline.ReplayDescriptor.End;

            var lastTimestamp = DateTime.MinValue;

            try
            {
                var connected = false;
                while (!connected)
                {
                    try
                    {
                        Trace.WriteLine($"TcpSource Attempting to connect to {this.address}:{this.port}");
                        this.client.Connect(this.address, this.port);
                        Trace.WriteLine($"TcpSource Connected to {this.address}:{this.port}.");
                        connected = true;
                    }
                    catch (SocketException)
                    {
                        // Retry for as long as the client has not been disposed
                        Trace.WriteLine($"TcpSource Failed to connect to port {this.address}:{this.port}. Retrying ...");
                    }
                }

                using var reader = new BinaryReader(this.client.GetStream());

                // read and deserialize frames from the stream reader
                for (var (message, timestamp) = this.ReadNextFrame(reader);
                    timestamp <= this.endTime;
                    lastTimestamp = timestamp, (message, timestamp) = this.ReadNextFrame(reader))
                {
                    this.Out.Post(message, timestamp);
                    this.deallocator(message);
                }
            }
            catch (EndOfStreamException)
            {
                // Catch when the peer close the stream unproperly.
                Trace.WriteLine($"Connection unproperly closed {this.address}:{this.port}.");
            }
            finally
            {
                // completion time is last posted message timestamp
                this.completed?.Invoke(lastTimestamp);
            }
        }
    }
}
