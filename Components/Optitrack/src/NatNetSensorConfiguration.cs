// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using NatNetML;

namespace SAAC.NatNetComponent
{
    /// <summary>
    /// Configuration settings for the NatNet Optitrack component.
    /// </summary>
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
        /// Gets or sets a value indicating whether gets or sets debugging mode.
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
