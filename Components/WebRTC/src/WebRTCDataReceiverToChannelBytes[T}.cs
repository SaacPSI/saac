// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    using Microsoft.Psi;

    /// <summary>
    /// WebRTC data receiver for binary messages.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    public class WebRTCDataReceiverToChannelBytes<T> : WebRTCDataReceiverToChannel<T>
    {
        private OnMessageDelegate<byte[]>? onMessage;
        private System.Reflection.MethodInfo toBytesMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRTCDataReceiverToChannelBytes{T}"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="label">The channel label.</param>
        /// <param name="toBytesMethod">The method to convert to bytes.</param>
        internal WebRTCDataReceiverToChannelBytes(Pipeline parent, string label, System.Reflection.MethodInfo toBytesMethod)
             : base(parent, label)
        {
            this.toBytesMethod = toBytesMethod;
            this.Type = MessageType.Bytes;
        }

        /// <inheritdoc/>
        public override void SetOnMessageDelegateBytes(OnMessageDelegate<byte[]> onMessage)
        {
            this.onMessage = onMessage;
        }

        /// <inheritdoc/>
        protected override void Process(T message, Envelope envelope)
        {
            if (this.onMessage == null)
            {
                return;
            }

            byte[] buffer = (byte[])this.toBytesMethod.Invoke(message, null);
            this.onMessage(buffer, this.name);
        }
    }
}