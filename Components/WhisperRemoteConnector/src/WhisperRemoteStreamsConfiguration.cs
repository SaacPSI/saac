using Microsoft.Psi.Remoting;

namespace SAAC.RemoteConnectors
{
    public class WhisperRemoteStreamsConfiguration
    {
        public string RendezVousAddress { get; set; } = "localhost";
        public int RendezVousPort { get; set; } = 13331;
        public int ExportPort { get; set; } = 11570;
        public TransportKind ConnectionType { get; set; } = TransportKind.Tcp;
        public string RendezVousApplicationName { get; set; } = "WhisperStreaming";
    }
}