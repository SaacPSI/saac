// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    /// <summary>
    /// Interface for WebRTC data channel to emitter conversion.
    /// </summary>
    public interface IWebRTCDataChannelToEmitter
    {
        /// <summary>
        /// Posts binary data with timestamp.
        /// </summary>
        /// <param name="data">The binary data.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns>True if successful.</returns>
        bool Post(byte[] data, DateTime timestamp);

        /// <summary>
        /// Posts string data.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <returns>True if successful.</returns>
        bool Post(string data);
    }
}