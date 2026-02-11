// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.LabStreamLayer
{
    using static LSL.liblsl;

    /// <summary>
    /// Interface for Lab Streaming Layer (LSL) components.
    /// </summary>
    public interface ILabStreamLayerComponent
    {
        /// <summary>
        /// Gets the stream information.
        /// </summary>
        /// <returns>The stream information object.</returns>
        StreamInfo GetStreamInfo();

        /// <summary>
        /// Gets the type of data in the stream channels.
        /// </summary>
        /// <returns>The channel data type, or null if unknown.</returns>
        Type? GetStreamChannelType();

        /// <summary>
        /// Disposes the component and releases resources.
        /// </summary>
        void Dispose();
    }
}
