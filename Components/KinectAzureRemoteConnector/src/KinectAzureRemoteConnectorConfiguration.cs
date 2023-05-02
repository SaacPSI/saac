namespace RemoteConnectors
{
    public class KinectAzureRemoteConnectorConfiguration
    {
        /// <summary>
        /// Get or set the list of data to connect to.
        /// </summary>
        public uint ActiveStreamNumber { get; set; } = 1;

        /// <summary>
        /// Gets or sets port number where the iteration begin.
        /// </summary>
        public uint StartPort { get; set; } = 11411;

        /// <summary>
        /// Gets or sets the ip of the remote exporter.
        /// </summary>
        public string Address { get; set; } = "localhost";
    }
}
