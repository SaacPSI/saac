// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Enumeration of gaze agitation states.
    /// </summary>
    public enum GazeAgitationState
    {
        /// <summary>Agitated gaze state.</summary>
        AgitatedGaze = 1,

        /// <summary>Undetermined gaze state.</summary>
        UndeterminedGaze = 0,

        /// <summary>Calm gaze state.</summary>
        CalmGaze = -1
    }

    /// <summary>
    /// Psi component counting the amount of fixations in a sliding window.
    /// </summary>
    public class GazeAgitation
    {
        // Thresholds
        private (int, int) fixCountThresholds;
        private (TimeSpan, TimeSpan) meanFixDurationThresholds;
        private (double, double) ratioSaccFixThresholds;
        private (double, double) saccRateThresholds;

        /// <summary>
        /// Gets the receiver for previous components.
        /// </summary>
        public Receiver<(int, TimeSpan, double, double)> In { get; private set; }

        /// <summary>
        /// Gets the emitter for GazeAgitation.
        /// </summary>
        public Emitter<GazeAgitationState> Out { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GazeAgitation"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to attach to.</param>
        public GazeAgitation(Pipeline pipeline)
        {
            this.In = pipeline.CreateReceiver<(int, TimeSpan, double, double)>(this, this.Receive, nameof(this.In));
            this.Out = pipeline.CreateEmitter<GazeAgitationState>(this, nameof(this.Out));
            this.fixCountThresholds = (3, 6);
            this.meanFixDurationThresholds = (TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(101));
            this.ratioSaccFixThresholds = (0.3, 0.6);
            this.saccRateThresholds = (0.2, 0.5);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GazeAgitation"/> class with custom thresholds.
        /// </summary>
        /// <param name="pipeline">The pipeline to attach to.</param>
        /// <param name="fixCountThresholds">Fix count thresholds.</param>
        /// <param name="meanFixDurationThresholds">Mean fix duration thresholds.</param>
        /// <param name="ratioSaccFixThresholds">Ratio saccade/fixation thresholds.</param>
        /// <param name="saccRateThresholds">Saccade rate thresholds.</param>
        public GazeAgitation(Pipeline pipeline, (int, int) fixCountThresholds, (TimeSpan, TimeSpan) meanFixDurationThresholds, (double, double) ratioSaccFixThresholds, (double, double) saccRateThresholds)
        {
            this.In = pipeline.CreateReceiver<(int, TimeSpan, double, double)>(this, this.Receive, nameof(this.In));
            this.Out = pipeline.CreateEmitter<GazeAgitationState>(this, nameof(this.Out));
            this.fixCountThresholds = fixCountThresholds;
            this.meanFixDurationThresholds = meanFixDurationThresholds;
            this.ratioSaccFixThresholds = ratioSaccFixThresholds;
            this.saccRateThresholds = saccRateThresholds;
        }

        /// <summary>
        /// Executes upon receiving an input.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="envelope">The message envelope.</param>
        protected void Receive((int, TimeSpan, double, double) input, Envelope envelope)
        {
            GazeAgitationState fixCountState = this.ClassifyIndicatorState(input.Item1, this.fixCountThresholds);
            GazeAgitationState meanFixDurationState = this.ClassifyIndicatorState(input.Item2, this.meanFixDurationThresholds, false);
            GazeAgitationState ratioSaccFixState = this.ClassifyIndicatorState(input.Item3, this.ratioSaccFixThresholds);
            GazeAgitationState saccRateState = this.ClassifyIndicatorState(input.Item4, this.saccRateThresholds);

            int stateSum = (int)fixCountState + (int)meanFixDurationState + (int)ratioSaccFixState + (int)saccRateState;
            if (stateSum != 0)
            {
                stateSum = stateSum / Math.Abs(stateSum);
            }

            this.Out.Post((GazeAgitationState)stateSum, envelope.OriginatingTime);
        }

        /// <summary>
        /// Classifies the indicator state based on thresholds.
        /// </summary>
        /// <typeparam name="T">The type of the indicator value.</typeparam>
        /// <param name="input">The input value.</param>
        /// <param name="thresholds">The thresholds tuple.</param>
        /// <param name="isCrescent">Whether the state is crescent (calm to agitated).</param>
        /// <returns>The gaze agitation state.</returns>
        private GazeAgitationState ClassifyIndicatorState<T>(T input, (T, T) thresholds, bool isCrescent = true)
            where T : IComparable<T>
        {
            GazeAgitationState state = GazeAgitationState.UndeterminedGaze;

            // Checking if thresholds are in the right order
            if (thresholds.Item1.CompareTo(thresholds.Item2) <= 0)
            {
                // Comparing input with thresholds :
                // -1 -> input < thresholdN
                // 0 -> input = thresholdN
                // +1 -> input > thresholdN
                int compare1 = input.CompareTo(thresholds.Item1);
                int compare2 = input.CompareTo(thresholds.Item2);

                // If input < threshold1, return calmGaze
                if (compare1 < 0)
                {
                    state = GazeAgitationState.CalmGaze;
                }

                // If input >= threshold2, return agitatedGaze
                else if (compare2 >= 0)
                {
                    state = GazeAgitationState.AgitatedGaze;
                }

                // Else, if threshold1 <= input < threshold2, return undeterminedGaze
                else
                {
                    state = GazeAgitationState.UndeterminedGaze;
                }
            }

            // If states are not decrescent, which means it follows Agitated|Undetermined|Calm and not Calm|Undetermined|Agitated, we invert the state
            if (!isCrescent)
            {
                state = (GazeAgitationState)(-(int)state);
            }

            return state;
        }
    }
}
