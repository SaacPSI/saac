// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    using System.Net;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Configuration for the WebRTC connector.
    /// </summary>
    public class WebRTConnectorConfiguration
    {
        /// <summary>
        /// Gets or sets the WebSocket port.
        /// </summary>
        public uint WebsocketPort { get; set; } = 80;

        /// <summary>
        /// Gets or sets the WebSocket address.
        /// </summary>
        public IPAddress WebsocketAddress { get; set; } = IPAddress.Any;

        /// <summary>
        /// Gets or sets a value indicating whether to use Pixel Streaming connection.
        /// </summary>
        public bool PixelStreamingConnection { get; set; } = false;

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        public LogLevel Log { get; set; } = LogLevel.Trace;
    }
}
