// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    using System.Text;
    using Microsoft.Psi;
    using SIPSorcery.Net;

    /// <summary>
    /// WebRTC data connector component for sending and receiving from data channels.
    /// </summary>
    public class WebRTCDataConnector : WebRTConnector
    {
        private Dictionary<string, RTCDataChannel> channelDictionnary;
        private WebRTCDataConnectorConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRTCDataConnector"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="name">The component name.</param>
        public WebRTCDataConnector(Pipeline parent, WebRTCDataConnectorConfiguration configuration, string name = nameof(WebRTCDataConnector))
            : base(parent, configuration, name)
        {
            this.configuration = configuration;
            this.channelDictionnary = new Dictionary<string, RTCDataChannel>();
            foreach (var channel in configuration.InputChannels)
            {
                if (channel.Value.Type == AWebRTCDataReceiverToChannel.MessageType.Json)
                {
                    channel.Value.SetOnMessageDelegateJson(this.Send);
                }
                else
                {
                    channel.Value.SetOnMessageDelegateBytes(this.Send);
                }
            }
        }

        /// <summary>
        /// Sends a string message to a data channel.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="label">The channel label.</param>
        /// <returns>True if successful.</returns>
        public bool Send(string message, string label)
        {
            if (!this.channelDictionnary.ContainsKey(label))
            {
                return false;
            }

            this.channelDictionnary[label].send(message);
            return true;
        }

        /// <summary>
        /// Sends binary data to a data channel.
        /// </summary>
        /// <param name="message">The binary data to send.</param>
        /// <param name="label">The channel label.</param>
        /// <returns>True if successful.</returns>
        public bool Send(byte[] message, string label)
        {
            if (!this.channelDictionnary.ContainsKey(label))
            {
                return false;
            }

            this.channelDictionnary[label].send(message);
            return true;
        }

        /// <inheritdoc/>
        protected override void PrepareActions()
        {
            if (this.peerConnection == null)
            {
                return;
            }

            foreach (var channel in this.configuration.InputChannels)
            {
                this.peerConnection.createDataChannel(channel.Key);
            }

            this.peerConnection.ondatachannel += this.OnIncomingDataChannel;
            this.peerConnection.onconnectionstatechange += (state) =>
            {
                if (state == RTCPeerConnectionState.connected)
                {
                    this.InitDataChannel();
                }
            };
        }

        private void OnIncomingDataChannel(RTCDataChannel channel)
        {
            if (!this.channelDictionnary.ContainsKey(channel.label))
            {
                this.channelDictionnary.Add(channel.label, channel);
                channel.onmessage += this.OnData;
            }
        }

        private void InitDataChannel()
        {
            if (this.peerConnection == null)
            {
                return;
            }

            foreach (var channel in this.peerConnection.DataChannels)
            {
                if (!this.channelDictionnary.ContainsKey(channel.label))
                {
                    this.channelDictionnary.Add(channel.label, channel);
                    channel.onmessage += this.OnData;
                }
            }
        }

        private void OnData(RTCDataChannel dc, DataChannelPayloadProtocols proto, byte[] data)
        {
            if (!this.configuration.OutputChannels.ContainsKey(dc.label))
            {
                return;
            }

            if (proto == DataChannelPayloadProtocols.WebRTC_String)
            {
                this.configuration.OutputChannels[dc.label].Post(Encoding.UTF8.GetString(data));
            }
            else if (proto == DataChannelPayloadProtocols.WebRTC_Binary)
            {
                this.configuration.OutputChannels[dc.label].Post(data, DateTime.Now);
            }
        }
    }
}
