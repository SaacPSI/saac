using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class DominanceIdentityConfiguration : IndexComponentConfiguration
    {
        public DominanceIdentityConfiguration()
        {
            this.ComputationInterval = TimeSpan.Zero;
        }

        /// <summary>
        /// Minimum relative advantage required to declare a winner. 0 means that any strictly
        /// higher value wins, as in the legacy implementation.
        /// </summary>
        public double Tolerance { get; set; } = 1e-9;

        /// <summary>
        /// If true, the identity is published as id + 1 with 0 for a tie (legacy convention).
        /// If false, the raw identifier is published and -1 marks a tie.
        /// </summary>
        public bool UseLegacyEncoding { get; set; } = true;

        public override int MinimumParticipantCount => 2;
    }

    /// <summary>
    /// "Who does the most of X": talking most, tasking most, watched most, lead of visual
    /// attention. The legacy pipeline had one method per modality with a hard coded
    /// enumeration over three participants; this component takes any distribution and
    /// publishes the identity of the leader at the group level and inside each pair.
    ///
    /// Inputs:
    ///  - In: Dictionary&lt;uint, double&gt;.
    ///
    /// Outputs:
    ///  - Out: identity at the group level;
    ///  - PairOut: identity inside each pair;
    ///  - HasLeaderOut: false when the maximum is shared.
    /// </summary>
    public class DominanceIdentityComponent : IndexComponentBase<DominanceIdentityConfiguration>,
                                              IConsumer<Dictionary<uint, double>>,
                                              IProducer<double>
    {
        private Dictionary<uint, double> lastValues = new Dictionary<uint, double>();

        public DominanceIdentityComponent(Pipeline pipeline, DominanceIdentityConfiguration configuration, string name = nameof(DominanceIdentityComponent))
            : base(pipeline, configuration, name)
        {
            this.In = pipeline.CreateReceiver<Dictionary<uint, double>>(this, this.Receive, $"{name}-In");
            this.Out = pipeline.CreateEmitter<double>(this, $"{name}-Group");
            this.PairOut = pipeline.CreateEmitter<Dictionary<ParticipantPair, double>>(this, $"{name}-Pair");
            this.HasLeaderOut = pipeline.CreateEmitter<bool>(this, $"{name}-HasLeader");
            this.PairEmitters = new KeyedEmitters<ParticipantPair>(pipeline, this, configuration.Pairs(), $"{name}-Pair");
        }

        public Receiver<Dictionary<uint, double>> In { get; }

        public Emitter<double> Out { get; }

        public Emitter<Dictionary<ParticipantPair, double>> PairOut { get; }

        public Emitter<bool> HasLeaderOut { get; }

        public KeyedEmitters<ParticipantPair> PairEmitters { get; }

        protected override void Compute(DateTime originatingTime)
        {
            if (this.lastValues.Count == 0)
            {
                return;
            }

            uint? leader = GroupStatistics.ArgMax(this.lastValues, this.configuration.Tolerance);
            this.Out.Post(this.Encode(leader), originatingTime);
            this.HasLeaderOut.Post(leader.HasValue, originatingTime);

            var pairLeaders = new Dictionary<ParticipantPair, double>();
            foreach (ParticipantPair pair in this.configuration.Pairs())
            {
                uint? pairLeader = GroupStatistics.ArgMax(this.lastValues, new[] { pair.A, pair.B }, this.configuration.Tolerance);
                pairLeaders[pair] = this.Encode(pairLeader);
            }

            this.PairOut.Post(pairLeaders, originatingTime);
            this.PairEmitters.PostAll(pairLeaders, originatingTime);
        }

        private double Encode(uint? participantId)
        {
            if (this.configuration.UseLegacyEncoding)
            {
                return GroupStatistics.EncodeIdentity(participantId);
            }

            return participantId.HasValue ? participantId.Value : -1;
        }

        private void Receive(Dictionary<uint, double> values, Envelope envelope)
        {
            if (values == null)
            {
                return;
            }

            this.lastValues = values;
            this.TryCompute(envelope.OriginatingTime);
        }
    }
}
