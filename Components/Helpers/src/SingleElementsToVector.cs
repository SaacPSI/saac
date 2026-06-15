// <copyright file="SingleElementsToVector.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

namespace Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;

    /// <summary>
    /// A \psi component that merges N individual streams of type <typeparamref name="Tin"/> into a single
    /// stream of type <typeparamref name="Tout"/> by accumulating one message per receiver and emitting
    /// the combined result when all inputs share the same <see cref="Envelope.OriginatingTime"/>.
    /// </summary>
    /// <typeparam name="Tin">The type of each individual input element.</typeparam>
    /// <typeparam name="Tout">The type of the combined output vector.</typeparam>
    /// <remarks>
    /// Use <see cref="SingleElementsToVectorCombiner"/> to obtain predefined combiner delegates for
    /// common numeric vector types (<see cref="Vector2"/>, <see cref="Vector3"/>, <see cref="Vector4"/>,
    /// <see cref="Point3D"/>, <see cref="Vector3D"/>).
    /// </remarks>
    public class SingleElementsToVector<Tin, Tout> : IProducer<Tout>
    {
        private readonly string name;
        private readonly Func<List<Tin>, Tout> combiner;
        private readonly int count;
        private readonly Dictionary<DateTime, Tin[]> pendingElements;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleElementsToVector{Tin, Tout}"/> class.
        /// </summary>
        /// <param name="pipeline">The \psi pipeline this component belongs to.</param>
        /// <param name="count">The number of individual input receivers to create.</param>
        /// <param name="combiner">
        /// A function that converts an ordered list of <paramref name="count"/> <typeparamref name="Tin"/>
        /// elements (indexed 0 to count-1) into the output <typeparamref name="Tout"/> value.
        /// </param>
        /// <param name="name">An optional display name for this component.</param>
        public SingleElementsToVector(Pipeline pipeline, int count, Func<List<Tin>, Tout> combiner, string name = nameof(SingleElementsToVector<Tin, Tout>))
        {
            this.name = name;
            this.count = count;
            this.combiner = combiner;
            this.pendingElements = new Dictionary<DateTime, Tin[]>();
            this.Out = pipeline.CreateEmitter<Tout>(this, $"{this.name}-Out");
            this.In = new List<Receiver<Tin>>(count);
            for (int i = 0; i < count; i++)
            {
                int index = i;
                this.In.Add(pipeline.CreateReceiver<Tin>(this, (element, envelope) => this.Process(element, index, envelope), $"{this.name}-In{i}"));
            }
        }

        /// <summary>Gets the output emitter that posts the combined <typeparamref name="Tout"/> vector.</summary>
        public Emitter<Tout> Out { get; private set; }

        /// <summary>
        /// Gets the list of input receivers. Use <c>In[i]</c> to pipe the i-th source stream into this component.
        /// </summary>
        public List<Receiver<Tin>> In { get; private set; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Stores the received element at the given index for the message's originating time.
        /// Emits a combined output when all <see cref="count"/> elements for that time have arrived.
        /// </summary>
        /// <param name="element">The received element.</param>
        /// <param name="index">The receiver index (0-based).</param>
        /// <param name="envelope">The \psi message envelope carrying timing information.</param>
        private void Process(Tin element, int index, Envelope envelope)
        {
            DateTime time = envelope.OriginatingTime;
            if (!this.pendingElements.TryGetValue(time, out Tin[]? buffer))
            {
                buffer = new Tin[this.count];
                this.pendingElements[time] = buffer;
            }

            buffer[index] = element;

            bool allReceived = true;
            for (int i = 0; i < this.count; i++)
            {
                if (buffer[i] == null)
                {
                    allReceived = false;
                    break;
                }
            }

            if (allReceived)
            {
                this.pendingElements.Remove(time);
                this.Out.Post(this.combiner(new List<Tin>(buffer)), envelope.OriginatingTime);
            }
        }
    }
}
