using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class AttentionLevelConfiguration : IndexComponentConfiguration
    {
        public AttentionLevelConfiguration()
        {
            // The accumulator is driven by its own fast clock, not by the index window.
            this.ComputationInterval = TimeSpan.Zero;
            this.ComputeOnDataReception = true;
        }

        /// <summary>Increment applied per tick while the participant is attentive.</summary>
        public TimeSpan Step { get; set; } = TimeSpan.FromMilliseconds(66.66);

        /// <summary>Upper bound of the accumulator (legacy: 1932 ms).</summary>
        public TimeSpan Ceiling { get; set; } = TimeSpan.FromMilliseconds(1932);

        /// <summary>Lower bound of the accumulator.</summary>
        public TimeSpan Floor { get; set; } = TimeSpan.Zero;

        /// <summary>If true, the value is published in [0, 1] instead of milliseconds.</summary>
        public bool PublishAsRatio { get; set; } = false;
    }

    /// <summary>
    /// Attention level: a leaky accumulator that grows while a participant is looking at the
    /// shared focus and decreases otherwise. It is the only indicator of the framework that is
    /// not a sliding window statistic: it is a state, updated at the rate of its clock.
    ///
    /// Inputs:
    ///  - GetAttentiveInput(participantId): the boolean state of one participant;
    ///  - TickIn: the clock that paces the accumulation (legacy: the 50 ms timer).
    /// </summary>
    public class AttentionLevelComponent : IndexComponentBase<AttentionLevelConfiguration>,
                                           IProducer<Dictionary<uint, double>>
    {
        private readonly Dictionary<uint, Receiver<bool>> attentiveReceivers = new Dictionary<uint, Receiver<bool>>();
        private readonly Dictionary<uint, bool> attentive = new Dictionary<uint, bool>();
        private readonly Dictionary<uint, double> level = new Dictionary<uint, double>();

        public AttentionLevelComponent(Pipeline pipeline, AttentionLevelConfiguration configuration, string name = nameof(AttentionLevelComponent))
            : base(pipeline, configuration, name)
        {
            this.Out = pipeline.CreateEmitter<Dictionary<uint, double>>(this, $"{name}-Levels");
            this.ParticipantEmitters = new KeyedEmitters<uint>(pipeline, this, configuration.ParticipantIds, $"{name}-Level");

            foreach (uint participantId in configuration.ParticipantIds)
            {
                uint participant = participantId;
                this.attentive[participant] = false;
                this.level[participant] = 0;
                this.attentiveReceivers[participant] = pipeline.CreateReceiver<bool>(
                    this,
                    (isAttentive, _) => this.attentive[participant] = isAttentive,
                    $"{name}-Attentive-{participant}");
            }
        }

        public Emitter<Dictionary<uint, double>> Out { get; }

        public KeyedEmitters<uint> ParticipantEmitters { get; }

        public Receiver<bool> GetAttentiveInput(uint participantId)
        {
            if (!this.attentiveReceivers.TryGetValue(participantId, out var receiver))
            {
                throw new ArgumentException($"Participant {participantId} is not declared in the configuration of {this.name}.", nameof(participantId));
            }

            return receiver;
        }

        protected override void Compute(DateTime originatingTime)
        {
            double step = this.configuration.Step.TotalMilliseconds;
            double ceiling = this.configuration.Ceiling.TotalMilliseconds;
            double floor = this.configuration.Floor.TotalMilliseconds;

            var published = new Dictionary<uint, double>();
            foreach (uint participantId in this.configuration.ParticipantIds)
            {
                double value = this.level[participantId];
                value += this.attentive[participantId] ? step : -step;

                if (value > ceiling)
                {
                    value = ceiling;
                }

                if (value < floor)
                {
                    value = floor;
                }

                this.level[participantId] = value;
                published[participantId] = this.configuration.PublishAsRatio && ceiling > double.Epsilon ? value / ceiling : value;
            }

            this.Out.Post(published, originatingTime);
            this.ParticipantEmitters.PostAll(published, originatingTime);
        }
    }
}
