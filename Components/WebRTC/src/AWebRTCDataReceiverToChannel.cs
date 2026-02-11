// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    /// <summary>
    /// Base class for WebRTC data receiver to channel.
    /// </summary>
    public abstract class AWebRTCDataReceiverToChannel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AWebRTCDataReceiverToChannel"/> class.
        /// </summary>
        public AWebRTCDataReceiverToChannel()
        {
        }

        /// <summary>
        /// Delegate for message handling.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="label">The channel label.</param>
        /// <returns>True if successful.</returns>
        public delegate bool OnMessageDelegate<T>(T message, string label);

        /// <summary>
        /// Message type enumeration.
        /// </summary>
        public enum MessageType
        {
            /// <summary>
            /// JSON message type.
            /// </summary>
            Json,

            /// <summary>
            /// Binary message type.
            /// </summary>
            Bytes,
        }

        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public MessageType Type { get; protected set; } = MessageType.Json;

        /// <summary>
        /// Sets the JSON message delegate.
        /// </summary>
        /// <param name="onMessage">The delegate.</param>
        public virtual void SetOnMessageDelegateJson(OnMessageDelegate<string> onMessage)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the bytes message delegate.
        /// </summary>
        /// <param name="onMessage">The delegate.</param>
        public virtual void SetOnMessageDelegateBytes(OnMessageDelegate<byte[]> onMessage)
        {
            throw new NotImplementedException();
        }
    }
}