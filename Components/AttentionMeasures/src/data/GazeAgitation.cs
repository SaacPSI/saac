using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    public enum GazeAgitationState
    {
        AgitatedGaze = 1,
        UndeterminedGaze = 0,
        CalmGaze = -1
    }


    //Psi component counting the amount of fixations in a sliding window
    public class GazeAgitation
    {
        //Thresholds
        private (int, int) fixCountThresholds;
        private (TimeSpan, TimeSpan) meanFixDurationThresholds;
        private (double, double) ratioSaccFixThresholds;
        private (double, double) saccRateThresholds;

        //Receivers for previous components
        public Receiver<(int, TimeSpan, double, double)> In { get; private set; }

        //Emitter for GazeAgitation
        public Emitter<GazeAgitationState> Out { get; private set; }

        //Constructors
        public GazeAgitation(Pipeline pipeline)
        {
            this.In = pipeline.CreateReceiver<(int, TimeSpan, double, double)>(this, Receive, nameof(this.In));
            this.Out = pipeline.CreateEmitter<GazeAgitationState>(this, nameof(this.Out));
            fixCountThresholds = (3, 6);
            meanFixDurationThresholds = (TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(101));
            ratioSaccFixThresholds = (0.3, 0.6);
            saccRateThresholds = (0.2, 0.5);
        }
        public GazeAgitation(Pipeline pipeline, (int, int) fixCountThresholds, (TimeSpan, TimeSpan) meanFixDurationThresholds, (double, double) ratioSaccFixThresholds, (double, double) saccRateThresholds)
        {
            this.In = pipeline.CreateReceiver<(int, TimeSpan, double, double)>(this, Receive, nameof(this.In));
            this.Out = pipeline.CreateEmitter<GazeAgitationState>(this, nameof(this.Out));
            this.fixCountThresholds = fixCountThresholds;
            this.meanFixDurationThresholds = meanFixDurationThresholds;
            this.ratioSaccFixThresholds = ratioSaccFixThresholds;
            this.saccRateThresholds = saccRateThresholds;
        }

        //Executes upon receiving an input
        protected void Receive((int, TimeSpan, double, double) input, Envelope envelope)
        {
            GazeAgitationState fixCountState = ClassifyIndicatorState<int>(input.Item1, fixCountThresholds);
            GazeAgitationState meanFixDurationState = ClassifyIndicatorState<TimeSpan>(input.Item2, meanFixDurationThresholds, false);
            GazeAgitationState ratioSaccFixState = ClassifyIndicatorState<double>(input.Item3, ratioSaccFixThresholds);
            GazeAgitationState saccRateState = ClassifyIndicatorState<double>(input.Item4, saccRateThresholds);

            int stateSum = (int)fixCountState + (int)meanFixDurationState + (int)ratioSaccFixState + (int)saccRateState;
            if (stateSum != 0) { stateSum = stateSum / Math.Abs(stateSum); }

            Out.Post((GazeAgitationState)stateSum, envelope.OriginatingTime);

        }

        private GazeAgitationState ClassifyIndicatorState<T>(T input, (T, T) thresholds, bool isCrescent = true) where T : IComparable<T>
        {
            GazeAgitationState state = GazeAgitationState.UndeterminedGaze;


            //Checking if thresholds are in the right order
            if (thresholds.Item1.CompareTo(thresholds.Item2) <= 0)
            {

                //Comparing input with thresholds :
                // -1 -> input < thresholdN
                // 0 -> input = thresholdN
                // +1 -> input > thresholdN
                int compare1 = input.CompareTo(thresholds.Item1);
                int compare2 = input.CompareTo(thresholds.Item2);

                //If input < threshold1, return calmGaze
                if (compare1 < 0) { state = GazeAgitationState.CalmGaze; }
                //If input >= threshold2, return agitatedGaze
                else if (compare2 >= 0) { state = GazeAgitationState.AgitatedGaze; }
                //Else, if threshold1 <= input < threshold2, return undeterminedGaze
                else { state = GazeAgitationState.UndeterminedGaze; }

            }


            //If states are not decrescent, which means it follows Agitated|Undetermined|Calm and not Calm|Undetermined|Agitated, we invert the state
            if (!isCrescent) { state = (GazeAgitationState)(-(int)state); }


            return state;
        }

    }
}