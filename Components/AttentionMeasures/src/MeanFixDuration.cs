using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    //Psi component calculating the mean duration of fixations in a sliding window
    public class MeanFixDuration : TimedSlidingWindowComponent<EyeMovement, TimeSpan>
    {
        //Constructors
        public MeanFixDuration(Pipeline pipeline, string name = nameof(FixCountByObjects)) : base(pipeline, name) { }
        public MeanFixDuration(Pipeline pipeline, TimeSpan w, string name = nameof(FixCountByObjects)) : base(pipeline, w, name) { }

        //Executes upon receiving a message
        protected override void Receive(EyeMovement input, Envelope envelope)
        {
            base.Receive(input, envelope);
        }

        //Executes upon receiving the timer
        protected override void ReceiveTimer(TimeSpan input, Envelope envelope)
        {
            base.ReceiveTimer(input, envelope);
            this.Out.Post(CalculateMeanDuration(), envelope.OriginatingTime);
        }

        //Calculating the mean duration of fixations
        private TimeSpan CalculateMeanDuration()
        {
            int fixCount = 0;
            TimeSpan meanFixDuration = new TimeSpan(0);
            if (inputQueue is not null)
            {
                foreach ((EyeMovement, DateTime) input in inputQueue)
                {
                    if (input.Item1.isFixation) { fixCount++; meanFixDuration = meanFixDuration + input.Item1.GetDuration(); }
                }
                if (fixCount > 0)
                {
                    meanFixDuration = TimeSpan.FromSeconds((double)meanFixDuration.TotalSeconds / fixCount);
                }
            }
            return meanFixDuration;
        }
    }
}