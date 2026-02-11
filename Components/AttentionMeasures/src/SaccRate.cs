// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Psi component calculating the saccade rate (saccades per second) in a sliding window.
    /// </summary>
    public class SaccRate : TimedSlidingWindowComponent<EyeMovement, double>
    {
        /// <summary>
        /// Field to store the amount of saccades.
        /// </summary>
        private int saccCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaccRate"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public SaccRate(Pipeline pipeline, string name = nameof(SaccRate))
            : base(pipeline, name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaccRate"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="w">The window size.</param>
        /// <param name="name">The name of the component.</param>
        public SaccRate(Pipeline pipeline, TimeSpan w, string name = nameof(SaccRate))
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
            this.Out.Post(this.CalculateSaccRate(), envelope.OriginatingTime);
        }

        /// <summary>
        /// Calculates the saccade rate (saccades per second).
        /// </summary>
        /// <returns>The saccade rate.</returns>
        private double CalculateSaccRate()
        {
            int saccCount = 0;
            double saccRate = 0;
            if (this.inputQueue is not null)
            {
                foreach ((EyeMovement, DateTime) input in this.inputQueue)
                {
                    if (!input.Item1.IsFixation)
                    {
                        saccCount++;
                    }
                }
            }

            if (this.TimeWindow > new TimeSpan(0))
            {
                saccRate = (double)saccCount / this.TimeWindow.TotalSeconds;
            }

            return saccRate;
        }

        /// <summary>
        /// Receives and stores the saccade count.
        /// </summary>
        /// <param name="input">The saccade count.</param>
        /// <param name="envelope">The message envelope.</param>
        private void ReceiveSaccCount(int input, Envelope envelope)
        {
            this.saccCount = input.DeepClone();
        }
    }
}
