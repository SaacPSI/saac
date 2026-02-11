// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace Microsoft.Psi.Interop.Rendezvous
{
    using System;
    using System.Linq;
    using Microsoft.Psi.Interop.Transport;
    using Microsoft.Psi.Remoting;

    /// <summary>
    /// Represents a websocket source endpoint providing a remoted data stream(s).
    /// </summary>
    public class WebsocketSourceEndpoint : Rendezvous.Endpoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebsocketSourceEndpoint"/> class.
        /// </summary>
        /// <param name="host">Host name used by the endpoint.</param>
        /// <param name="stream">Endpoint stream.</param>
        public WebsocketSourceEndpoint(string host, Rendezvous.Stream? stream = null)
            : base(stream is null ? Enumerable.Empty<Rendezvous.Stream>() : new[] { stream })
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("Host must be not null or empty.");
            }

            this.Host = host;
        }

        /// <summary>
        /// Gets the endpoint address.
        /// </summary>
        public string Host { get; private set; }

        /// <summary>
        /// Gets the stream (websocket endpoints have only one).
        /// </summary>
        public Rendezvous.Stream Stream => this.Streams.FirstOrDefault();

        /// <inheritdoc/>
        public override void AddStream(Rendezvous.Stream stream)
        {
            if (this.Streams.Count() > 0)
            {
                throw new InvalidOperationException($"Cannot add more than one stream to a single {nameof(WebsocketSourceEndpoint)}");
            }

            base.AddStream(stream);
        }
    }

    /// <summary>
    /// Rendezvous related operators.
    /// </summary>
    public static class Operators
    {
        /// <summary>
        /// Create a rendezvous endpoint from a <see cref="WebSocketWriter{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of data stream.</typeparam>
        /// <param name="writer"><see cref="WebSocketWriter{T}"/> from which to create endpoint.</param>
        /// <param name="address">Address with which to create endpoint.</param>
        /// <param name="streamName">The name of the rendezvous stream.</param>
        /// <returns>Rendezvous endpoint.</returns>
        public static Rendezvous.Endpoint ToRendezvousEndpoint<T>(this WebSocketWriter<T> writer, string address, string streamName)
            => new WebsocketSourceEndpoint(address, new Rendezvous.Stream(streamName, typeof(T)));

        /// <summary>
        /// Creates a <see cref="WebSocketSource{T}"/> from a <see cref="WebsocketSourceEndpoint"/>.
        /// </summary>
        /// <typeparam name="T">Type of data stream.</typeparam>
        /// <param name="endpoint"><see cref="WebsocketSourceEndpoint"/> from which to create the source.</param>
        /// <param name="manager">The WebSocket manager to use for connection.</param>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="deserializer">The deserializer to use to deserialize messages.</param>
        /// <returns><see cref="WebSocketSource{T}"/> if successful; otherwise null.</returns>
        public static WebSocketSource<T>? ToRemoteWebSocketSource<T>(this WebsocketSourceEndpoint endpoint, WebSocketsManager manager, Pipeline pipeline, Serialization.IFormatDeserializer deserializer)
            => manager.ConnectWebsocketSource<T>(pipeline, deserializer, endpoint.Host, endpoint.Stream.StreamName, true);
    }
}
