using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    public class SaccRate : TimedSlidingWindowComponent<EyeMovement, double>
    {
        //Int to store the amout of saccades
        private int saccCount = 0;

        //Constructors
        public SaccRate(Pipeline pipeline, string name = nameof(SaccRate)) : base(pipeline, name) { }
        public SaccRate(Pipeline pipeline, TimeSpan w, string name = nameof(SaccRate)) : base(pipeline, w, name) { }

        //Executes upon receiving a message
        protected override void Receive(EyeMovement input, Envelope envelope)
        {
            base.Receive(input, envelope);
        }

        //Executes upon receiving the timer
        protected override void ReceiveTimer(TimeSpan input, Envelope envelope)
        {
            base.ReceiveTimer(input, envelope);
            this.Out.Post(CalculateSaccRate(), envelope.OriginatingTime);
        }

        //Calculating the mean duration of fixations
        private double CalculateSaccRate()
        {
            int saccCount = 0;
            double saccRate = 0;
            if (inputQueue is not null)
            {
                foreach ((EyeMovement, DateTime) input in inputQueue)
                {
                    if (!input.Item1.isFixation) { saccCount++; }
                }
            }
            if (timeWindow > new TimeSpan(0))
            {
                saccRate = (double)saccCount / timeWindow.TotalSeconds;

            }
            return saccRate;
        }

        private void ReceiveSaccCount(int input, Envelope envelope)
        {
            saccCount = input.DeepClone();
        }
    }
}