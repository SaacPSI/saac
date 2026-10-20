using System;
using System.Collections.Generic;
using System.Numerics;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Resamples N participants on a common time grid, incrementally.
    ///
    /// Two properties make it usable in real time:
    ///  1. the grid is anchored on absolute time (index = ticks / interval), not on
    ///     "currentTime - window". A grid point therefore keeps the same index from one
    ///     computation to the next, and can be cached;
    ///  2. only the grid points that appeared since the previous computation are resampled.
    ///     At 5 Hz with a 50 ms grid, that is about 10 new points per computation instead of
    ///     the 100 points of the whole window.
    ///
    /// A grid point is only finalized once it is older than the settling delay, so that a
    /// sample arriving slightly late is still taken into account before the point is cached.
    /// </summary>
    public class IncrementalGridResampler
    {
        /// <summary>
        /// One resampled point: the positions of all the configured body parts of one
        /// participant at one grid index.
        /// </summary>
        public readonly struct GridSample
        {
            public long Index { get; }

            public Vector3[] Positions { get; }

            public GridSample(long index, Vector3[] positions)
            {
                this.Index = index;
                this.Positions = positions;
            }
        }

        private readonly MultiParticipantSlidingBuffer buffer;
        private readonly IReadOnlyList<uint> participantIds;
        private readonly IReadOnlyList<string> bodyParts;
        private readonly TimeSpan interval;
        private readonly TimeSpan maxDelta;
        private readonly Dictionary<uint, List<GridSample>> cache = new Dictionary<uint, List<GridSample>>();

        private long lastFinalizedIndex = long.MinValue;

        public IncrementalGridResampler(
            MultiParticipantSlidingBuffer buffer,
            IReadOnlyList<uint> participantIds,
            IReadOnlyList<string> bodyParts,
            TimeSpan interval,
            TimeSpan maxDelta)
        {
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentException("The sampling interval must be strictly positive.", nameof(interval));
            }

            this.buffer = buffer;
            this.participantIds = participantIds;
            this.bodyParts = bodyParts;
            this.interval = interval;
            this.maxDelta = maxDelta;

            foreach (uint participantId in participantIds)
            {
                this.cache[participantId] = new List<GridSample>();
            }
        }

        public TimeSpan Interval => this.interval;

        public long ToGridIndex(DateTime time) => time.Ticks / this.interval.Ticks;

        public DateTime ToTime(long gridIndex) => new DateTime(gridIndex * this.interval.Ticks);

        /// <summary>
        /// Resamples the new grid points and drops those that left the window.
        /// </summary>
        /// <param name="currentTime">Current originating time.</param>
        /// <param name="window">Duration of the sliding window.</param>
        /// <param name="settlingDelay">
        /// Grid points more recent than currentTime - settlingDelay are not finalized yet.
        /// Typically equal to the resampling tolerance (MaxDelta).
        /// </param>
        public void Update(DateTime currentTime, TimeSpan window, TimeSpan settlingDelay)
        {
            long firstIndex = this.ToGridIndex(currentTime - window);
            long lastIndex = this.ToGridIndex(currentTime - settlingDelay);

            long from = this.lastFinalizedIndex == long.MinValue ? firstIndex : Math.Max(firstIndex, this.lastFinalizedIndex + 1);

            for (long index = from; index <= lastIndex; index++)
            {
                DateTime time = this.ToTime(index);
                foreach (uint participantId in this.participantIds)
                {
                    var positions = new Vector3[this.bodyParts.Count];
                    bool complete = true;
                    for (int b = 0; b < this.bodyParts.Count; b++)
                    {
                        if (!this.buffer.TryFindClosest(participantId, this.bodyParts[b], time, this.maxDelta, out positions[b]))
                        {
                            complete = false;
                            break;
                        }
                    }

                    if (complete)
                    {
                        this.cache[participantId].Add(new GridSample(index, positions));
                    }
                }
            }

            if (lastIndex >= from)
            {
                this.lastFinalizedIndex = lastIndex;
            }

            this.PruneBefore(firstIndex);
        }

        /// <summary>
        /// Resampled points of one participant, in increasing grid index order.
        /// </summary>
        public IReadOnlyList<GridSample> GetSamples(uint participantId)
            => this.cache.TryGetValue(participantId, out var samples) ? samples : (IReadOnlyList<GridSample>)Array.Empty<GridSample>();

        /// <summary>
        /// Clears the cache, for instance after a pipeline restart or a long data gap.
        /// </summary>
        public void Reset()
        {
            foreach (var samples in this.cache.Values)
            {
                samples.Clear();
            }

            this.lastFinalizedIndex = long.MinValue;
        }

        private void PruneBefore(long firstIndex)
        {
            foreach (var samples in this.cache.Values)
            {
                int cut = 0;
                while (cut < samples.Count && samples[cut].Index < firstIndex)
                {
                    cut++;
                }

                // One point before the window is kept so that the first displacement
                // of the window remains computable.
                if (cut > 1)
                {
                    samples.RemoveRange(0, cut - 1);
                }
            }
        }
    }
}
