using Microsoft.Psi.Remoting;

namespace SAAC.RemoteConnectors
{
    public class KinectAzureRemoteStreamsConfiguration
    {
        public int KinectDeviceIndex { get; set; } = 0;

        // Stream available
        public bool StreamAudio { get; set; } = true;
        public bool StreamSkeleton { get; set; } = true;
        public bool StreamVideo { get; set; } = true;
        public bool StreamDepth { get; set; } = false;
        public bool StreamDepthCalibration { get; set; } = false;
        public bool StreamIMU { get; set; } = false;

        // Configuration for video stream
        public int EncodingVideoLevel { get; set; } = 90;
        public Tuple<float, float>? VideoResolution { get; set; } = new Tuple<float, float>(640.0f, 360.0f);

        // Network
        public TransportKind ConnectionType { get; set; } = TransportKind.Tcp;
        public string RendezVousAddress { get; set; } = "localhost";
        public int RendezVousPort { get; set; } = 11411;
        public string RendezVousApplicationName { get; set; } = "RemoteKinectAzureServer";
    }
}
