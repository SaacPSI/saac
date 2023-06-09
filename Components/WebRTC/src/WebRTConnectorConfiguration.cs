﻿using System.Net;

namespace WebRTC
{
    public class WebRTConnectorConfiguration
    {
        public uint WebsocketPort { get; set; } = 80;
        public IPAddress WebsocketAddress { get; set; } = IPAddress.Any;
        public bool PixelStreamingConnection = true;
        public bool AudioStreaming = false;
    }
}
