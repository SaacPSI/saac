namespace RemoteConnectors
{
    public class KinectAzureRemoteConnectorConfiguration
    {
        /// <summary>
        /// Gets or sets port number where the iteration begin.
        /// </summary>
        public uint ServeurtPort { get; set; } = 13331;

        /// <summary>
        /// Gets or sets KinectStreaming application name (if there is more than one).
        /// </summary>
        public string ApplicationName { get; set; } = "KinectStreaming";

        /// <summary>
        /// Gets or sets the ip of the rendez-vous server.
        /// </summary>
        public string Address { get; set; } = "localhost";
    }
}
