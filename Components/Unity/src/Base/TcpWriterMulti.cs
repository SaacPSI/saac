﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace SAAC.PipelineServices
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Interop.Transport;

    /// <summary>
    /// Component that serializes and writes messages to a remote server over TCP.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class TcpWriterMulti<T> : IConsumer<T>, IDisposable
    {
        private readonly IFormatSerializer<T> serializer;
        private readonly string name;

        private TcpListener listener;
        private List<TcpClient> clients;
        private Thread? acceptingThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpWriter{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="port">The connection port.</param>
        /// <param name="serializer">The serializer to use to serialize messages.</param>
        /// <param name="name">An optional name for the component.</param>
        public TcpWriterMulti(Pipeline pipeline, int port, IFormatSerializer<T> serializer, string name = nameof(TcpWriterMulti<T>))
        {
            this.serializer = serializer;
            this.name = name;
            this.Port = port;
            this.In = pipeline.CreateReceiver<T>(this, this.Receive, nameof(this.In));
            this.listener = new TcpListener(IPAddress.Any, port);
            this.clients = new List<TcpClient>();
            Start();
        }

        /// <summary>
        /// Gets the connection port.
        /// </summary>
        public int Port { get; private set; }

        /// <inheritdoc/>
        public Receiver<T> In { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Stop();
            this.listener = null;
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Receive(T message, Envelope envelope)
        {
            (var bytes, int offset, int count) = this.serializer.SerializeMessage(message, envelope.OriginatingTime);


            if (this.clients.Count != 0)
            {
                List<TcpClient> clientsToRemove = new List<TcpClient>();
                foreach (var client in this.clients)
                {
                    if (!client.Connected)
                    {
                        clientsToRemove.Add(client);
                        continue;
                    }
                    try
                    {
                        var stream = client.GetStream();
                        stream.Write(BitConverter.GetBytes(count), 0, sizeof(int));
                        stream.Write(bytes, offset, count);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"TcpWriterMulti Exception: {ex.Message}");
                        clientsToRemove.Add(client);
                    }
                }
                clientsToRemove.ForEach(client =>
                {
                    this.clients.Remove(client);
                });
            }
        }

        private void Start()
        {
            acceptingThread = new Thread(new ThreadStart(this.Listen)) { IsBackground = true };
            acceptingThread.Start();
        }

        private void Stop()
        {
            acceptingThread?.Abort();
            // Dispose active client if any
            if (this.clients.Count != 0)
                foreach (var client in this.clients)
                    client.Dispose();
            this.clients.Clear();
            this.listener.Stop();
        }

        private void Listen()
        {
            while (this.listener != null)
            {
                try
                {
                    this.listener.Start();
                    this.clients.Add(this.listener.AcceptTcpClient());
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"TcpWriter Exception: {ex.Message}");
                }
            }
        }

        public Rendezvous.Endpoint ToRendezvousEndpoint(string address, string streamName)
        {
            return new Rendezvous.TcpSourceEndpoint(address, this.Port, new Rendezvous.Stream(streamName, typeof(T)));
        }
    }
}
