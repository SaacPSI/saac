using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    //
    public abstract class TimedSlidingWindowComponent<TIn, TOut> where TIn : class
    {
        TIn lastInput;
        //Queue saving the messages in the sliding window
        protected Queue<(TIn, DateTime)> inputQueue = new Queue<(TIn, DateTime)>();
        //Length of the sliding window
        public TimeSpan timeWindow;

        // Receiver for the input
        public Receiver<TIn> In { get; private set; }
        // Receiver for the timer
        public Receiver<TimeSpan> TimerIn { get; private set; }
        // Emitter for the output stream
        public Emitter<TOut> Out { get; private set; }
        public Emitter<int> QueueCountOut { get; private set; }

        protected string name;

        //Constructors
        public TimedSlidingWindowComponent(Pipeline pipeline, string name)
        {
            this.name = name;
            Initialize(pipeline);
        }

        public TimedSlidingWindowComponent(Pipeline pipeline, TimeSpan window, string name)
        {
            this.name = name;
            timeWindow = window;
            Initialize(pipeline);
        }

        private void Initialize(Pipeline pipeline)
        {

            In = pipeline.CreateReceiver<TIn>(this, Receive, $"{name}-In");
            TimerIn = pipeline.CreateReceiver<TimeSpan>(this, ReceiveTimer, $"{name}-TimerIn");
            Out = pipeline.CreateEmitter<TOut>(this, $"{name}-Out");
            QueueCountOut = pipeline.CreateEmitter<int>(this, $"{name}-QueueCountOut");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        protected virtual void Receive(TIn input, Envelope envelope)
        {
            AddToQueue(input, envelope.OriginatingTime);
            lastInput = input;
        }

        protected virtual void ReceiveTimer(TimeSpan input, Envelope envelope)
        {
            UpdateQueue(envelope.OriginatingTime);
        }

        //Adding the new input to the queue
        protected void AddToQueue(TIn input, DateTime t)
        {
            if (lastInput is null) { inputQueue.Enqueue((input.DeepClone(), t)); }
            else if (lastInput == input) { inputQueue.Enqueue((input.DeepClone(), t)); }
        }

        //Updating the queue to remove inputs which are no more in the time window
        protected void UpdateQueue(DateTime t)
        {
            if (inputQueue is not null)
            {
                //Removing inputs until they are in the window
                while (inputQueue.Count() > 0 && inputQueue.Peek().Item2 < (t - timeWindow))
                {
                    inputQueue.Dequeue();
                }
            }
            QueueCountOut.Post(inputQueue.Count(), t);
        }
    }
}