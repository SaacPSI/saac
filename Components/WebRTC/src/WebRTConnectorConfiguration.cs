using Microsoft.Extensions.Logging;
using System.Net;

namespace SAAC.WebRTC
{
    public class WebRTConnectorConfiguration
    {
        public uint WebsocketPort { get; set; } = 80;
        public IPAddress WebsocketAddress { get; set; } = IPAddress.Any;
        public bool PixelStreamingConnection = false;
        public LogLevel Log = LogLevel.Trace;
    }
}
