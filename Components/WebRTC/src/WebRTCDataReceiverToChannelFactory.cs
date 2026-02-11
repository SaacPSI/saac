// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    using Microsoft.Psi;

    /// <summary>
    /// Factory for creating WebRTC data receivers.
    /// </summary>
    public static class WebRTCDataReceiverToChannelFactory
    {
        /// <summary>
        /// Creates a new WebRTC data receiver.
        /// </summary>
        /// <typeparam name="T">The data type.</typeparam>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="label">The channel label.</param>
        /// <param name="hasBytesMethod">Whether the type has a bytes conversion method.</param>
        /// <returns>The created receiver.</returns>
        public static WebRTCDataReceiverToChannel<T> Create<T>(Pipeline parent, string label, bool hasBytesMethod = false)
        {
            if (hasBytesMethod)
            {
                foreach (var method in typeof(T).GetMethods())
                {
                    if (method.ReturnType == typeof(byte[]))
                    {
                        return new WebRTCDataReceiverToChannelBytes<T>(parent, label, method);
                    }
                }
            }

            return new WebRTCDataReceiverToChannelJson<T>(parent, label);
        }
    }
}
