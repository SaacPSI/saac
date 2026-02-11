// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Psi component classifying visual attention states based on gaze agitation and fixation patterns.
    /// </summary>
    public class VisualAttentionClassifier
    {
        /// <summary>
        /// Enumeration of visual attention states.
        /// </summary>
        public enum VisualAttentionState
        {
            /// <summary>Exploitation state - focused attention on specific objects.</summary>
            Exploitation,

            /// <summary>Wandering state - unfocused gaze movement.</summary>
            Wandering,

            /// <summary>Exploration state - actively searching.</summary>
            Exploration,

            /// <summary>Pursuit state - tracking moving objects.</summary>
            Pursuit,

            /// <summary>Undetermined state - cannot be classified.</summary>
            Undetermined,
        }

        /// <summary>
        /// Gets the receiver for the gaze agitation state, saccade amplitude, and fixation count by objects.
        /// </summary>
        public Receiver<(GazeAgitationState, double, Dictionary<(int, string), int>)> In { get; private set; }

        /// <summary>
        /// Gets the receiver for the timer.
        /// </summary>
        public Receiver<TimeSpan> TimerIn { get; private set; }

        /// <summary>
        /// Gets the emitter for the output visual attention state.
        /// </summary>
        public Emitter<VisualAttentionState> Out { get; private set; }

        /// <summary>
        /// Gets or sets the threshold for refixations.
        /// </summary>
        public int ReFixationsThreshold { get; set; } = 3;

        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualAttentionClassifier"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public VisualAttentionClassifier(Pipeline pipeline, string name)
        {
            this.In = pipeline.CreateReceiver<(GazeAgitationState, double, Dictionary<(int, string), int>)>(this, this.Receive, $"{name}-In");
            this.TimerIn = pipeline.CreateReceiver<TimeSpan>(this, this.ReceiveTimer, $"{name}-TimerIn");
            this.name = name;
        }

        /// <summary>
        /// Receives and processes gaze data to classify visual attention state.
        /// </summary>
        /// <param name="input">The input tuple containing gaze agitation state, amplitude, and fixation counts.</param>
        /// <param name="envelope">The message envelope.</param>
        protected void Receive((GazeAgitationState, double, Dictionary<(int, string), int>) input, Envelope envelope)
        {
            VisualAttentionState visualAttentionState = VisualAttentionState.Exploitation;

            switch (input.Item1)
            {
                case GazeAgitationState.UndeterminedGaze:
                    visualAttentionState = VisualAttentionState.Undetermined;
                    break;
                case GazeAgitationState.CalmGaze:
                    visualAttentionState = this.GetAttentionFromCalmGaze(visualAttentionState, input.Item3);
                    break;
            }

            this.Out.Post(visualAttentionState, envelope.OriginatingTime);
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Receives timer messages.
        /// </summary>
        /// <param name="input">The timer input.</param>
        /// <param name="envelope">The message envelope.</param>
        protected virtual void ReceiveTimer(TimeSpan input, Envelope envelope)
        {
        }

        private VisualAttentionState GetAttentionFromCalmGaze(VisualAttentionState visualAttentionState, Dictionary<(int, string), int> fixCountByObjects)
        {
            if (this.GetHighestNumberOfFixations(fixCountByObjects) >= this.ReFixationsThreshold)
            {
                visualAttentionState = VisualAttentionState.Exploitation;
            }
            else
            {
                visualAttentionState = VisualAttentionState.Wandering;
            }

            return visualAttentionState;
        }

        private int GetHighestNumberOfFixations(Dictionary<(int, string), int> fixCountByObjects)
        {
            int maxCount = 0;
            foreach (var kvp in fixCountByObjects)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                }
            }

            return maxCount;
        }
    }
}
