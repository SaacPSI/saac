// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices.Helpers
{
    using Microsoft.Psi;

    /// <summary>
    /// Class that transform pipeline data to message forwarded to the delegate given.
    /// </summary>
    /// <typeparam name="T">The type of the data to receive and transform to message.</typeparam>
    public class PipeToMessage<T> : IConsumer<T>
    {
        /// <summary>
        /// Gets template receiver.
        /// </summary>
        public Receiver<T> In { get; private set; }

        /// <summary>
        /// Delegate function that will be called at each received data.
        /// </summary>
        /// <param name="source">The source name of the message.</param>
        /// <param name="message">The message created from the received data.</param>
        public delegate void Do(string source, Message<T> message);

        private Do delegateDo;
        private string name;
        private string sourceName;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipeToMessage{T}"/> class.
        /// </summary>
        /// <param name="parent">The pipeline to use for loading.</param>
        /// <param name="toDo">The delegate to trigger.</param>
        /// <param name="sourceName">The source name of the message.</param>
        /// <param name="name">The name of the loader.</param>
        public PipeToMessage(Pipeline parent, Do toDo, string sourceName, string name = nameof(PipeToMessage<T>))
        {
            this.name = name;
            this.sourceName = sourceName;
            this.delegateDo = toDo;
            this.In = parent.CreateReceiver<T>(this, this.Process, $"{name}-In");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(T data, Envelope envelope)
        {
            Message<T> message = new Message<T>(data, envelope.OriginatingTime, envelope.CreationTime, envelope.SourceId, envelope.SequenceId);
            this.delegateDo(this.sourceName, message);
        }
    }
}
