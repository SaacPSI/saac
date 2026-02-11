// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    using Microsoft.Psi.Remoting;

    /// <summary>
    /// Configuration for Whisper remote streams.
    /// </summary>
    public class WhisperRemoteStreamsConfiguration
    {
        /// <summary>
        /// Gets or sets the rendezvous server address.
        /// </summary>
        public string RendezVousAddress { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the rendezvous server port.
        /// </summary>
        public int RendezVousPort { get; set; } = 13331;

        /// <summary>
        /// Gets or sets the export port.
        /// </summary>
        public int ExportPort { get; set; } = 11570;

        /// <summary>
        /// Gets or sets the connection type.
        /// </summary>
        public TransportKind ConnectionType { get; set; } = TransportKind.Tcp;

        /// <summary>
        /// Gets or sets the rendezvous application name.
        /// </summary>
        public string RendezVousApplicationName { get; set; } = "WhisperStreaming";
    }
}