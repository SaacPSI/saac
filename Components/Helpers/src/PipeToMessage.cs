using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace SAAC.Helpers
{
    /// <summary>
    /// Class that transform pipeline data to message forwarded to the delegate given.
    /// </summary>
    public class PipeToMessage<T> : IConsumer<T>
    {
        /// <summary>
        /// Template receiver
        /// </summary>
        public Receiver<T> In { get; private set; }

        /// <summary>
        /// Delegate function that will be called at each received data
        /// </summary>
        public delegate void Do(Message<T> message);
        private Do delegateDo;
        private string name;

        public PipeToMessage(Pipeline parent, Do toDo, string name = nameof(PipeToMessage<T>))
        {
            this.name = name;
            delegateDo = toDo;
            In = parent.CreateReceiver<T>(this, Process, $"{name}-In");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(T data, Envelope envelope)
        {
            Message<T> message = new Message<T>(data, envelope.OriginatingTime, envelope.CreationTime, envelope.SourceId, envelope.SequenceId);
            delegateDo(message);
        }
    }
}
