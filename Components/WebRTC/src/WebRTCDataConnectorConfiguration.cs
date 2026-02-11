// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    /// <summary>
    /// Configuration for the WebRTC data connector.
    /// </summary>
    public class WebRTCDataConnectorConfiguration : WebRTConnectorConfiguration
    {
        /// <summary>
        /// Gets or sets the input channels dictionary.
        /// </summary>
        public Dictionary<string, AWebRTCDataReceiverToChannel> InputChannels { get; set; } = new Dictionary<string, AWebRTCDataReceiverToChannel>();

        /// <summary>
        /// Gets or sets the output channels dictionary.
        /// </summary>
        public Dictionary<string, IWebRTCDataChannelToEmitter> OutputChannels { get; set; } = new Dictionary<string, IWebRTCDataChannelToEmitter>();
    }
}
