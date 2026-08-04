using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class GazeOnPeersConfiguration : EventCountIndexConfiguration
    {
        public GazeOnPeersConfiguration()
        {
            this.Categories = new List<string> { IndexCategories.GazeOnPeer };
            this.Levels = IndexLevel.DirectedPair | IndexLevel.Group;
            this.GroupAggregation = GroupAggregation.Sum;
        }

        /// <summary>Normalizer of the "number of times watched" per participant.</summary>
        public IIndexNormalizer WatchedNormalizer { get; set; } = new IdentityNormalizer();

        public override int MinimumParticipantCount => 2;
    }

    /// <summary>
    /// Gaze on peers: how often each participant looks at each peer, over the sliding window.
    /// This is a directed index: 1 -&gt; 2 and 2 -&gt; 1 are two different values, which the
    /// legacy version handled with six hard coded emitters (gazeOnPeers12, 21, 13, ...).
    ///
    /// Additional outputs:
    ///  - WatchedOut: in-degree, i.e. how often each participant is watched by the others;
    ///  - MostWatchedOut: identity of the most watched participant (0 when tied);
    ///  - MostWatchedByPairOut: same restricted to each pair.
    /// </summary>
    public class GazeOnPeersComponent : EventCountIndexComponent<GazeOnPeersConfiguration>
    {
        public GazeOnPeersComponent(Pipeline pipeline, GazeOnPeersConfiguration configuration, string name = nameof(GazeOnPeersComponent))
            : base(pipeline, configuration, name)
        {
            this.WatchedOut = pipeline.CreateEmitter<Dictionary<uint, double>>(this, $"{name}-Watched");
            this.MostWatchedOut = pipeline.CreateEmitter<double>(this, $"{name}-MostWatched");
            this.MostWatchedByPairOut = pipeline.CreateEmitter<Dictionary<ParticipantPair, double>>(this, $"{name}-MostWatchedByPair");
            this.WatchedEmitters = new KeyedEmitters<uint>(pipeline, this, configuration.ParticipantIds, $"{name}-Watched");
        }

        /// <summary>How often each participant is looked at (sum over the gazers).</summary>
        public Emitter<Dictionary<uint, double>> WatchedOut { get; }

        /// <summary>Identity of the most watched participant, encoded as id + 1, or 0 on a tie.</summary>
        public Emitter<double> MostWatchedOut { get; }

        public Emitter<Dictionary<ParticipantPair, double>> MostWatchedByPairOut { get; }

        public KeyedEmitters<uint> WatchedEmitters { get; }

        protected override void OnComputed(Dictionary<uint, double> individualRaw, DateTime originatingTime)
        {
            DateTime start = this.WindowStart(originatingTime);

            // In-degree: how often each participant is the target of a gaze.
            var watched = new Dictionary<uint, double>();
            foreach (uint target in this.configuration.ParticipantIds)
            {
                double total = 0;
                foreach (uint gazer in this.configuration.ParticipantIds)
                {
                    if (gazer == target)
                    {
                        continue;
                    }

                    foreach (string category in this.configuration.Categories)
                    {
                        total += this.events.CountDirectedWithin(gazer, target, category, start, originatingTime);
                    }
                }

                watched[target] = total;
            }

            var normalizedWatched = new Dictionary<uint, double>();
            foreach (var entry in watched)
            {
                normalizedWatched[entry.Key] = this.configuration.WatchedNormalizer.Normalize(entry.Value);
            }

            this.WatchedOut.Post(normalizedWatched, originatingTime);
            this.WatchedEmitters.PostAll(normalizedWatched, originatingTime);

            this.MostWatchedOut.Post(GroupStatistics.EncodeIdentity(GroupStatistics.ArgMax(watched)), originatingTime);

            var mostWatchedByPair = new Dictionary<ParticipantPair, double>();
            foreach (ParticipantPair pair in this.configuration.Pairs())
            {
                uint? winner = GroupStatistics.ArgMax(watched, new[] { pair.A, pair.B });
                mostWatchedByPair[pair] = GroupStatistics.EncodeIdentity(winner);
            }

            this.MostWatchedByPairOut.Post(mostWatchedByPair, originatingTime);
        }
    }
}
