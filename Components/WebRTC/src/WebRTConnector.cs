using Microsoft.Psi;
using SIPSorcery.Net;
using System.Net;
using Microsoft.Psi.Components;
using System.Net.WebSockets;
using System.Text;
using System.Windows.Media.Animation;

namespace WebRTC
{
    public class WebRTConnector : ISourceComponent
    {
        private const int MAX_RECEIVE_BUFFER = 8192;
        private const int MAX_SEND_BUFFER = 8192;
        private const int WEB_SOCKET_CONNECTION_TIMEOUT_MS = 1200000;

        protected RTCPeerConnection? PeerConnection = null;
        protected CancellationToken CToken;

        public string Name { get; set; }

        protected Pipeline Pipeline;
        private WebRTConnectorConfiguration Configuration;
        private Uri WebSocketServerUri;

        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<string> OutLog { get; private set; }

        public WebRTConnector(Pipeline parent, WebRTConnectorConfiguration configuration, string name = nameof(WebRTConnector), DeliveryPolicy? defaultDeliveryPolicy = null)
        {
            Name = name;
            Configuration = configuration;
            Pipeline = parent;
            OutLog = parent.CreateEmitter<string>(this, nameof(OutLog));
            CToken = new CancellationToken();
            WebSocketServerUri = new Uri("ws://" + configuration.WebsocketAddress.ToString() + ':' + configuration.WebsocketPort.ToString());
        }

        public async void Start(Action<DateTime> notifyCompletionTime)
        {
            PeerConnection = await CreatePeerConnection().ConfigureAwait(false);
            log($"websocket-client attempting to connect to {WebSocketServerUri}.");

            _ = Task.Run(() => WebSocketConnection(PeerConnection, CToken)).ConfigureAwait(false);

            notifyCompletionTime(DateTime.MaxValue);
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (PeerConnection != null)
            {
                PeerConnection.Close("Stoping PSI");
                PeerConnection.Dispose();
            }
            notifyCompleted();
        }

        private void log(string message)
        {
            Console.WriteLine(message);
            try
            {
                OutLog.Post(message, DateTime.Now);
            }
            catch(Exception ex) 
            {
                //do nothing
            }
        }

        private Task<RTCPeerConnection> CreatePeerConnection()
        {
            PeerConnection = new RTCPeerConnection(null);

            PrepareActions(); 
            PeerConnection.onconnectionstatechange += (state) =>
            {
                log($"Peer connection state change to {state}.");
                if (state == RTCPeerConnectionState.connected)
                {
                }
                else if (state == RTCPeerConnectionState.failed)
                {
                    PeerConnection.Close("ice disconnection");
                }
            };

            // Diagnostics.
            PeerConnection.OnReceiveReport += PeerConnection_OnReceiveReport;
            PeerConnection.OnSendReport += PeerConnection_OnSendReport;
            PeerConnection.GetRtpChannel().OnStunMessageReceived += WebRTConnector_OnStunMessageReceived;
            PeerConnection.oniceconnectionstatechange += PeerConnection_oniceconnectionstatechange;

            return Task.FromResult(PeerConnection);
        }

        private void PeerConnection_OnReceiveReport(IPEndPoint re, SDPMediaTypesEnum media, RTCPCompoundPacket rr)
        {
            log($"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}");
        }

        private void PeerConnection_OnSendReport(SDPMediaTypesEnum media, RTCPCompoundPacket sr)
        {
            log($"RTCP Send for {media}\n{sr.GetDebugSummary()}");
        }

        private void WebRTConnector_OnStunMessageReceived(STUNMessage msg, IPEndPoint ep, bool isRelay)
        {
            log($"STUN {msg.Header.MessageType} received from {ep}.");
        }
        
        private void PeerConnection_oniceconnectionstatechange(RTCIceConnectionState state)
        {
           log($"ICE connection state change to {state}.");
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
                    await webSocketClient.ConnectAsync(WebSocketServerUri, connectCts.Token).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    loop = true;
                    continue;
                }
            
                loop = false;
                if (webSocketClient.State == WebSocketState.Open)
                {
                    log($"websocket-client starting receive task for server {WebSocketServerUri}.");
                    _ = Task.Run(() => ReceiveFromWebSocket(pc, webSocketClient, ct)).ConfigureAwait(false);
                }
                else
                {
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
                    if (Configuration.PixelStreamingConnection)
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

            log($"websocket-client receive loop exiting.");
        }

        private async Task<string> OnMessage(string jsonStr, RTCPeerConnection pc)
        {

            if (RTCIceCandidateInit.TryParse(jsonStr, out var iceCandidateInit))
            {
                log("Got remote ICE candidate.");
                pc.addIceCandidate(iceCandidateInit);
            }
            else if (RTCSessionDescriptionInit.TryParse(jsonStr, out var descriptionInit))
            {
                log($"Got remote SDP, type {descriptionInit.type}.");

                var result = pc.setRemoteDescription(descriptionInit);
                if (result != SetDescriptionResultEnum.OK)
                {
                    log($"Failed to set remote description, {result}.");
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
                log($"websocket-client could not parse JSON message. {jsonStr}");
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
                    log("Got remote ICE candidate.");
                    pc.addIceCandidate(iceCandidateInit);
                }
            }
            else if (RTCSessionDescriptionInit.TryParse(jsonStr, out var descriptionInit))
            {
                log($"Got remote SDP, type {descriptionInit.type}.");

                var result = pc.setRemoteDescription(descriptionInit);
                if (result != SetDescriptionResultEnum.OK)
                {
                    log($"Failed to set remote description, {result}.");
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
                log($"websocket-client could not parse JSON message. {jsonStr}");
            }

            return null;
        }
    }
}
