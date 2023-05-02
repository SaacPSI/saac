using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace Helpers
{
    /// <summary>
    /// Class that transform pipeline data to message forwarded to the delegate given.
    /// </summary>
    public class PipeToMessage<T>
    {
        /// <summary>
        /// Template connector
        /// </summary>
        private Connector<T> InConnector;

        /// <summary>
        /// Template receiver
        /// </summary>
        public Receiver<T> In => InConnector.In;

        /// <summary>
        /// Name of the component
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Delegate function that will be called at each received data
        /// </summary>
        public delegate void Do(Message<T> message);
        private Do delegateDo;

        public PipeToMessage(Pipeline parent, Do toDo, string? name = null)
        {
            Name = name ?? "PipeToMessage";
            delegateDo = toDo;
            InConnector = parent.CreateConnector<T>(nameof(In));
            InConnector.Out.Do(Process);
        }

        private void Process(T data, Envelope envelope)
        {
            Message<T> message = new Message<T>(data, envelope.OriginatingTime, envelope.CreationTime, envelope.SourceId, envelope.SequenceId);
            delegateDo(message);
        }
    }
}
