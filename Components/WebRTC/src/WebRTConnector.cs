// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    using System.Net;
    using System.Net.WebSockets;
    using System.Text;
    using Microsoft.Extensions.Logging;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using SIPSorcery.Net;

    /// <summary>
    /// WebRTConnector component class only do the basic connection throught SipSorcery librairy (https://github.com/sipsorcery-org/).
    /// See WebRTConnectorConfiguration for basic configuration details.
    /// </summary>
    public class WebRTConnector : ISourceComponent
    {
        /// <summary>
        /// The logger instance.
        /// </summary>
        internal WebRTCLogger Logger;

        /// <summary>
        /// The peer connection.
        /// </summary>
        protected RTCPeerConnection? peerConnection = null;

        /// <summary>
        /// The cancellation token.
        /// </summary>
        protected CancellationToken cToken;

        /// <summary>
        /// The pipeline.
        /// </summary>
        protected Pipeline pipeline;

        /// <summary>
        /// The component name.
        /// </summary>
        protected string name;

        private const int MaxReceiveBuffer = 8192;
        private const int MaxSendBuffer = 8192;
        private const int WebSocketConnectionTimeoutMs = 1200000;

        private WebRTConnectorConfiguration configuration;
        private Uri webSocketServerUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRTConnector"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="name">The name of the component.</param>
        public WebRTConnector(Pipeline parent, WebRTConnectorConfiguration configuration, string name = nameof(WebRTConnector))
        {
            this.name = name;
            this.configuration = configuration;
            this.pipeline = parent;
            this.Logger = new WebRTCLogger();
            this.Logger.LogLevel = configuration.Log;
            this.cToken = CancellationToken.None;
            this.webSocketServerUri = new Uri("ws://" + configuration.WebsocketAddress.ToString() + ':' + configuration.WebsocketPort.ToString());
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <inheritdoc/>
        public async void Start(Action<DateTime> notifyCompletionTime)
        {
            this.peerConnection = await this.CreatePeerConnection().ConfigureAwait(false);
            this.Logger.Log(LogLevel.Information, $"websocket-client attempting to connect to {this.webSocketServerUri}.");

            _ = Task.Run(() => this.WebSocketConnection(this.peerConnection, this.cToken)).ConfigureAwait(false);

            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (this.peerConnection != null)
            {
                this.peerConnection.Close("Stoping PSI");
                this.peerConnection.Dispose();
            }

            notifyCompleted();
        }

        /// <summary>
        /// Virtual method trigger in child classes.
        /// </summary>
        protected virtual void PrepareActions()
        {
        }

        private Task<RTCPeerConnection> CreatePeerConnection()
        {
            this.peerConnection = new RTCPeerConnection(null);

            this.PrepareActions();
            this.peerConnection.onconnectionstatechange += (state) =>
            {
                this.Logger.Log(LogLevel.Trace, $"Peer connection state change to {this.webSocketServerUri}.");
                if (state == RTCPeerConnectionState.connected)
                {
                    this.Logger.Log(LogLevel.Information, $"Peer connected.");
                }
                else if (state == RTCPeerConnectionState.failed)
                {
                    this.peerConnection.Close("ice disconnection");
                    this.Logger.Log(LogLevel.Error, $"Peer connection disconnected.");
                }
            };

            // Diagnostics.
            this.peerConnection.OnReceiveReport += this.PeerConnection_OnReceiveReport;
            this.peerConnection.OnSendReport += this.PeerConnection_OnSendReport;
            this.peerConnection.GetRtpChannel().OnStunMessageReceived += this.WebRTConnector_OnStunMessageReceived;
            this.peerConnection.oniceconnectionstatechange += this.PeerConnection_oniceconnectionstatechange;

            return Task.FromResult(this.peerConnection);
        }

        private void PeerConnection_OnReceiveReport(IPEndPoint re, SDPMediaTypesEnum media, RTCPCompoundPacket rr)
        {
            this.Logger.Log(LogLevel.Trace, $"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}");
        }

        private void PeerConnection_OnSendReport(SDPMediaTypesEnum media, RTCPCompoundPacket sr)
        {
            this.Logger.Log(LogLevel.Trace, $"RTCP Send for {media}\n{sr.GetDebugSummary()}");
        }

        private void WebRTConnector_OnStunMessageReceived(STUNMessage msg, IPEndPoint ep, bool isRelay)
        {
            this.Logger.Log(LogLevel.Trace, $"STUN {msg.Header.MessageType} received from {ep}.");
        }

        private void PeerConnection_oniceconnectionstatechange(RTCIceConnectionState state)
        {
            this.Logger.Log(LogLevel.Information, $"ICE connection state change to {state}.");
        }

        private async Task WebSocketConnection(RTCPeerConnection pc, CancellationToken ct)
        {
            _ = WebSocket.CreateClientBuffer(MaxReceiveBuffer, MaxSendBuffer);
            CancellationTokenSource connectCts = new CancellationTokenSource();
            connectCts.CancelAfter(WebSocketConnectionTimeoutMs);
            bool loop = true;
            while (loop)
            {
                var webSocketClient = new ClientWebSocket();

                // As best I can tell the point of the CreateClientBuffer call is to set the size of the internal
                // web socket buffers. The return buffer seems to be for cases where direct access to the raw
                // web socket data is desired.
                _ = WebSocket.CreateClientBuffer(MaxReceiveBuffer, MaxSendBuffer);

                try
                {
                    connectCts.CancelAfter(WebSocketConnectionTimeoutMs);
                    await webSocketClient.ConnectAsync(this.webSocketServerUri, connectCts.Token).ConfigureAwait(true);
                }
                catch (Exception)
                {
                    loop = true;
                    continue;
                }

                loop = false;
                if (webSocketClient.State == WebSocketState.Open)
                {
                    this.Logger.Log(LogLevel.Information, $"websocket-client starting receive task for server {this.webSocketServerUri}.");
                    _ = Task.Run(() => this.ReceiveFromWebSocket(pc, webSocketClient, ct)).ConfigureAwait(false);
                }
                else
                {
                    this.Logger.Log(LogLevel.Warning, "websocket-client connection failure.");
                    pc.Close("web socket connection failure");
                }
            }
        }

        private async Task ReceiveFromWebSocket(RTCPeerConnection pc, ClientWebSocket ws, CancellationToken ct)
        {
            var buffer = new byte[MaxReceiveBuffer];
            int posn = 0;

            while (ws.State == WebSocketState.Open &&
                (pc.connectionState == RTCPeerConnectionState.@new || pc.connectionState == RTCPeerConnectionState.connecting))
            {
                WebSocketReceiveResult receiveResult;
                do
                {
                    receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer, posn, MaxReceiveBuffer - posn), ct).ConfigureAwait(false);
                    posn += receiveResult.Count;
                }
                while (!receiveResult.EndOfMessage);

                if (posn > 0)
                {
                    var jsonMsg = Encoding.UTF8.GetString(buffer, 0, posn);
                    string? jsonResp;
                    if (this.configuration.PixelStreamingConnection)
                    {
                        jsonResp = await this.OnPixelStreamingMessage(jsonMsg, pc);
                    }
                    else
                    {
                        jsonResp = await this.OnMessage(jsonMsg, pc);
                    }

                    if (jsonResp != null)
                    {
                        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonResp)), WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
                    }
                }

                posn = 0;
            }

            this.Logger.Log(LogLevel.Information, "websocket-client receive loop exiting.");
        }

        private async Task<string?> OnMessage(string jsonStr, RTCPeerConnection pc)
        {
            if (RTCIceCandidateInit.TryParse(jsonStr, out var iceCandidateInit))
            {
                this.Logger.Log(LogLevel.Information, "Got remote ICE candidate.");
                pc.addIceCandidate(iceCandidateInit);
            }
            else if (RTCSessionDescriptionInit.TryParse(jsonStr, out var descriptionInit))
            {
                this.Logger.Log(LogLevel.Information, $"Got remote SDP, type {descriptionInit.type}.");

                var result = pc.setRemoteDescription(descriptionInit);
                if (result != SetDescriptionResultEnum.OK)
                {
                    this.Logger.Log(LogLevel.Error, $"Failed to set remote description, {result}.");
                    pc.Close("failed to set remote description");
                }

                if (descriptionInit.type == RTCSdpType.offer)
                {
                    var answerSdp = pc.createAnswer(null);
                    await pc.setLocalDescription(answerSdp).ConfigureAwait(false);

                    return answerSdp.toJSON();
                }
            }
            else
            {
                this.Logger.Log(LogLevel.Error, $"websocket-client could not parse JSON message. {jsonStr}");
            }

            return null;
        }

        private async Task<string?> OnPixelStreamingMessage(string jsonStr, RTCPeerConnection pc)
        {
            if (jsonStr.Contains("iceCandidate"))
            {
                int pos = jsonStr.IndexOf("\"candidate\":{") + 12;
                string sub = jsonStr.Substring(pos, jsonStr.IndexOf("}}") - (pos - 1));
                if (RTCIceCandidateInit.TryParse(sub, out var iceCandidateInit))
                {
                    this.Logger.Log(LogLevel.Information, "Got remote ICE candidate.");
                    pc.addIceCandidate(iceCandidateInit);
                }
            }
            else if (RTCSessionDescriptionInit.TryParse(jsonStr, out var descriptionInit))
            {
                this.Logger.Log(LogLevel.Information, $"Got remote SDP, type {descriptionInit.type}.");

                var result = pc.setRemoteDescription(descriptionInit);
                if (result != SetDescriptionResultEnum.OK)
                {
                    this.Logger.Log(LogLevel.Error, $"Failed to set remote description, {result}.");
                    pc.Close("failed to set remote description");
                }

                if (descriptionInit.type == RTCSdpType.offer)
                {
                    var answerSdp = pc.createAnswer(null);
                    await pc.setLocalDescription(answerSdp).ConfigureAwait(false);

                    return answerSdp.toJSON();
                }
            }
            else
            {
                this.Logger.Log(LogLevel.Error, $"websocket-client could not parse JSON message. {jsonStr}");
            }

            return null;
        }
    }
}
