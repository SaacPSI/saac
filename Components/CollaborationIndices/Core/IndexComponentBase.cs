using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Directed pair (gazer -> gazed, current speaker -> previous speaker...).
    /// Unlike ParticipantPair, the order matters.
    /// </summary>
    public readonly struct DirectedParticipantPair : IEquatable<DirectedParticipantPair>
    {
        public uint From { get; }

        public uint To { get; }

        public DirectedParticipantPair(uint from, uint to)
        {
            this.From = from;
            this.To = to;
        }

        public ParticipantPair AsUndirected() => new ParticipantPair(this.From, this.To);

        public DirectedParticipantPair Reversed() => new DirectedParticipantPair(this.To, this.From);

        public bool Equals(DirectedParticipantPair other) => this.From == other.From && this.To == other.To;

        public override bool Equals(object obj) => obj is DirectedParticipantPair other && this.Equals(other);

        public override int GetHashCode() => (int)(((long)this.From * 397) ^ this.To);

        public override string ToString() => $"{this.From}->{this.To}";
    }

    /// <summary>
    /// Common configuration of every sliding window indicator component.
    /// A component always describes the last WindowDuration of data and publishes at most
    /// once per ComputationInterval.
    /// </summary>
    public class IndexComponentConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>Sliding window (the "threshold" of the legacy implementation).</summary>
        public TimeSpan WindowDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Minimum delay between two publications.</summary>
        public TimeSpan ComputationInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>Extra retention beyond the window, to absorb jitter and late data.</summary>
        public TimeSpan RetentionMargin { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>If true, a computation is triggered by incoming data as well as by TickIn.</summary>
        public bool ComputeOnDataReception { get; set; } = false;

        /// <summary>If false, the component publishes nothing (used by the phase gate).</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Normalizer applied to the published values. Identity by default.</summary>
        public IIndexNormalizer Normalizer { get; set; } = new IdentityNormalizer();

        public virtual int MinimumParticipantCount => 1;

        public virtual TimeSpan BufferRetention => this.WindowDuration + this.RetentionMargin;

        /// <summary>All the unordered pairs of the group.</summary>
        public IEnumerable<ParticipantPair> Pairs() => Combinatorics.Pairs(this.ParticipantIds);

        /// <summary>All the ordered pairs of the group.</summary>
        public IEnumerable<DirectedParticipantPair> DirectedPairs()
        {
            foreach (uint from in this.ParticipantIds)
            {
                foreach (uint to in this.ParticipantIds)
                {
                    if (from != to)
                    {
                        yield return new DirectedParticipantPair(from, to);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Set of double emitters indexed by an arbitrary key (participant, pair, directed pair).
    /// Removes the per-key emitter boilerplate that made the legacy class thousands of lines long.
    /// </summary>
    public class KeyedEmitters<TKey>
    {
        private readonly Dictionary<TKey, Emitter<double>> emitters = new Dictionary<TKey, Emitter<double>>();

        public KeyedEmitters(Pipeline pipeline, object owner, IEnumerable<TKey> keys, string prefix)
        {
            foreach (TKey key in keys)
            {
                if (!this.emitters.ContainsKey(key))
                {
                    this.emitters[key] = pipeline.CreateEmitter<double>(owner, $"{prefix}-{key}");
                }
            }
        }

        public Emitter<double> this[TKey key]
        {
            get
            {
                if (!this.emitters.TryGetValue(key, out var emitter))
                {
                    throw new ArgumentException($"No emitter declared for key {key}.", nameof(key));
                }

                return emitter;
            }
        }

        public bool Contains(TKey key) => this.emitters.ContainsKey(key);

        public IEnumerable<TKey> Keys => this.emitters.Keys;

        public void PostAll(IReadOnlyDictionary<TKey, double> values, DateTime originatingTime)
        {
            foreach (var entry in values)
            {
                if (this.emitters.TryGetValue(entry.Key, out var emitter))
                {
                    emitter.Post(entry.Value, originatingTime);
                }
            }
        }
    }

    /// <summary>
    /// Base class of every indicator component.
    /// It owns the clock input, the throttling of the computations and the pruning of the
    /// stores; derived classes only declare their inputs, their emitters and implement Compute().
    /// </summary>
    public abstract class IndexComponentBase<TConfiguration>
        where TConfiguration : IndexComponentConfiguration
    {
        private DateTime lastComputationTime = DateTime.MinValue;

        protected readonly Pipeline pipeline;
        protected readonly TConfiguration configuration;
        protected readonly string name;

        protected IndexComponentBase(Pipeline pipeline, TConfiguration configuration, string name)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (configuration.ParticipantIds == null || configuration.ParticipantIds.Count < configuration.MinimumParticipantCount)
            {
                throw new ArgumentException($"{name} needs at least {configuration.MinimumParticipantCount} participant(s).", nameof(configuration));
            }

            this.pipeline = pipeline;
            this.configuration = configuration;
            this.name = name;

            this.TickIn = pipeline.CreateReceiver<bool>(this, (_, envelope) => this.TryCompute(envelope.OriginatingTime), $"{name}-Tick");
            this.EnableIn = pipeline.CreateReceiver<bool>(this, (enabled, _) => this.configuration.Enabled = enabled, $"{name}-Enable");
        }

        /// <summary>Clock input: publishes the index at the rate of the connected generator.</summary>
        public Receiver<bool> TickIn { get; }

        /// <summary>Gating input, driven by the phase gate component.</summary>
        public Receiver<bool> EnableIn { get; }

        public TConfiguration Configuration => this.configuration;

        public IReadOnlyList<uint> ParticipantIds => this.configuration.ParticipantIds;

        /// <summary>Window start for the given current time.</summary>
        protected DateTime WindowStart(DateTime currentTime) => currentTime - this.configuration.WindowDuration;

        /// <summary>Window duration in seconds, used to turn counts into rates.</summary>
        protected double WindowSeconds => this.configuration.WindowDuration.TotalSeconds;

        protected abstract void Compute(DateTime originatingTime);

        /// <summary>Drops the data that left the window. Called before every computation.</summary>
        protected virtual void Prune(DateTime oldestAllowed)
        {
        }

        protected virtual bool CanCompute(DateTime originatingTime) => true;

        /// <summary>
        /// Called by the receivers of the derived classes when data arrives.
        /// </summary>
        protected void OnDataReceived(DateTime originatingTime)
        {
            if (this.configuration.ComputeOnDataReception)
            {
                this.TryCompute(originatingTime);
            }
        }

        protected void TryCompute(DateTime originatingTime)
        {
            this.Prune(originatingTime - this.configuration.BufferRetention);

            if (!this.configuration.Enabled)
            {
                return;
            }

            // \psi requires strictly increasing originating times on an emitter.
            if (originatingTime <= this.lastComputationTime)
            {
                return;
            }

            if (originatingTime - this.lastComputationTime < this.configuration.ComputationInterval)
            {
                return;
            }

            if (!this.CanCompute(originatingTime))
            {
                return;
            }

            this.lastComputationTime = originatingTime;
            this.Compute(originatingTime);
        }

        /// <summary>Applies the configured normalizer.</summary>
        protected double Normalize(double value) => this.configuration.Normalizer.Normalize(value);

        protected Dictionary<TKey, double> NormalizeAll<TKey>(Dictionary<TKey, double> values)
        {
            var result = new Dictionary<TKey, double>();
            foreach (var entry in values)
            {
                result[entry.Key] = this.Normalize(entry.Value);
            }

            return result;
        }

        public override string ToString() => this.name;
    }
}
