using Microsoft.Psi.Kinect;
using Microsoft.Psi.Remoting;

namespace SAAC.RemoteConnectors
{
    public class KinectRemoteStreamsComponentConfiguration : KinectSensorConfiguration
    {
        // Configuration for video stream
        public int EncodingVideoLevel { get; set; } = 90;

        // Network
        public TransportKind ConnectionType { get; set; } = TransportKind.Tcp;
        public string IpToUse { get; set; } = "localhost";
        public int StartingPort { get; set; } = 11411;
        public string RendezVousApplicationName { get; set; } = "RemoteKinectAzureServer";
    }
}
