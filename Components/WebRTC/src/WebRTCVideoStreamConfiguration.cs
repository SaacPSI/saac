// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    /// <summary>
    /// Configuration for the WebRTC video stream.
    /// </summary>
    public class WebRTCVideoStreamConfiguration : WebRTCDataConnectorConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether audio streaming is enabled.
        /// </summary>
        public bool AudioStreaming { get; set; } = false;

        /// <summary>
        /// Gets or sets the FFMPEG full path.
        /// </summary>
        public string? FFMPEGFullPath { get; set; } = null;
    }
}
