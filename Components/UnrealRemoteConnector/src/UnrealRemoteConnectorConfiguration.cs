namespace SAAC.RemoteConnectors
{
    public class UnrealRemoteConnectorConfiguration
    {
        /// <summary>
        /// Gets or sets the address of the unreal webserver.
        /// </summary>
        public string Address { get; set; } = "localhost";

        /// <summary>
        /// Emit request from the input.
        /// </summary>
        public bool ForwardAction { get; set; } = false;
    }
}
