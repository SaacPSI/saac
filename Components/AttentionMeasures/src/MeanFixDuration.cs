// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Psi component calculating the mean duration of fixations in a sliding window.
    /// </summary>
    public class MeanFixDuration : TimedSlidingWindowComponent<EyeMovement, TimeSpan>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MeanFixDuration"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public MeanFixDuration(Pipeline pipeline, string name = nameof(MeanFixDuration))
            : base(pipeline, name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeanFixDuration"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="w">The window size.</param>
        /// <param name="name">The name of the component.</param>
        public MeanFixDuration(Pipeline pipeline, TimeSpan w, string name = nameof(MeanFixDuration))
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
            this.Out.Post(this.CalculateMeanDuration(), envelope.OriginatingTime);
        }

        /// <summary>
        /// Calculates the mean duration of fixations in the sliding window.
        /// </summary>
        /// <returns>The mean duration of fixations.</returns>
        private TimeSpan CalculateMeanDuration()
        {
            int fixCount = 0;
            TimeSpan meanFixDuration = new TimeSpan(0);
            if (this.inputQueue is not null)
            {
                foreach ((EyeMovement, DateTime) input in this.inputQueue)
                {
                    if (input.Item1.IsFixation)
                    {
                        fixCount++;
                        meanFixDuration = meanFixDuration + input.Item1.GetDuration();
                    }
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
