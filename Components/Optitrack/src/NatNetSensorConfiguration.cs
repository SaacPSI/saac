using NatNetML;

namespace SAAC.NatNetComponent
{
    public class NatNetCoreConfiguration
    {
        /// <summary>
        /// Gets or sets the ip of the server.
        /// </summary>
        public string ServerIP { get; set; } = "127.0.0.1";

        /// <summary>
        /// Gets or sets the local ip of.
        /// </summary>
        public string LocalIP { get; set; } = "127.0.0.1";

        /// <summary>
        /// Gets or sets the NatNet connection type.
        /// </summary>
        public NatNetML.ConnectionType ConnectionType { get; set; } = ConnectionType.Multicast;

        /// <summary>
        /// Gets or sets debugging mode.
        /// </summary>
        public bool Debug { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the rigid bodies stream is emitted.
        /// </summary>
        public bool OutputRigidBodies { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the skeletons stream is emitted.
        /// </summary>
        public bool OutputSkeletons { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the forces plates stream is emitted.
        /// </summary>
        public bool OutputForcePlates { get; set; } = false;

    }
}
