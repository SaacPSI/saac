namespace SAAC.RemoteConnectors
{
    public class KinectAzureRemoteConnectorConfiguration
    {
        /// <summary>
        /// Gets or sets port number where the iteration begin.
        /// </summary>
        public uint RendezVousServerPort { get; set; } = 11411;

        /// <summary>
        /// Gets or sets the ip of the rendez-vous server.
        /// </summary>
        public string RendezVousServerAddress { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets KinectStreaming application name (if there is more than one).
        /// </summary>
        public string RendezVousApplicationName { get; set; } = "RemoteKinectAzureServer";

    }
}
