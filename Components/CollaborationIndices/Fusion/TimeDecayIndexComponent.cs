using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class TimeDecayIndexConfiguration : IndexComponentConfiguration
    {
        /// <summary>
        /// Decay rate per second, per category. A recent event weighs 1, an event that
        /// happened dt seconds ago weighs exp(-lambda * dt).
        /// </summary>
        public Dictionary<string, double> DecayRates { get; set; } = new Dictionary<string, double>();

        public double DefaultDecayRate { get; set; } = 0.14;

        /// <summary>Categories aggregated by the component.</summary>
        public List<string> Categories { get; set; } = new List<string>();

        /// <summary>Normalizer per category, applied after the weighted sum.</summary>
        public Dictionary<string, IIndexNormalizer> CategoryNormalizers { get; set; } = new Dictionary<string, IIndexNormalizer>();

        public double DecayRateFor(string category)
            => this.DecayRates != null && this.DecayRates.TryGetValue(category, out double rate) ? rate : this.DefaultDecayRate;

        public IIndexNormalizer NormalizerFor(string category)
            => this.CategoryNormalizers != null && this.CategoryNormalizers.TryGetValue(category, out var normalizer) ? normalizer : this.Normalizer;
    }

    /// <summary>
    /// Recency weighted count: instead of counting every event of the window equally, an event
    /// contributes exp(-lambda * age). Two groups with the same number of events but different
    /// temporal distributions therefore get different scores, which a plain sliding count cannot
    /// distinguish. This is the legacy TimeDecayIndexCalculator / ComputeIndex, turned into a
    /// component with per category decay rates.
    ///
    /// Outputs:
    ///  - Out: transformed value per category;
    ///  - GetEmitter(category): dedicated stream.
    /// </summary>
    public class TimeDecayIndexComponent : MultiParticipantEventComponent<TimeDecayIndexConfiguration>,
                                           IProducer<Dictionary<string, double>>
    {
        private readonly KeyedEmitters<string> categoryEmitters;

        public TimeDecayIndexComponent(Pipeline pipeline, TimeDecayIndexConfiguration configuration, string name = nameof(TimeDecayIndexComponent))
            : base(pipeline, configuration, name)
        {
            this.Out = pipeline.CreateEmitter<Dictionary<string, double>>(this, $"{name}-Transformed");
            this.categoryEmitters = new KeyedEmitters<string>(pipeline, this, configuration.Categories, $"{name}-Transformed");
        }

        public Emitter<Dictionary<string, double>> Out { get; }

        public Emitter<double> GetEmitter(string category) => this.categoryEmitters[category];

        protected override void Compute(DateTime originatingTime)
        {
            DateTime start = this.WindowStart(originatingTime);
            var transformed = new Dictionary<string, double>();

            foreach (string category in this.configuration.Categories)
            {
                double lambda = this.configuration.DecayRateFor(category);
                double total = 0;

                foreach (uint participantId in this.configuration.ParticipantIds)
                {
                    foreach (InteractionEvent interactionEvent in this.events.Within(participantId, category, start, originatingTime))
                    {
                        double ageSeconds = (originatingTime - interactionEvent.OriginatingTime).TotalSeconds;
                        if (ageSeconds < 0)
                        {
                            continue;
                        }

                        total += interactionEvent.Intensity * Math.Exp(-lambda * ageSeconds);
                    }
                }

                transformed[category] = this.configuration.NormalizerFor(category).Normalize(total);
            }

            this.Out.Post(transformed, originatingTime);
            this.categoryEmitters.PostAll(transformed, originatingTime);
        }
    }
}
