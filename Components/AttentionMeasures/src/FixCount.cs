using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    //Psi component counting the amount of fixations in a sliding window
    public class FixCount : TimedSlidingWindowComponent<EyeMovement, int>
    {
        //Constructors
        public FixCount(Pipeline pipeline, string name = nameof(FixCount)) : base(pipeline, name) { }
        public FixCount(Pipeline pipeline, TimeSpan w, string name = nameof(FixCount)) : base(pipeline, w, name) { }

        //Executes upon receiving an EyeMovement
        protected override void Receive(EyeMovement input, Envelope envelope)
        {
            base.Receive(input, envelope);
        }

        protected override void ReceiveTimer(TimeSpan input, Envelope envelope)
        {
            base.ReceiveTimer(input, envelope);
            this.Out.Post(CalculateCount(), envelope.OriginatingTime);
        }

        //Counting the amount of fixation in the queue
        private int CalculateCount()
        {
            int fixationCount = 0;
            foreach ((EyeMovement, DateTime) input in inputQueue)
            {
                if (input.Item1.isFixation) { fixationCount++; }
            }
            return fixationCount;
        }
    }
}