// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    using Microsoft.Psi;
    using TinyJson;

    /// <summary>
    /// WebRTC data receiver for JSON messages.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    public class WebRTCDataReceiverToChannelJson<T> : WebRTCDataReceiverToChannel<T>
    {
        private OnMessageDelegate<string>? onMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRTCDataReceiverToChannelJson{T}"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="label">The channel label.</param>
        internal WebRTCDataReceiverToChannelJson(Pipeline parent, string label)
            : base(parent, label)
        {
            this.Type = MessageType.Json;
        }

        /// <inheritdoc/>
        public override void SetOnMessageDelegateJson(OnMessageDelegate<string> onMessage)
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

            JsonStructT jSONStructT = default(JsonStructT);
            jSONStructT.Data = message;
            jSONStructT.Timestamp = envelope.OriginatingTime.ToString();
            this.onMessage(JSONWriter.ToJson(jSONStructT), this.name);
        }

        private struct JsonStructT
        {
            public string Timestamp;
            public T Data;
        }
    }
}