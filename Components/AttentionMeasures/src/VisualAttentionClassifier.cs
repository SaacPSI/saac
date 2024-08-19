using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    public class VisualAttentionClassifier
    {
        public enum VisualAttentionState
        {
            Exploitation,
            Wandering,
            Exploration,
            Pursuit,
            Undetermined
        }

        //  Receiver for the GazeAgitationState, the saccade amplitude, the fixcount by objects
        public Receiver<(GazeAgitationState, double, Dictionary<(int, string), int>)> In { get; private set; }
        // Receiver for the timer
        public Receiver<TimeSpan> TimerIn { get; private set; }
        // Emitter for the output stream
        public Emitter<VisualAttentionState> Out { get; private set; }
        // Threshold for refixations
        public int ReFixationsThreshold { get; set; } = 3;

        private string name;

        //Constructor
        public VisualAttentionClassifier(Pipeline pipeline, string name)
        {
            In = pipeline.CreateReceiver<(GazeAgitationState, double, Dictionary<(int, string), int>)>(this, Receive, $"{name}-In");
            TimerIn = pipeline.CreateReceiver<TimeSpan>(this, ReceiveTimer, $"{name}-TimerIn");
            this.name = name;
        }

        protected void Receive((GazeAgitationState, double, Dictionary<(int, string), int>) input, Envelope envelope)
        {
            VisualAttentionState visualAttentionState = new VisualAttentionState();

            switch (input.Item1)
            {
                case GazeAgitationState.UndeterminedGaze:
                    visualAttentionState = VisualAttentionState.Undetermined;
                    break;
                case GazeAgitationState.CalmGaze:
                    visualAttentionState = GetAttentionFromCalmGaze(visualAttentionState, input.Item3);
                    break;

            }

            Out.Post(visualAttentionState, envelope.OriginatingTime);
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        protected virtual void ReceiveTimer(TimeSpan input, Envelope envelope)
        { }

        private VisualAttentionState GetAttentionFromCalmGaze(VisualAttentionState visualAttentionState, Dictionary<(int, string), int> fixCountByObjects)
        {

            if (GetHighestNumberOfFixations(fixCountByObjects) >= ReFixationsThreshold)
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
                if (kvp.Value > maxCount) { maxCount = kvp.Value; }
            }
            return maxCount;
        }
    }
}