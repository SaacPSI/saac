// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.RemoteConnectors
{
    /// <summary>
    /// Configuration for the Unreal Remote Connector.
    /// </summary>
    public class UnrealRemoteConnectorConfiguration
    {
        /// <summary>
        /// Gets or sets the address of the unreal webserver.
        /// </summary>
        public string Address { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets a value indicating whether to emit request from the input.
        /// </summary>
        public bool ForwardAction { get; set; } = false;
    }
}
