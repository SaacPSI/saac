// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    /// <summary>
    /// Configuration settings for connecting to a remote Kinect Azure device via rendezvous.
    /// </summary>
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

        /// <summary>
        /// Gets or sets display debug info on message recieved.
        /// </summary>
        public bool Debug { get; set; } = false;
    }
}
