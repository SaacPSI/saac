using System;
using System.Collections.Generic;
using System.Numerics;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Minimal timestamped position. Equivalent of the legacy BodyPartPosition type,
    /// kept internal to the indices library so that components do not depend on a
    /// particular body representation.
    /// </summary>
    public readonly struct TimestampedPosition
    {
        public DateTime Timestamp { get; }

        public Vector3 Position { get; }

        public TimestampedPosition(DateTime timestamp, Vector3 position)
        {
            this.Timestamp = timestamp;
            this.Position = position;
        }
    }

    /// <summary>
    /// Sliding track of the positions of one body part of one participant.
    ///
    /// Real time design: the cumulated travelled distance is maintained incrementally when
    /// each sample arrives (O(1) per message), so the distance over any window is obtained
    /// with two binary searches and one subtraction (O(log n)) instead of re-walking the
    /// whole window on every computation. The cost of an indicator therefore no longer
    /// grows with the window duration.
    /// </summary>
    public class PositionTrack
    {
        private const double RebaseThreshold = 1e6;

        private readonly List<TimestampedPosition> samples = new List<TimestampedPosition>();
        private readonly List<double> cumulatedDistance = new List<double>();

        public IReadOnlyList<TimestampedPosition> Samples => this.samples;

        public int Count => this.samples.Count;

        public DateTime LastTimestamp => this.samples.Count == 0 ? DateTime.MinValue : this.samples[this.samples.Count - 1].Timestamp;

        /// <summary>
        /// True when no sample was received recently: the sensor of this participant is
        /// probably lost. Lets a component publish a degraded status instead of a stale value.
        /// </summary>
        public bool IsStale(DateTime currentTime, TimeSpan maximumAge)
            => this.samples.Count == 0 || (currentTime - this.LastTimestamp) > maximumAge;

        /// <summary>
        /// Adds a sample. The nominal case (in order arrival) is O(1).
        /// Late samples are inserted at the right place and only the tail of the
        /// cumulated distances is recomputed.
        /// </summary>
        public void Add(DateTime timestamp, Vector3 position)
        {
            var sample = new TimestampedPosition(timestamp, position);

            if (this.samples.Count == 0)
            {
                this.samples.Add(sample);
                this.cumulatedDistance.Add(0);
                return;
            }

            int last = this.samples.Count - 1;
            if (this.samples[last].Timestamp <= timestamp)
            {
                double distance = Vector3.Distance(this.samples[last].Position, position);
                this.samples.Add(sample);
                this.cumulatedDistance.Add(this.cumulatedDistance[last] + distance);
                return;
            }

            int index = this.FirstIndexAtOrAfter(timestamp);
            this.samples.Insert(index, sample);
            this.cumulatedDistance.Insert(index, 0);
            this.RecomputeCumulatedFrom(index == 0 ? 0 : index);
        }

        /// <summary>
        /// Drops every sample strictly older than the given time. Bounded memory.
        /// </summary>
        public void Prune(DateTime oldestAllowed)
        {
            int cut = this.FirstIndexAtOrAfter(oldestAllowed);
            if (cut <= 0)
            {
                return;
            }

            // Keep one sample before the window so that the first displacement of the
            // window can still be computed.
            cut--;
            if (cut <= 0)
            {
                return;
            }

            this.samples.RemoveRange(0, cut);
            this.cumulatedDistance.RemoveRange(0, cut);

            if (this.cumulatedDistance.Count > 0 && this.cumulatedDistance[0] > RebaseThreshold)
            {
                double offset = this.cumulatedDistance[0];
                for (int i = 0; i < this.cumulatedDistance.Count; i++)
                {
                    this.cumulatedDistance[i] -= offset;
                }
            }
        }

        /// <summary>
        /// Distance travelled between the given start time and the most recent sample.
        /// </summary>
        /// <param name="start">Beginning of the window.</param>
        /// <param name="elapsedSeconds">Time actually covered by the samples of the window.</param>
        /// <param name="stepCount">Number of displacements used.</param>
        public double DistanceOverWindow(DateTime start, out double elapsedSeconds, out int stepCount)
        {
            elapsedSeconds = 0;
            stepCount = 0;

            int first = this.FirstIndexAtOrAfter(start);
            int last = this.samples.Count - 1;
            if (last - first < 1)
            {
                return 0;
            }

            elapsedSeconds = (this.samples[last].Timestamp - this.samples[first].Timestamp).TotalSeconds;
            stepCount = last - first;
            return this.cumulatedDistance[last] - this.cumulatedDistance[first];
        }

        /// <summary>
        /// Nearest sample of the requested time, within the given tolerance. O(log n).
        /// </summary>
        public bool TryFindClosest(DateTime targetTime, TimeSpan maxDelta, out Vector3 position)
        {
            position = default;
            if (this.samples.Count == 0)
            {
                return false;
            }

            int index = this.FirstIndexAtOrAfter(targetTime);
            double bestDelta = double.MaxValue;
            bool found = false;

            for (int candidate = index - 1; candidate <= index; candidate++)
            {
                if (candidate < 0 || candidate >= this.samples.Count)
                {
                    continue;
                }

                double delta = Math.Abs((this.samples[candidate].Timestamp - targetTime).TotalMilliseconds);
                if (delta < bestDelta && delta <= maxDelta.TotalMilliseconds)
                {
                    bestDelta = delta;
                    position = this.samples[candidate].Position;
                    found = true;
                }
            }

            return found;
        }

        public int FirstIndexAtOrAfter(DateTime time)
        {
            int low = 0;
            int high = this.samples.Count;
            while (low < high)
            {
                int mid = low + ((high - low) / 2);
                if (this.samples[mid].Timestamp < time)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            return low;
        }

        private void RecomputeCumulatedFrom(int start)
        {
            if (start == 0)
            {
                this.cumulatedDistance[0] = 0;
                start = 1;
            }

            for (int i = start; i < this.samples.Count; i++)
            {
                this.cumulatedDistance[i] = this.cumulatedDistance[i - 1] + Vector3.Distance(this.samples[i - 1].Position, this.samples[i].Position);
            }
        }
    }

    /// <summary>
    /// Sliding storage of the positions of N participants and M body parts.
    /// No locking is required: \psi serializes the delivery of all the receivers of a single
    /// component, so only one receiver at a time writes into the buffer.
    /// </summary>
    public class MultiParticipantSlidingBuffer
    {
        private static readonly PositionTrack EmptyTrack = new PositionTrack();

        private readonly Dictionary<uint, Dictionary<string, PositionTrack>> data
            = new Dictionary<uint, Dictionary<string, PositionTrack>>();

        public MultiParticipantSlidingBuffer(IEnumerable<uint> participantIds, IEnumerable<string> bodyParts)
        {
            foreach (uint participantId in participantIds)
            {
                var parts = new Dictionary<string, PositionTrack>();
                foreach (string bodyPart in bodyParts)
                {
                    parts[bodyPart] = new PositionTrack();
                }

                this.data[participantId] = parts;
            }
        }

        public void Add(uint participantId, string bodyPart, DateTime timestamp, Vector3 position)
            => this.GetOrCreateTrack(participantId, bodyPart).Add(timestamp, position);

        public PositionTrack GetTrack(uint participantId, string bodyPart)
        {
            if (this.data.TryGetValue(participantId, out var parts) && parts.TryGetValue(bodyPart, out var track))
            {
                return track;
            }

            return EmptyTrack;
        }

        public int SampleCount(uint participantId, string bodyPart) => this.GetTrack(participantId, bodyPart).Count;

        public bool TryFindClosest(uint participantId, string bodyPart, DateTime targetTime, TimeSpan maxDelta, out Vector3 position)
            => this.GetTrack(participantId, bodyPart).TryFindClosest(targetTime, maxDelta, out position);

        public void Prune(DateTime oldestAllowed)
        {
            foreach (var parts in this.data.Values)
            {
                foreach (var track in parts.Values)
                {
                    track.Prune(oldestAllowed);
                }
            }
        }

        private PositionTrack GetOrCreateTrack(uint participantId, string bodyPart)
        {
            if (!this.data.TryGetValue(participantId, out var parts))
            {
                parts = new Dictionary<string, PositionTrack>();
                this.data[participantId] = parts;
            }

            if (!parts.TryGetValue(bodyPart, out var track))
            {
                track = new PositionTrack();
                parts[bodyPart] = track;
            }

            return track;
        }
    }
}
