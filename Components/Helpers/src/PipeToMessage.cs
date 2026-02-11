// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Helpers
{
    using Microsoft.Psi;

    /// <summary>
    /// Component that transforms pipeline data to messages forwarded to a delegate.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class PipeToMessage<T> : IConsumer<T>
    {
        private readonly Do delegateDo;
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipeToMessage{T}"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="toDo">The delegate function to call for each received message.</param>
        /// <param name="name">The name of the component.</param>
        public PipeToMessage(Pipeline parent, Do toDo, string name = nameof(PipeToMessage<T>))
        {
            this.name = name;
            this.delegateDo = toDo;
            this.In = parent.CreateReceiver<T>(this, this.Process, $"{name}-In");
        }

        /// <summary>
        /// Delegate function that will be called for each received message.
        /// </summary>
        /// <param name="message">The message containing data and envelope information.</param>
        public delegate void Do(Message<T> message);

        /// <summary>
        /// Gets the receiver for input data.
        /// </summary>
        public Receiver<T> In { get; private set; }

        /// <summary>
        /// Processes received data, creating a Message and forwarding it to the delegate.
        /// </summary>
        /// <param name="data">The received data.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process(T data, Envelope envelope)
        {
            Message<T> message = new Message<T>(data, envelope.OriginatingTime, envelope.CreationTime, envelope.SourceId, envelope.SequenceId);
            this.delegateDo(message);
        }
    }
}
