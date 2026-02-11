// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    using Microsoft.Psi;
    using TinyJson;

    /// <summary>
    /// Generic WebRTC data receiver to channel.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    public class WebRTCDataReceiverToChannel<T> : AWebRTCDataReceiverToChannel, IConsumer<T>
    {
        /// <summary>
        /// The component name.
        /// </summary>
        protected string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRTCDataReceiverToChannel{T}"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="label">The channel label.</param>
        internal WebRTCDataReceiverToChannel(Pipeline parent, string label)
        {
            this.name = label;
            this.In = parent.CreateReceiver<T>(this, this.Process, $"{this.name}-In");
        }

        /// <summary>
        /// Gets the input receiver.
        /// </summary>
        public Receiver<T> In { get; private set; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Called at each message recieved to process the data.
        /// </summary>
        /// <param name="message">The data.</param>
        /// <param name="envelope">The timesptamps metadata of the message.</param>
        protected virtual void Process(T message, Envelope envelope)
        {
            throw new NotImplementedException();
        }
    }
}