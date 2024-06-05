using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAAC.WebRTC
{
    public class WebRTCDataConnectorConfiguration : WebRTConnectorConfiguration
    {
        public Dictionary<string, IWebRTCDataReceiverToChannel> InputChannels { get; set; } = new Dictionary<string, IWebRTCDataReceiverToChannel>();
        public Dictionary<string, IWebRTCDataChannelToEmitter> OutputChannels { get; set; } = new Dictionary<string, IWebRTCDataChannelToEmitter>();
    }
}
