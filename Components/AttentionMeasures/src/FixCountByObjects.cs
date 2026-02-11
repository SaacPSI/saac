// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Psi component counting the amount of fixations per object in a sliding window.
    /// </summary>
    public class FixCountByObjects : TimedSlidingWindowComponent<EyeMovement, Dictionary<(int, string), int>>
    {
        /// <summary>
        /// Dictionary to store fixations counts along with the object keys.
        /// </summary>
        private Dictionary<(int, string), int> fixCountByObjects = new Dictionary<(int, string), int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FixCountByObjects"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public FixCountByObjects(Pipeline pipeline, string name = nameof(FixCountByObjects))
            : base(pipeline, name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FixCountByObjects"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="w">The window size.</param>
        /// <param name="name">The name of the component.</param>
        public FixCountByObjects(Pipeline pipeline, TimeSpan w, string name = nameof(FixCountByObjects))
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
            this.UpdateReFixationRanks();
            this.Out.Post(this.fixCountByObjects, envelope.OriginatingTime);
        }

        /// <summary>
        /// Updates the refixations dictionary by counting fixations per object.
        /// </summary>
        private void UpdateReFixationRanks()
        {
            this.fixCountByObjects.Clear();
            foreach (var input in this.inputQueue)
            {
                if (input.Item1.IsFixation)
                {
                    (int, string) fixedObjectKey = input.Item1.FixedObjectKey.DeepClone();
                    if (this.fixCountByObjects.ContainsKey(fixedObjectKey))
                    {
                        this.fixCountByObjects[fixedObjectKey]++;
                    }
                    else
                    {
                        this.fixCountByObjects.Add(fixedObjectKey, 1);
                    }
                }
            }
        }
    }
}
