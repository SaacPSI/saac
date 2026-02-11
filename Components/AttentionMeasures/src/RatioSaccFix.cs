// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Psi component calculating the ratio of saccade time to fixation time in a sliding window.
    /// </summary>
    public class RatioSaccFix : TimedSlidingWindowComponent<EyeMovement, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RatioSaccFix"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public RatioSaccFix(Pipeline pipeline, string name = nameof(RatioSaccFix))
            : base(pipeline, name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RatioSaccFix"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="w">The window size.</param>
        /// <param name="name">The name of the component.</param>
        public RatioSaccFix(Pipeline pipeline, TimeSpan w, string name = nameof(RatioSaccFix))
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
            this.Out.Post(this.CalculateRatioSaccFix(), envelope.OriginatingTime);
        }

        /// <summary>
        /// Calculates the ratio of saccade time to fixation time.
        /// </summary>
        /// <returns>The ratio of saccade time to fixation time.</returns>
        private double CalculateRatioSaccFix()
        {
            TimeSpan saccTime = new TimeSpan(0);
            TimeSpan fixTime = new TimeSpan(0);
            double ratioSaacFix = 0;
            if (this.inputQueue is not null)
            {
                foreach ((EyeMovement, DateTime) input in this.inputQueue)
                {
                    if (input.Item1.IsFixation)
                    {
                        fixTime = fixTime + input.Item1.GetDuration();
                    }
                    else
                    {
                        saccTime = saccTime + input.Item1.GetDuration();
                    }
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
