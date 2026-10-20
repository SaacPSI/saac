using System;
using System.Collections.Generic;
using System.Linq;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Behaviour while the sliding window is not full yet (session start, sensor reconnection).
    /// </summary>
    public enum WarmUpBehavior
    {
        /// <summary>Publish the index computed on the available data (real time friendly).</summary>
        PublishPartial,

        /// <summary>Publish nothing until enough data is available (analysis friendly).</summary>
        WaitForEnoughData,
    }

    /// <summary>
    /// Common configuration of every sliding window multi participant component.
    /// All the durations are TimeSpan; when your existing configuration stores milliseconds,
    /// use TimeSpan.FromMilliseconds(speechConfiguration.threshold).
    /// </summary>
    public class SlidingWindowConfiguration
    {
        /// <summary>
        /// Identifiers of the participants. The size of this list drives the number of
        /// inputs, outputs and sub-groups: nothing is hard coded to 2 or 3.
        /// </summary>
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>
        /// Body parts used by the indicator. One input per participant and per body part.
        /// </summary>
        public List<string> BodyParts { get; set; } = new List<string> { BodyPartNames.Head, BodyPartNames.LeftHand, BodyPartNames.RightHand };

        /// <summary>
        /// Duration of the sliding window: the index always describes the last
        /// WindowDuration of data (the "threshold" of the legacy implementation).
        /// </summary>
        public TimeSpan WindowDuration { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Minimum delay between two computations, i.e. the publication period of the index.
        /// This decouples the output rate from the sensor rate: the receivers stay O(1) and
        /// only this period drives the CPU cost. Set to TimeSpan.Zero to compute on every message.
        /// </summary>
        public TimeSpan ComputationInterval { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Extra data kept in the buffer beyond the window, to absorb jitter and late samples.
        /// Memory is bounded by WindowDuration + RetentionMargin.
        /// </summary>
        public TimeSpan RetentionMargin { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// If true, the computation is triggered by incoming data. If false, it is only
        /// triggered by messages posted on TickIn (clock driven mode, constant output rate).
        /// </summary>
        public bool ComputeOnDataReception { get; set; } = true;

        /// <summary>
        /// Minimum number of samples required in the window to publish a value.
        /// </summary>
        public int MinimumSampleCount { get; set; } = 2;

        public WarmUpBehavior WarmUp { get; set; } = WarmUpBehavior.PublishPartial;

        /// <summary>
        /// Beyond this delay without any sample, a participant is considered lost.
        /// Used to avoid publishing an index built on stale data.
        /// </summary>
        public TimeSpan MaximumSampleAge { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Minimum number of participants required by the indicator (1, 2, 3...).
        /// </summary>
        public virtual int MinimumParticipantCount => 1;

        /// <summary>
        /// Time span kept in the buffers.
        /// </summary>
        public virtual TimeSpan BufferRetention => this.WindowDuration + this.RetentionMargin;
    }

    /// <summary>
    /// Unit of the movement quantity.
    /// </summary>
    public enum MovementUnit
    {
        /// <summary>Mean displacement per sample, as in the legacy implementation. Depends on the frame rate.</summary>
        DisplacementPerSample,

        /// <summary>Mean displacement per second (speed). Recommended, frame rate independent.</summary>
        DisplacementPerSecond,
    }

    /// <summary>
    /// Configuration of <see cref="PhysicalActivityLevelComponent"/>.
    /// </summary>
    public class PhysicalActivityLevelConfiguration : SlidingWindowConfiguration
    {
        /// <summary>
        /// Weight of each body part in the activity level. Missing body parts get DefaultWeight.
        /// Legacy values: Head 0.4, LeftHand 0.3, RightHand 0.3.
        /// </summary>
        public Dictionary<string, double> BodyPartWeights { get; set; } = new Dictionary<string, double>
        {
            { BodyPartNames.Head, 0.4 },
            { BodyPartNames.LeftHand, 0.3 },
            { BodyPartNames.RightHand, 0.3 },
        };

        public double DefaultWeight { get; set; } = 1.0;

        /// <summary>
        /// If true, weights are divided by their sum, so the index stays comparable
        /// whatever the number of tracked body parts.
        /// </summary>
        public bool NormalizeWeights { get; set; } = true;

        public MovementUnit Unit { get; set; } = MovementUnit.DisplacementPerSecond;

        /// <summary>
        /// Additional windows computed in parallel of WindowDuration (the legacy 5 s window).
        /// Each one gets its own emitter. Thanks to the cumulated distances, an additional
        /// window costs two binary searches per participant and per body part.
        /// </summary>
        public List<TimeSpan> AdditionalWindows { get; set; } = new List<TimeSpan>();

        /// <summary>
        /// Aggregation used for the group level activity.
        /// </summary>
        public IScoreAggregator GroupAggregator { get; set; } = new MeanAggregator();

        public double GetWeight(string bodyPart)
            => this.BodyPartWeights != null && this.BodyPartWeights.TryGetValue(bodyPart, out double weight) ? weight : this.DefaultWeight;

        public override TimeSpan BufferRetention
        {
            get
            {
                TimeSpan longest = this.WindowDuration;
                if (this.AdditionalWindows != null)
                {
                    foreach (TimeSpan window in this.AdditionalWindows)
                    {
                        if (window > longest)
                        {
                            longest = window;
                        }
                    }
                }

                return longest + this.RetentionMargin;
            }
        }
    }

    /// <summary>
    /// Configuration of <see cref="PhysicalSynchronyComponent"/>.
    /// </summary>
    public class PhysicalSynchronyConfiguration : SlidingWindowConfiguration
    {
        public PhysicalSynchronyConfiguration()
        {
            // Synchrony is usually computed on the head only, hence different defaults.
            this.BodyParts = new List<string> { BodyPartNames.Head };
            this.WindowDuration = TimeSpan.FromSeconds(5);
            this.MinimumSampleCount = 10;
            this.ComputationInterval = TimeSpan.FromMilliseconds(500);
        }

        /// <summary>
        /// Step of the common time grid on which all the participants are resampled.
        /// </summary>
        public TimeSpan SamplingInterval { get; set; } = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// Maximum distance between a grid point and the sample used for it.
        /// </summary>
        public TimeSpan MaxDelta { get; set; } = TimeSpan.FromMilliseconds(30);

        /// <summary>
        /// A grid point is cached only once it is older than this delay, which leaves a
        /// chance to slightly late samples. Defaults to MaxDelta when null.
        /// This is the latency added by the resampling; the total latency of the index is
        /// roughly SettlingDelay + ComputationInterval.
        /// </summary>
        public TimeSpan? CacheSettlingDelay { get; set; } = null;

        public TimeSpan EffectiveSettlingDelay => this.CacheSettlingDelay ?? this.MaxDelta;

        /// <summary>
        /// Weight of each body part in the movement series when several are configured.
        /// </summary>
        public Dictionary<string, double> BodyPartWeights { get; set; } = new Dictionary<string, double>();

        public double DefaultWeight { get; set; } = 1.0;

        public bool NormalizeWeights { get; set; } = true;

        public MovementUnit Unit { get; set; } = MovementUnit.DisplacementPerSecond;

        /// <summary>
        /// If true, a grid point is used only when every participant has a valid sample on it,
        /// which guarantees perfectly aligned series (legacy behaviour).
        /// If false, each pair is correlated on its own common support, which keeps the dyadic
        /// scores alive when one participant is temporarily untracked.
        /// </summary>
        public bool RequireAllParticipants { get; set; } = true;

        public ISynchronyMeasure Measure { get; set; } = new PearsonCorrelationMeasure();

        public SynchronyNormalization Normalization { get; set; } = SynchronyNormalization.ZeroToOne;

        /// <summary>
        /// Aggregation of the pairwise scores into sub-group and group scores.
        /// </summary>
        public IScoreAggregator Aggregator { get; set; } = new MeanAggregator();

        /// <summary>
        /// If true, a score is also published for every sub-group of SubsetSize participants
        /// (SubsetSize = 3 gives the triadic score of the legacy implementation).
        /// Beware of the combinatorics in real time: C(n, k) sub-groups per computation.
        /// </summary>
        public bool ComputeSubsets { get; set; } = false;

        public int SubsetSize { get; set; } = 3;

        /// <summary>
        /// If true, a single score is published for the whole group.
        /// </summary>
        public bool ComputeGroupScore { get; set; } = true;

        /// <summary>
        /// Synchrony is a relational index: at least two participants are needed.
        /// </summary>
        public override int MinimumParticipantCount => 2;

        public double GetWeight(string bodyPart)
            => this.BodyPartWeights != null && this.BodyPartWeights.TryGetValue(bodyPart, out double weight) ? weight : this.DefaultWeight;

        public double TotalWeight() => this.BodyParts.Sum(this.GetWeight);
    }
}
