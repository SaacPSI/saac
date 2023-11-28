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
        private Dictionary<string, RTCDataChannel> ChannelDictionnary;
        private WebRTCDataConnectorConfiguration Configuration;

        public WebRTCDataConnector(Pipeline parent, WebRTCDataConnectorConfiguration configuration, string name = nameof(WebRTCDataConnector), DeliveryPolicy? defaultDeliveryPolicy = null) 
            : base(parent, configuration, name, defaultDeliveryPolicy)
        {
            Configuration = configuration;
            ChannelDictionnary = new Dictionary<string, RTCDataChannel>();
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
            if (PeerConnection == null)
                return;
            foreach (var channel in Configuration.InputChannels)
                PeerConnection.createDataChannel(channel.Key);
            PeerConnection.ondatachannel += OnIncomingDataChannel;
            PeerConnection.onconnectionstatechange += (state) => {if (state == RTCPeerConnectionState.connected){InitDataChannel();}};
        }

        private void OnIncomingDataChannel(RTCDataChannel channel)
        {
            if (!ChannelDictionnary.ContainsKey(channel.label))
            {
                ChannelDictionnary.Add(channel.label, channel);
                channel.onmessage += OnData;
            }
        }

        private void InitDataChannel()
        {
            if (PeerConnection == null)
                return;
            foreach(var channel in PeerConnection.DataChannels)
            {
                if (!ChannelDictionnary.ContainsKey(channel.label))
                {
                    ChannelDictionnary.Add(channel.label, channel);
                    channel.onmessage += OnData;
                }
            }
        }

        public bool Send(string message, string label) 
        {
            if(!ChannelDictionnary.ContainsKey(label))
                return false;
            ChannelDictionnary[label].send(message);
            return true;
        }

        public bool Send(byte[] message, string label)
        {
            if (!ChannelDictionnary.ContainsKey(label))
                return false;
            ChannelDictionnary[label].send(message);
            return true;
        }

        private void OnData(RTCDataChannel dc, DataChannelPayloadProtocols proto, byte[] data)
        {
            if (!Configuration.OutputChannels.ContainsKey(dc.label))
                return;
            if (proto == DataChannelPayloadProtocols.WebRTC_String)
            {
                Configuration.OutputChannels[dc.label].Post(Encoding.UTF8.GetString(data));
            }
            else if (proto == DataChannelPayloadProtocols.WebRTC_Binary)
            {
                Configuration.OutputChannels[dc.label].Post(data, DateTime.Now);
            }
        }
    }
}
