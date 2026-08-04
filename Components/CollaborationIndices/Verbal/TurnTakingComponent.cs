using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class TurnTakingConfiguration : IndexComponentConfiguration
    {
        /// <summary>
        /// Categories published by the component. Each one gets its own set of outputs.
        /// A turn taking event carries the new speaker in ParticipantId and the previous
        /// speaker in TargetId, which is what makes the pair level possible.
        /// </summary>
        public List<string> Categories { get; set; } = new List<string>
        {
            IndexCategories.TurnTakingWithOverlap,
            IndexCategories.TurnTakingWithoutOverlap,
            IndexCategories.Overlap,
        };

        /// <summary>Normalizer per category. Falls back to Normalizer when the key is absent.</summary>
        public Dictionary<string, IIndexNormalizer> CategoryNormalizers { get; set; } = new Dictionary<string, IIndexNormalizer>();

        /// <summary>Normalizer of the pair level, per category.</summary>
        public Dictionary<string, IIndexNormalizer> PairNormalizers { get; set; } = new Dictionary<string, IIndexNormalizer>();

        /// <summary>Category of the silence intervals, published as a cumulated duration. Null to disable.</summary>
        public string SilenceCategory { get; set; } = IndexCategories.Silence;

        public override int MinimumParticipantCount => 2;

        public IIndexNormalizer NormalizerFor(string category)
            => this.CategoryNormalizers != null && this.CategoryNormalizers.TryGetValue(category, out var normalizer) ? normalizer : this.Normalizer;

        public IIndexNormalizer PairNormalizerFor(string category)
            => this.PairNormalizers != null && this.PairNormalizers.TryGetValue(category, out var normalizer) ? normalizer : this.NormalizerFor(category);
    }

    /// <summary>
    /// Turn taking indices: number of turn takings with overlap, without overlap, and number
    /// of overlaps, over the sliding window.
    ///
    /// The legacy version enumerated the three participants with a switch per case to
    /// dispatch the counts onto the pairs AB / AC / BC. Here a turn taking event carries
    /// (new speaker, previous speaker) and the dispatch on the pairs is generic.
    ///
    /// Outputs, per category:
    ///  - GetIndividualEmitter(category, participantId): normalized count per participant;
    ///  - GetPairEmitter(category, pair): normalized count per pair;
    ///  - GetGroupEmitter(category): normalized group total;
    ///  - AllOut: everything at once, keyed by category.
    /// </summary>
    public class TurnTakingComponent : MultiParticipantEventComponent<TurnTakingConfiguration>,
                                       IProducer<Dictionary<string, double>>
    {
        private readonly Dictionary<string, KeyedEmitters<uint>> individualEmitters = new Dictionary<string, KeyedEmitters<uint>>();
        private readonly Dictionary<string, KeyedEmitters<ParticipantPair>> pairEmitters = new Dictionary<string, KeyedEmitters<ParticipantPair>>();
        private readonly Dictionary<string, Emitter<double>> groupEmitters = new Dictionary<string, Emitter<double>>();

        public TurnTakingComponent(Pipeline pipeline, TurnTakingConfiguration configuration, string name = nameof(TurnTakingComponent))
            : base(pipeline, configuration, name)
        {
            this.Out = pipeline.CreateEmitter<Dictionary<string, double>>(this, $"{name}-GroupTotals");
            this.PairTotalsOut = pipeline.CreateEmitter<Dictionary<string, Dictionary<ParticipantPair, double>>>(this, $"{name}-PairTotals");
            this.SilenceOut = pipeline.CreateEmitter<double>(this, $"{name}-Silence");

            foreach (string category in configuration.Categories)
            {
                this.individualEmitters[category] = new KeyedEmitters<uint>(pipeline, this, configuration.ParticipantIds, $"{name}-{category}");
                this.pairEmitters[category] = new KeyedEmitters<ParticipantPair>(pipeline, this, configuration.Pairs(), $"{name}-{category}");
                this.groupEmitters[category] = pipeline.CreateEmitter<double>(this, $"{name}-{category}-Group");
            }
        }

        /// <summary>Normalized group total of every category.</summary>
        public Emitter<Dictionary<string, double>> Out { get; }

        /// <summary>Normalized pair values of every category.</summary>
        public Emitter<Dictionary<string, Dictionary<ParticipantPair, double>>> PairTotalsOut { get; }

        /// <summary>Cumulated silence duration over the window, in seconds.</summary>
        public Emitter<double> SilenceOut { get; }

        public Emitter<double> GetGroupEmitter(string category) => this.groupEmitters[category];

        public Emitter<double> GetIndividualEmitter(string category, uint participantId) => this.individualEmitters[category][participantId];

        public Emitter<double> GetPairEmitter(string category, ParticipantPair pair) => this.pairEmitters[category][pair];

        public Emitter<double> GetPairEmitter(string category, uint participantA, uint participantB)
            => this.GetPairEmitter(category, new ParticipantPair(participantA, participantB));

        protected override void Compute(DateTime originatingTime)
        {
            var groupTotals = new Dictionary<string, double>();
            var pairTotals = new Dictionary<string, Dictionary<ParticipantPair, double>>();

            foreach (string category in this.configuration.Categories)
            {
                Dictionary<uint, double> counts = this.CountsByParticipant(category, originatingTime);
                IIndexNormalizer normalizer = this.configuration.NormalizerFor(category);

                var normalizedCounts = new Dictionary<uint, double>();
                double total = 0;
                foreach (var entry in counts)
                {
                    total += entry.Value;
                    normalizedCounts[entry.Key] = normalizer.Normalize(entry.Value);
                }

                this.individualEmitters[category].PostAll(normalizedCounts, originatingTime);

                // Pair level: an event counts for the pair (new speaker, previous speaker).
                Dictionary<ParticipantPair, double> pairCounts = this.CountsByPair(category, originatingTime);
                IIndexNormalizer pairNormalizer = this.configuration.PairNormalizerFor(category);
                var normalizedPairs = new Dictionary<ParticipantPair, double>();
                foreach (var entry in pairCounts)
                {
                    normalizedPairs[entry.Key] = pairNormalizer.Normalize(entry.Value);
                }

                this.pairEmitters[category].PostAll(normalizedPairs, originatingTime);
                pairTotals[category] = normalizedPairs;

                double normalizedGroup = normalizer.Normalize(total);
                this.groupEmitters[category].Post(normalizedGroup, originatingTime);
                groupTotals[category] = normalizedGroup;
            }

            this.Out.Post(groupTotals, originatingTime);
            this.PairTotalsOut.Post(pairTotals, originatingTime);

            if (!string.IsNullOrEmpty(this.configuration.SilenceCategory))
            {
                double silence = 0;
                DateTime start = this.WindowStart(originatingTime);
                foreach (uint participantId in this.configuration.ParticipantIds)
                {
                    foreach (var silenceEvent in this.events.Within(participantId, this.configuration.SilenceCategory, start, originatingTime))
                    {
                        silence += silenceEvent.Intensity;
                    }
                }

                this.SilenceOut.Post(silence, originatingTime);
            }
        }
    }
}
