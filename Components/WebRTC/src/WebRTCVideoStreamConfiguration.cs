namespace WebRTC
{
    public class WebRTCVideoStreamConfiguration : WebRTCDataConnectorConfiguration
    {
        public bool AudioStreaming = false;
        public string? FFMPEGFullPath { get; set; } = null;
    }
}
