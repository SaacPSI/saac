// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Abstract base class for Psi components that process data in a timed sliding window.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    public abstract class TimedSlidingWindowComponent<TIn, TOut>
        where TIn : class
    {
        private TIn lastInput;

        /// <summary>
        /// Queue saving the messages in the sliding window.
        /// </summary>
        protected Queue<(TIn, DateTime)> inputQueue = new Queue<(TIn, DateTime)>();

        /// <summary>
        /// Gets or sets the length of the sliding window.
        /// </summary>
        public TimeSpan TimeWindow;

        /// <summary>
        /// Gets the receiver for the input.
        /// </summary>
        public Receiver<TIn> In { get; private set; }

        /// <summary>
        /// Gets the receiver for the timer.
        /// </summary>
        public Receiver<TimeSpan> TimerIn { get; private set; }

        /// <summary>
        /// Gets the emitter for the output stream.
        /// </summary>
        public Emitter<TOut> Out { get; private set; }

        /// <summary>
        /// Gets the emitter for the queue count output.
        /// </summary>
        public Emitter<int> QueueCountOut { get; private set; }

        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        protected string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedSlidingWindowComponent{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public TimedSlidingWindowComponent(Pipeline pipeline, string name)
        {
            this.name = name;
            this.Initialize(pipeline);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedSlidingWindowComponent{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="window">The window size.</param>
        /// <param name="name">The name of the component.</param>
        public TimedSlidingWindowComponent(Pipeline pipeline, TimeSpan window, string name)
        {
            this.name = name;
            this.TimeWindow = window;
            this.Initialize(pipeline);
        }

        /// <summary>
        /// Initializes the receivers and emitters.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        private void Initialize(Pipeline pipeline)
        {
            this.In = pipeline.CreateReceiver<TIn>(this, this.Receive, $"{this.name}-In");
            this.TimerIn = pipeline.CreateReceiver<TimeSpan>(this, this.ReceiveTimer, $"{this.name}-TimerIn");
            this.Out = pipeline.CreateEmitter<TOut>(this, $"{this.name}-Out");
            this.QueueCountOut = pipeline.CreateEmitter<int>(this, $"{this.name}-QueueCountOut");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Receives and processes an input message.
        /// </summary>
        /// <param name="input">The input message.</param>
        /// <param name="envelope">The message envelope.</param>
        protected virtual void Receive(TIn input, Envelope envelope)
        {
            this.AddToQueue(input, envelope.OriginatingTime);
            this.lastInput = input;
        }

        /// <summary>
        /// Receives and processes a timer message.
        /// </summary>
        /// <param name="input">The timer input.</param>
        /// <param name="envelope">The message envelope.</param>
        protected virtual void ReceiveTimer(TimeSpan input, Envelope envelope)
        {
            this.UpdateQueue(envelope.OriginatingTime);
        }

        /// <summary>
        /// Adds a new input to the queue.
        /// </summary>
        /// <param name="input">The input to add.</param>
        /// <param name="t">The timestamp.</param>
        protected void AddToQueue(TIn input, DateTime t)
        {
            if (this.lastInput is null)
            {
                this.inputQueue.Enqueue((input.DeepClone(), t));
            }
            else if (this.lastInput == input)
            {
                this.inputQueue.Enqueue((input.DeepClone(), t));
            }
        }

        /// <summary>
        /// Updates the queue to remove inputs that are no longer in the time window.
        /// </summary>
        /// <param name="t">The current timestamp.</param>
        protected void UpdateQueue(DateTime t)
        {
            if (this.inputQueue is not null)
            {
                // Removing inputs until they are in the window
                while (this.inputQueue.Count() > 0 && this.inputQueue.Peek().Item2 < (t - this.TimeWindow))
                {
                    this.inputQueue.Dequeue();
                }
            }

            this.QueueCountOut.Post(this.inputQueue.Count(), t);
        }
    }
}
