// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Psi component that displays the ranking score for a specific object.
    /// </summary>
    public class ObjectRankDisplay
    {
        /// <summary>
        /// Gets the receiver for the objects ranking dictionary.
        /// </summary>
        public Receiver<Dictionary<(int, string), double>> In { get; private set; }

        /// <summary>
        /// Gets the emitter for the score output.
        /// </summary>
        public Emitter<double> ScoreOut { get; private set; }

        /// <summary>
        /// Gets the emitter for the name output.
        /// </summary>
        public Emitter<string> NameOut { get; private set; }

        private string objectName;
        private int objectNumber;
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRankDisplay"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="objectName">The name of the object to display.</param>
        /// <param name="name">The name of the component.</param>
        public ObjectRankDisplay(Pipeline pipeline, string objectName, string name = nameof(ObjectRankDisplay))
        {
            this.objectName = objectName;
            this.name = name;
            this.InitialiseReceiverEmitter(pipeline);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRankDisplay"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="objectNumber">The index of the object to display.</param>
        /// <param name="name">The name of the component.</param>
        public ObjectRankDisplay(Pipeline pipeline, int objectNumber, string name = nameof(ObjectRankDisplay))
        {
            this.objectNumber = objectNumber;
            this.name = name;
            this.InitialiseReceiverEmitter(pipeline);
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Initializes the receiver and emitters.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        private void InitialiseReceiverEmitter(Pipeline pipeline)
        {
            this.In = pipeline.CreateReceiver<Dictionary<(int, string), double>>(this, this.Receive, $"{this.name}-In");
            this.ScoreOut = pipeline.CreateEmitter<double>(this, $"{this.name}-RankingOut");
            this.NameOut = pipeline.CreateEmitter<string>(this, $"{this.name}-NameOut");
        }

        /// <summary>
        /// Receives and processes the ranking dictionary.
        /// </summary>
        /// <param name="input">The ranking dictionary.</param>
        /// <param name="envelope">The message envelope.</param>
        protected void Receive(Dictionary<(int, string), double> input, Envelope envelope)
        {
            double score = 0;
            string name = string.Empty;

            if (input.Count > this.objectNumber + 2)
            {
                score = input.ElementAt(this.objectNumber + 1).Value;
                name = input.ElementAt(this.objectNumber + 1).Key.Item2;
            }

            this.ScoreOut.Post(score, envelope.OriginatingTime);
            this.NameOut.Post(name, envelope.OriginatingTime);
        }
    }
}
