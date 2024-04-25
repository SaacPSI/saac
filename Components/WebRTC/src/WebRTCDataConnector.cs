using Microsoft.Psi;
using System.Text;
using SIPSorcery.Net;

namespace SAAC.WebRTC
{
    /// <summary>
    /// WebRTCDataConnector component class to send and recieve from datachannels 
    /// </summary>

    public class WebRTCDataConnector : WebRTConnector
    {
        private Dictionary<string, RTCDataChannel> channelDictionnary;
        private WebRTCDataConnectorConfiguration configuration;

        public WebRTCDataConnector(Pipeline parent, WebRTCDataConnectorConfiguration configuration, string name = nameof(WebRTCDataConnector)) 
            : base(parent, configuration, name)
        {
            this.configuration = configuration;
            channelDictionnary = new Dictionary<string, RTCDataChannel>();
            foreach (var channel in configuration.InputChannels)
            {
                if(channel.Value.Type == IWebRTCDataReceiverToChannel.MessageType.Json) 
                    channel.Value.SetOnMessageDelegateJson(this.Send);
                else
                    channel.Value.SetOnMessageDelegateBytes(this.Send);
            }
        }

        protected override void PrepareActions()
        {
            if (peerConnection == null)
                return;
            foreach (var channel in configuration.InputChannels)
                peerConnection.createDataChannel(channel.Key);
            peerConnection.ondatachannel += OnIncomingDataChannel;
            peerConnection.onconnectionstatechange += (state) => {if (state == RTCPeerConnectionState.connected){InitDataChannel();}};
        }

        private void OnIncomingDataChannel(RTCDataChannel channel)
        {
            if (!channelDictionnary.ContainsKey(channel.label))
            {
                channelDictionnary.Add(channel.label, channel);
                channel.onmessage += OnData;
            }
        }

        private void InitDataChannel()
        {
            if (peerConnection == null)
                return;
            foreach(var channel in peerConnection.DataChannels)
            {
                if (!channelDictionnary.ContainsKey(channel.label))
                {
                    channelDictionnary.Add(channel.label, channel);
                    channel.onmessage += OnData;
                }
            }
        }

        public bool Send(string message, string label) 
        {
            if(!channelDictionnary.ContainsKey(label))
                return false;
            channelDictionnary[label].send(message);
            return true;
        }

        public bool Send(byte[] message, string label)
        {
            if (!channelDictionnary.ContainsKey(label))
                return false;
            channelDictionnary[label].send(message);
            return true;
        }

        private void OnData(RTCDataChannel dc, DataChannelPayloadProtocols proto, byte[] data)
        {
            if (!configuration.OutputChannels.ContainsKey(dc.label))
                return;
            if (proto == DataChannelPayloadProtocols.WebRTC_String)
            {
                configuration.OutputChannels[dc.label].Post(Encoding.UTF8.GetString(data));
            }
            else if (proto == DataChannelPayloadProtocols.WebRTC_Binary)
            {
                configuration.OutputChannels[dc.label].Post(data, DateTime.Now);
            }
        }
    }
}
