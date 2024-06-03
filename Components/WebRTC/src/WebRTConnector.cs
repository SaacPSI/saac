using Microsoft.Psi;
using SIPSorcery.Net;
using System.Net;
using Microsoft.Psi.Components;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SAAC.WebRTC
{
    /// <summary>
    /// WebRTConnector component class only do the basic connection throught SipSorcery librairy (https://github.com/sipsorcery-org/).
    /// See WebRTConnectorConfiguration for basic configuration details.
    /// </summary>
    public class WebRTConnector : ISourceComponent
    {
        private const int MAX_RECEIVE_BUFFER = 8192;
        private const int MAX_SEND_BUFFER = 8192;
        private const int WEB_SOCKET_CONNECTION_TIMEOUT_MS = 1200000;

        internal WebRTCLogger logger;

        protected RTCPeerConnection? peerConnection = null;
        protected CancellationToken cToken;
        protected Pipeline pipeline;
        protected string name;

        private WebRTConnectorConfiguration configuration;
        private Uri webSocketServerUri;

        public WebRTConnector(Pipeline parent, WebRTConnectorConfiguration configuration, string name = nameof(WebRTConnector))
        {
            this.name = name;
            this.configuration = configuration;
            pipeline = parent;
            logger = new WebRTCLogger();
            logger.LogLevel = configuration.Log;
            cToken = new CancellationToken();
            webSocketServerUri = new Uri("ws://" + configuration.WebsocketAddress.ToString() + ':' + configuration.WebsocketPort.ToString());
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        public async void Start(Action<DateTime> notifyCompletionTime)
        {
            peerConnection = await CreatePeerConnection().ConfigureAwait(false);
            logger.Log(LogLevel.Information, $"websocket-client attempting to connect to {webSocketServerUri}.");

            _ = Task.Run(() => WebSocketConnection(peerConnection, cToken)).ConfigureAwait(false);

            notifyCompletionTime(DateTime.MaxValue);
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (peerConnection != null)
            {
                peerConnection.Close("Stoping PSI");
                peerConnection.Dispose();
            }
            notifyCompleted();
        }

        private Task<RTCPeerConnection> CreatePeerConnection()
        {
            peerConnection = new RTCPeerConnection(null);

            PrepareActions(); 
            peerConnection.onconnectionstatechange += (state) =>
            {
                logger.Log(LogLevel.Trace, $"Peer connection state change to {webSocketServerUri}.");
                if (state == RTCPeerConnectionState.connected)
                {
                    logger.Log(LogLevel.Information, $"Peer connected.");
                }
                else if (state == RTCPeerConnectionState.failed)
                {
                    peerConnection.Close("ice disconnection");
                    logger.Log(LogLevel.Error, $"Peer connection disconnected.");
                }
            };

            // Diagnostics.
            peerConnection.OnReceiveReport += PeerConnection_OnReceiveReport;
            peerConnection.OnSendReport += PeerConnection_OnSendReport;
            peerConnection.GetRtpChannel().OnStunMessageReceived += WebRTConnector_OnStunMessageReceived;
            peerConnection.oniceconnectionstatechange += PeerConnection_oniceconnectionstatechange;

            return Task.FromResult(peerConnection);
        }

        private void PeerConnection_OnReceiveReport(IPEndPoint re, SDPMediaTypesEnum media, RTCPCompoundPacket rr)
        {
            logger.Log(LogLevel.Trace, $"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}");
        }

        private void PeerConnection_OnSendReport(SDPMediaTypesEnum media, RTCPCompoundPacket sr)
        {
            logger.Log(LogLevel.Trace, $"RTCP Send for {media}\n{sr.GetDebugSummary()}");
        }

        private void WebRTConnector_OnStunMessageReceived(STUNMessage msg, IPEndPoint ep, bool isRelay)
        {
            logger.Log(LogLevel.Trace, $"STUN {msg.Header.MessageType} received from {ep}.");
        }
        
        private void PeerConnection_oniceconnectionstatechange(RTCIceConnectionState state)
        {
            logger.Log(LogLevel.Information, $"ICE connection state change to {state}.");
        }

        protected virtual void PrepareActions()
        {}

        private async Task WebSocketConnection(RTCPeerConnection pc, CancellationToken ct)
        {
            _ = WebSocket.CreateClientBuffer(MAX_RECEIVE_BUFFER, MAX_SEND_BUFFER);
            CancellationTokenSource connectCts = new CancellationTokenSource();
            connectCts.CancelAfter(WEB_SOCKET_CONNECTION_TIMEOUT_MS);
            bool loop = true;
            while(loop)
            {
                var webSocketClient = new ClientWebSocket();
                // As best I can tell the point of the CreateClientBuffer call is to set the size of the internal
                // web socket buffers. The return buffer seems to be for cases where direct access to the raw
                // web socket data is desired.
                _ = WebSocket.CreateClientBuffer(MAX_RECEIVE_BUFFER, MAX_SEND_BUFFER);

                try
                {
                    connectCts.CancelAfter(WEB_SOCKET_CONNECTION_TIMEOUT_MS);
                    await webSocketClient.ConnectAsync(webSocketServerUri, connectCts.Token).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    loop = true;
                    continue;
                }
            
                loop = false;
                if (webSocketClient.State == WebSocketState.Open)
                {
                    logger.Log(LogLevel.Information, $"websocket-client starting receive task for server {webSocketServerUri}.");
                    _ = Task.Run(() => ReceiveFromWebSocket(pc, webSocketClient, ct)).ConfigureAwait(false);
                }
                else
                {
                    logger.Log(LogLevel.Warning, "websocket-client connection failure.");
                    pc.Close("web socket connection failure");
                }
            }
        }


        private async Task ReceiveFromWebSocket(RTCPeerConnection pc, ClientWebSocket ws, CancellationToken ct)
        {
            var buffer = new byte[MAX_RECEIVE_BUFFER];
            int posn = 0;

            while (ws.State == WebSocketState.Open &&
                (pc.connectionState == RTCPeerConnectionState.@new || pc.connectionState == RTCPeerConnectionState.connecting))
            {
                WebSocketReceiveResult receiveResult;
                do
                {
                    receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer, posn, MAX_RECEIVE_BUFFER - posn), ct).ConfigureAwait(false);
                    posn += receiveResult.Count;
                }
                while (!receiveResult.EndOfMessage);

                if (posn > 0)
                {
                    var jsonMsg = Encoding.UTF8.GetString(buffer, 0, posn);
                    string jsonResp;
                    if (configuration.PixelStreamingConnection)
                        jsonResp = await OnPixelStreamingMessage(jsonMsg, pc);
                    else
                        jsonResp = await OnMessage(jsonMsg, pc);

                    if (jsonResp != null)
                    {
                        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonResp)), WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
                    }
                }

                posn = 0;
            }

            logger.Log(LogLevel.Information, "websocket-client receive loop exiting.");
        }

        private async Task<string> OnMessage(string jsonStr, RTCPeerConnection pc)
        {

            if (RTCIceCandidateInit.TryParse(jsonStr, out var iceCandidateInit))
            {
                logger.Log(LogLevel.Information, "Got remote ICE candidate.");
                pc.addIceCandidate(iceCandidateInit);
            }
            else if (RTCSessionDescriptionInit.TryParse(jsonStr, out var descriptionInit))
            {
                logger.Log(LogLevel.Information, $"Got remote SDP, type {descriptionInit.type}.");

                var result = pc.setRemoteDescription(descriptionInit);
                if (result != SetDescriptionResultEnum.OK)
                {
                    logger.Log(LogLevel.Error, $"Failed to set remote description, {result}.");
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
                logger.Log(LogLevel.Error, $"websocket-client could not parse JSON message. {jsonStr}");
            }

            return null;
        }

        private async Task<string> OnPixelStreamingMessage(string jsonStr, RTCPeerConnection pc)
        {
            if (jsonStr.Contains("iceCandidate"))
            {
                int pos = jsonStr.IndexOf("\"candidate\":{") + 12;
                string sub = jsonStr.Substring(pos, jsonStr.IndexOf("}}") - (pos - 1));
                if (RTCIceCandidateInit.TryParse(sub, out var iceCandidateInit))
                {
                    logger.Log(LogLevel.Information, "Got remote ICE candidate.");
                    pc.addIceCandidate(iceCandidateInit);
                }
            }
            else if (RTCSessionDescriptionInit.TryParse(jsonStr, out var descriptionInit))
            {
                logger.Log(LogLevel.Information, $"Got remote SDP, type {descriptionInit.type}.");

                var result = pc.setRemoteDescription(descriptionInit);
                if (result != SetDescriptionResultEnum.OK)
                {
                    logger.Log(LogLevel.Error, $"Failed to set remote description, {result}.");
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
                logger.Log(LogLevel.Error, $"websocket-client could not parse JSON message. {jsonStr}");
            }

            return null;
        }
    }
}
