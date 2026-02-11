// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Remoting;

    /// <summary>
    /// Configuration settings for Kinect remote streaming over a network.
    /// Extends KinectSensorConfiguration with network and encoding settings.
    /// </summary>
    public class KinectRemoteStreamsComponentConfiguration : KinectSensorConfiguration
    {
        /// <summary>
        /// Gets or sets the JPEG encoding quality level for video streams (0-100).
        /// Higher values produce better quality but larger files.
        /// </summary>
        public int EncodingVideoLevel { get; set; } = 90;

        /// <summary>
        /// Gets or sets the network transport type to use for streaming.
        /// </summary>
        public TransportKind ConnectionType { get; set; } = TransportKind.Tcp;

        /// <summary>
        /// Gets or sets the IP address to use for the remote connection.
        /// </summary>
        public string IpToUse { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the starting port number for the remote exporters.
        /// Each stream will use a sequential port starting from this value.
        /// </summary>
        public int StartingPort { get; set; } = 11411;

        /// <summary>
        /// Gets or sets the application name used in the rendezvous process.
        /// </summary>
        public string RendezVousApplicationName { get; set; } = "RemoteKinectAzureServer";
    }
}
