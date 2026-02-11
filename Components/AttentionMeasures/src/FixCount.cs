// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Psi component counting the amount of fixations in a sliding window.
    /// </summary>
    public class FixCount : TimedSlidingWindowComponent<EyeMovement, int>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FixCount"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public FixCount(Pipeline pipeline, string name = nameof(FixCount))
            : base(pipeline, name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FixCount"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="w">The window size.</param>
        /// <param name="name">The name of the component.</param>
        public FixCount(Pipeline pipeline, TimeSpan w, string name = nameof(FixCount))
            : base(pipeline, w, name)
        {
        }

        /// <summary>
        /// Executes upon receiving an EyeMovement.
        /// </summary>
        /// <param name="input">The input eye movement.</param>
        /// <param name="envelope">The message envelope.</param>
        protected override void Receive(EyeMovement input, Envelope envelope)
        {
            base.Receive(input, envelope);
        }

        /// <inheritdoc/>
        protected override void ReceiveTimer(TimeSpan input, Envelope envelope)
        {
            base.ReceiveTimer(input, envelope);
            this.Out.Post(this.CalculateCount(), envelope.OriginatingTime);
        }

        /// <summary>
        /// Counting the amount of fixation in the queue.
        /// </summary>
        /// <returns>The fixation count.</returns>
        private int CalculateCount()
        {
            int fixationCount = 0;
            foreach ((EyeMovement, DateTime) input in this.inputQueue)
            {
                if (input.Item1.IsFixation)
                {
                    fixationCount++;
                }
            }

            return fixationCount;
        }
    }
}
