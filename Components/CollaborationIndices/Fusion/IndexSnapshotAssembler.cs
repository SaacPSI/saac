// <copyright file="IndexSnapshotAssembler.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Gathers the indices of each tick into one <see cref="IndexSnapshot"/>.
    ///
    /// Why it exists: the score, the graph and the export used to read each index from its own
    /// receiver and to compute as soon as the first one arrived. \psi delivers the receivers of
    /// a component in no guaranteed order, so the score of tick T was built from one fresh
    /// index and the values of tick T-1 for all the others, and which one was fresh changed
    /// from one run to the next.
    ///
    /// How it works: every clock driven indicator posts, after handling a tick, whether it
    /// published for that tick (TickProcessedOut). For each tick the assembler therefore knows
    /// exactly which values are still to come, waits for those and only those, and emits the
    /// snapshot. Nothing is guessed from arrival order and no timeout is involved, so the result
    /// is the same at any replay speed and with any number of threads.
    ///
    /// An input declared without a pulse is not aligned: the snapshot carries its latest value.
    /// Use it for a stream that does not follow the clock of the indices (attention level).
    /// </summary>
    public class IndexSnapshotAssembler : IProducer<IndexSnapshot>
    {
        private readonly Pipeline pipeline;
        private readonly IndexSnapshotAssemblerConfiguration configuration;
        private readonly string name;

        private readonly Queue<DateTime> pendingTicks = new Queue<DateTime>();
        private readonly List<Queue<Marker>> pulses = new List<Queue<Marker>>();
        private readonly List<bool> pulsePublished = new List<bool>();
        private readonly List<AlignedInput> alignedInputs = new List<AlignedInput>();
        private readonly Dictionary<string, Dictionary<int, bool>> validityParts = new Dictionary<string, Dictionary<int, bool>>();
        private readonly IndexSnapshot state = new IndexSnapshot();

        private DateTime lastTick = DateTime.MinValue;
        private int inputCount;
        private bool warned;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexSnapshotAssembler"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline hosting the component.</param>
        /// <param name="configuration">Configuration, or null for the defaults.</param>
        /// <param name="name">Name of the component, prefix of its streams.</param>
        public IndexSnapshotAssembler(Pipeline pipeline, IndexSnapshotAssemblerConfiguration? configuration = null, string name = nameof(IndexSnapshotAssembler))
        {
            this.pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            this.configuration = configuration ?? new IndexSnapshotAssemblerConfiguration();
            this.name = name;

            this.TickIn = pipeline.CreateReceiver<bool>(this, this.ReceiveTick, $"{name}-Tick");
            this.Out = pipeline.CreateEmitter<IndexSnapshot>(this, $"{name}-Snapshot");
        }

        /// <summary>Gets the clock of the indices: one snapshot is emitted per tick received here.</summary>
        public Receiver<bool> TickIn { get; }

        /// <summary>Gets the snapshots, one per tick, in order.</summary>
        public Emitter<IndexSnapshot> Out { get; }

        /// <summary>
        /// Gets the number of snapshots emitted without waiting for every announced value,
        /// because more than MaximumPendingTicks ticks were waiting. Zero in a sound pipeline.
        /// </summary>
        public long ForcedSnapshotCount { get; private set; }

        /// <summary>
        /// Declares the completion stream of a clock driven indicator.
        /// </summary>
        /// <param name="tickProcessed">One message per tick: true when the indicator published for it.</param>
        /// <returns>Identifier to pass when declaring the outputs of that indicator.</returns>
        public int AddPulse(IProducer<bool> tickProcessed)
        {
            if (tickProcessed == null)
            {
                throw new ArgumentNullException(nameof(tickProcessed));
            }

            int id = this.pulses.Count;
            var queue = new Queue<Marker>();
            this.pulses.Add(queue);
            this.pulsePublished.Add(false);

            Receiver<bool> receiver = this.pipeline.CreateReceiver<bool>(
                this,
                (published, envelope) =>
                {
                    queue.Enqueue(new Marker(envelope.OriginatingTime, published));
                    this.Drain();
                },
                $"{this.name}-Pulse-{id}");

            tickProcessed.PipeTo(receiver);
            return id;
        }

        /// <summary>Declares a group level value.</summary>
        /// <param name="indexName">Name of the index in the snapshot.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="pulse">Pulse of the indicator that produces it, or null for a stream that does not follow the clock.</param>
        public void AddGroup(string indexName, IProducer<double> stream, int? pulse = null)
            => this.AddInput(stream, pulse, value => value, value => this.state.Group[indexName] = value, $"Group-{indexName}");

        /// <summary>Declares a value per participant.</summary>
        /// <param name="indexName">Name of the index in the snapshot.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="pulse">Pulse of the indicator that produces it, or null for a stream that does not follow the clock.</param>
        public void AddIndividual(string indexName, IProducer<Dictionary<uint, double>> stream, int? pulse = null)
            => this.AddInput(stream, pulse, Copy, value => this.state.Individual[indexName] = value, $"Individual-{indexName}");

        /// <summary>Declares a value per pair.</summary>
        /// <param name="indexName">Name of the index in the snapshot.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="pulse">Pulse of the indicator that produces it, or null for a stream that does not follow the clock.</param>
        public void AddPair(string indexName, IProducer<Dictionary<ParticipantPair, double>> stream, int? pulse = null)
            => this.AddInput(stream, pulse, Copy, value => this.state.Pair[indexName] = value, $"Pair-{indexName}");

        /// <summary>Declares a value per ordered pair.</summary>
        /// <param name="indexName">Name of the index in the snapshot.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="pulse">Pulse of the indicator that produces it, or null for a stream that does not follow the clock.</param>
        public void AddDirectedPair(string indexName, IProducer<Dictionary<DirectedParticipantPair, double>> stream, int? pulse = null)
            => this.AddInput(stream, pulse, Copy, value => this.state.DirectedPair[indexName] = value, $"DirectedPair-{indexName}");

        /// <summary>Declares a normalized index feeding the collaboration score.</summary>
        /// <param name="indexName">Name of the index, as used by the score dimensions.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="pulse">Pulse of the indicator that produces it, or null for a stream that does not follow the clock.</param>
        public void AddScoreInput(string indexName, IProducer<double> stream, int? pulse = null)
            => this.AddInput(stream, pulse, value => value, value => this.state.ScoreInputs[indexName] = value, $"Score-{indexName}");

        /// <summary>
        /// Declares a usability flag of a score input. Several flags may be declared for one
        /// index: it is usable when all of them are true.
        /// </summary>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="pulse">Pulse of the indicator that produces it, or null for a stream that does not follow the clock.</param>
        public void AddValidity(string indexName, IProducer<bool> stream, int? pulse = null)
        {
            if (!this.validityParts.TryGetValue(indexName, out var parts))
            {
                parts = new Dictionary<int, bool>();
                this.validityParts[indexName] = parts;
            }

            int part = parts.Count;
            parts[part] = true;

            this.AddInput(
                stream,
                pulse,
                value => value,
                value =>
                {
                    parts[part] = value;
                    this.state.Validity[indexName] = parts.Values.All(flag => flag);
                },
                $"Validity-{indexName}");
        }

        private static Dictionary<TKey, double> Copy<TKey>(Dictionary<TKey, double> values)
            => values == null ? new Dictionary<TKey, double>() : new Dictionary<TKey, double>(values);

        private void AddInput<T>(IProducer<T> stream, int? pulse, Func<T, T> copy, Action<T> apply, string label)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (pulse.HasValue && (pulse.Value < 0 || pulse.Value >= this.pulses.Count))
            {
                throw new ArgumentException($"Unknown pulse {pulse.Value}: declare it with AddPulse first.", nameof(pulse));
            }

            AlignedInput<T>? aligned = null;
            if (pulse.HasValue)
            {
                aligned = new AlignedInput<T>(pulse.Value, apply);
                this.alignedInputs.Add(aligned);
            }

            Receiver<T> receiver = this.pipeline.CreateReceiver<T>(
                this,
                (value, envelope) =>
                {
                    // Copy first: \psi recycles the payload once the handler returns.
                    T owned = copy(value);
                    if (aligned == null)
                    {
                        apply(owned);
                    }
                    else
                    {
                        aligned.Enqueue(envelope.OriginatingTime, owned);
                        this.Drain();
                    }
                },
                $"{this.name}-{label}-{this.inputCount++}");

            stream.PipeTo(receiver);
        }

        private void ReceiveTick(bool value, Envelope envelope)
        {
            if (envelope.OriginatingTime <= this.lastTick)
            {
                return;
            }

            this.lastTick = envelope.OriginatingTime;
            this.pendingTicks.Enqueue(envelope.OriginatingTime);
            this.Drain();
        }

        /// <summary>Emits every tick whose values are all known, oldest first.</summary>
        private void Drain()
        {
            while (this.pendingTicks.Count > 0)
            {
                DateTime tick = this.pendingTicks.Peek();

                if (!this.IsComplete(tick))
                {
                    bool overflow = this.configuration.MaximumPendingTicks > 0 && this.pendingTicks.Count > this.configuration.MaximumPendingTicks;
                    if (!overflow)
                    {
                        return;
                    }

                    this.ForcedSnapshotCount++;
                    if (this.configuration.LogWarnings && !this.warned)
                    {
                        this.warned = true;
                        Console.WriteLine(
                            $"[{this.name}] more than {this.configuration.MaximumPendingTicks} ticks are waiting for an index that was announced " +
                            "but never arrived. Snapshots are now emitted with the values available. An indicator probably declares " +
                            "an output that it does not post on every computation.");
                    }
                }

                this.Emit(tick);
            }
        }

        private bool IsComplete(DateTime tick)
        {
            for (int pulse = 0; pulse < this.pulses.Count; pulse++)
            {
                Queue<Marker> markers = this.pulses[pulse];
                while (markers.Count > 0 && markers.Peek().Time < tick)
                {
                    markers.Dequeue();
                }

                if (markers.Count == 0)
                {
                    // The indicator has not handled this tick yet.
                    return false;
                }

                // A marker of a later tick means that this one never reached the indicator.
                Marker head = markers.Peek();
                this.pulsePublished[pulse] = head.Time == tick && head.Published;
            }

            foreach (AlignedInput input in this.alignedInputs)
            {
                input.DropBefore(tick);
                if (input.IsEmpty && this.pulsePublished[input.Pulse])
                {
                    // Announced by the indicator, not delivered yet.
                    return false;
                }
            }

            return true;
        }

        private void Emit(DateTime tick)
        {
            foreach (AlignedInput input in this.alignedInputs)
            {
                input.DropBefore(tick);
                input.ApplyAt(tick);
            }

            foreach (Queue<Marker> markers in this.pulses)
            {
                while (markers.Count > 0 && markers.Peek().Time <= tick)
                {
                    markers.Dequeue();
                }
            }

            this.pendingTicks.Dequeue();

            IndexSnapshot snapshot = this.state.Clone();
            snapshot.OriginatingTime = tick;
            this.Out.Post(snapshot, tick);
        }

        private readonly struct Marker
        {
            public Marker(DateTime time, bool published)
            {
                this.Time = time;
                this.Published = published;
            }

            public DateTime Time { get; }

            public bool Published { get; }
        }

        /// <summary>Values of one stream waiting for their tick.</summary>
        private abstract class AlignedInput
        {
            protected AlignedInput(int pulse)
            {
                this.Pulse = pulse;
            }

            public int Pulse { get; }

            public abstract bool IsEmpty { get; }

            public abstract void DropBefore(DateTime tick);

            public abstract void ApplyAt(DateTime tick);
        }

        private sealed class AlignedInput<T> : AlignedInput
        {
            private readonly Queue<KeyValuePair<DateTime, T>> values = new Queue<KeyValuePair<DateTime, T>>();
            private readonly Action<T> apply;

            public AlignedInput(int pulse, Action<T> apply)
                : base(pulse)
            {
                this.apply = apply;
            }

            public override bool IsEmpty => this.values.Count == 0;

            public void Enqueue(DateTime time, T value) => this.values.Enqueue(new KeyValuePair<DateTime, T>(time, value));

            public override void DropBefore(DateTime tick)
            {
                while (this.values.Count > 0 && this.values.Peek().Key < tick)
                {
                    this.values.Dequeue();
                }
            }

            public override void ApplyAt(DateTime tick)
            {
                if (this.values.Count > 0 && this.values.Peek().Key == tick)
                {
                    this.apply(this.values.Dequeue().Value);
                }
            }
        }
    }

    /// <summary>
    /// Configuration of <see cref="IndexSnapshotAssembler"/>.
    /// </summary>
    public class IndexSnapshotAssemblerConfiguration
    {
        /// <summary>
        /// Gets or sets the number of waiting ticks beyond which a snapshot is emitted with the
        /// values available. It only protects against an indicator that announces a value and
        /// never posts it, which would otherwise silence the pipeline for good. Zero disables it.
        /// </summary>
        public int MaximumPendingTicks { get; set; } = 600;

        /// <summary>Gets or sets a value indicating whether the overflow is reported on the console.</summary>
        public bool LogWarnings { get; set; } = true;
    }
}
