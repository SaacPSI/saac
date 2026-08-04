using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class JointVisualAttentionConfiguration : EventCountIndexConfiguration
    {
        public JointVisualAttentionConfiguration()
        {
            this.Categories = new List<string> { IndexCategories.JointVisualAttention };
            this.Levels = IndexLevel.UndirectedPair | IndexLevel.Group;
            this.GroupAggregation = GroupAggregation.Sum;
        }

        /// <summary>
        /// If true, the group value is the number of distinct JVA events rather than the sum
        /// of the per participant counts (an event involving two participants would otherwise
        /// be counted twice).
        /// </summary>
        public bool CountEventsOnceAtGroupLevel { get; set; } = true;

        public override int MinimumParticipantCount => 2;
    }

    /// <summary>
    /// Joint visual attention: episodes where a participant follows the visual attention of
    /// another one. An event carries the initiator in ParticipantId and the responder in TargetId.
    ///
    /// Additional outputs:
    ///  - InitiatorCountsOut: number of JVA initiated by each participant;
    ///  - LeadVisualAttentionOut: identity of the main initiator (0 when tied);
    ///  - LeadVisualAttentionByPairOut: same restricted to each pair.
    /// </summary>
    public class JointVisualAttentionComponent : EventCountIndexComponent<JointVisualAttentionConfiguration>
    {
        public JointVisualAttentionComponent(Pipeline pipeline, JointVisualAttentionConfiguration configuration, string name = nameof(JointVisualAttentionComponent))
            : base(pipeline, configuration, name)
        {
            this.InitiatorCountsOut = pipeline.CreateEmitter<Dictionary<uint, double>>(this, $"{name}-InitiatorCounts");
            this.LeadVisualAttentionOut = pipeline.CreateEmitter<double>(this, $"{name}-LeadVisualAttention");
            this.LeadVisualAttentionByPairOut = pipeline.CreateEmitter<Dictionary<ParticipantPair, double>>(this, $"{name}-LeadVisualAttentionByPair");
        }

        public Emitter<Dictionary<uint, double>> InitiatorCountsOut { get; }

        public Emitter<double> LeadVisualAttentionOut { get; }

        public Emitter<Dictionary<ParticipantPair, double>> LeadVisualAttentionByPairOut { get; }

        protected override void Compute(DateTime originatingTime)
        {
            base.Compute(originatingTime);

            if (this.configuration.CountEventsOnceAtGroupLevel && this.configuration.Levels.HasFlag(IndexLevel.Group))
            {
                double distinctEvents = 0;
                DateTime start = this.WindowStart(originatingTime);
                foreach (string category in this.configuration.Categories)
                {
                    foreach (uint participantId in this.configuration.ParticipantIds)
                    {
                        distinctEvents += this.events.CountWithin(participantId, category, start, originatingTime);
                    }
                }

                // Each event is stored once, under its initiator, so the sum is already the
                // number of episodes; the normalizer is applied as for the other levels.
                this.GroupOut.Post(this.configuration.GroupOrDefault.Normalize(distinctEvents), originatingTime);
            }
        }

        protected override void OnComputed(Dictionary<uint, double> individualRaw, DateTime originatingTime)
        {
            // individualRaw is already the number of episodes initiated by each participant.
            this.InitiatorCountsOut.Post(individualRaw, originatingTime);
            this.LeadVisualAttentionOut.Post(GroupStatistics.EncodeIdentity(GroupStatistics.ArgMax(individualRaw)), originatingTime);

            var leadByPair = new Dictionary<ParticipantPair, double>();
            foreach (ParticipantPair pair in this.configuration.Pairs())
            {
                uint? winner = GroupStatistics.ArgMax(individualRaw, new[] { pair.A, pair.B });
                leadByPair[pair] = GroupStatistics.EncodeIdentity(winner);
            }

            this.LeadVisualAttentionByPairOut.Post(leadByPair, originatingTime);
        }
    }
}
