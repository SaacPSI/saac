// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Psi component for testing if consecutive eye tracking data messages are identical.
    /// </summary>
    public class TestInput : ConsumerProducer<Dictionary<ETData, IEyeTracking>, bool>
    {
        /// <summary>
        /// Last received eye tracking message.
        /// </summary>
        private Dictionary<ETData, IEyeTracking> lastInput;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestInput"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        public TestInput(Pipeline pipeline)
            : base(pipeline)
        {
            this.lastInput = new EyeTrackingTemplate().Content;
        }

        /// <summary>
        /// Receives and compares eye tracking data with the last received message.
        /// </summary>
        /// <param name="input">The input eye tracking data.</param>
        /// <param name="envelope">The message envelope.</param>
        protected override void Receive(Dictionary<ETData, IEyeTracking> input, Envelope envelope)
        {
            this.Out.Post(input == this.lastInput, envelope.OriginatingTime);

            // Updating last input
            this.lastInput = input.DeepClone();
        }
    }
}
