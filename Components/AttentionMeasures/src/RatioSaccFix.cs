using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    public class RatioSaccFix : TimedSlidingWindowComponent<EyeMovement, double>
    {
        //Constructors
        public RatioSaccFix(Pipeline pipeline, string name = nameof(RatioSaccFix)) : base(pipeline, name) { }
        public RatioSaccFix(Pipeline pipeline, TimeSpan w, string name = nameof(RatioSaccFix)) : base(pipeline, w, name) { }

        //Executes upon receiving a message
        protected override void Receive(EyeMovement input, Envelope envelope)
        {
            base.Receive(input, envelope);
        }

        //Executes upon receiving the timer
        protected override void ReceiveTimer(TimeSpan input, Envelope envelope)
        {
            base.ReceiveTimer(input, envelope);
            this.Out.Post(CalculateRatioSaccFix(), envelope.OriginatingTime);
        }

        //Calculating the mean duration of fixations
        private double CalculateRatioSaccFix()
        {
            TimeSpan saccTime = new TimeSpan(0);
            TimeSpan fixTime = new TimeSpan(0);
            double ratioSaacFix = 0;
            if (inputQueue is not null)
            {
                foreach ((EyeMovement, DateTime) input in inputQueue)
                {
                    if (input.Item1.isFixation) { fixTime = fixTime + input.Item1.GetDuration(); }
                    else { saccTime = saccTime + input.Item1.GetDuration(); }
                }
            }
            if (fixTime > new TimeSpan(0))
            {
                ratioSaacFix = (double)saccTime.TotalSeconds / fixTime.TotalSeconds;

            }
            return ratioSaacFix;
        }
    }
}