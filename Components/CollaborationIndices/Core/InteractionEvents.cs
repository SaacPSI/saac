using System;
using System.Collections.Generic;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Point event of the collaborative activity: a turn taking, a JVA, a gaze onset,
    /// a piece grabbed, an F-formation end, etc.
    /// Every event based indicator consumes this single type, so a new modality only
    /// requires an adapter (see Adapters.cs) and not a new component.
    /// </summary>
    public class InteractionEvent
    {
        /// <summary>When the event happened.</summary>
        public DateTime OriginatingTime { get; set; }

        /// <summary>Family of the event, e.g. "Grab", "JVA", "TurnTakingWithOverlap".</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Participant who produced the event (initiator, gazer, actor).</summary>
        public uint ParticipantId { get; set; }

        /// <summary>Optional second participant (responder, gazed, previous speaker).</summary>
        public uint? TargetId { get; set; }

        /// <summary>Weight of the event, 1 by default.</summary>
        public double Intensity { get; set; } = 1.0;

        /// <summary>Optional free label (object id, area name...) for filtering.</summary>
        public string Label { get; set; } = string.Empty;

        public InteractionEvent() { }

        public InteractionEvent(DateTime originatingTime, string category, uint participantId, uint? targetId = null, double intensity = 1.0, string label = "")
        {
            this.OriginatingTime = originatingTime;
            this.Category = category;
            this.ParticipantId = participantId;
            this.TargetId = targetId;
            this.Intensity = intensity;
            this.Label = label ?? string.Empty;
        }

        /// <summary>
        /// Independent copy. Mandatory before storing a received message: \psi returns the
        /// payload of a reference type to its recycling pool once the handler returns, so a
        /// stored reference is silently overwritten by a later message.
        /// </summary>
        public InteractionEvent Clone() => new InteractionEvent(
            this.OriginatingTime, this.Category, this.ParticipantId, this.TargetId, this.Intensity, this.Label);

        public override string ToString()
            => $"{this.Category}@{this.OriginatingTime:HH:mm:ss.fff} {this.ParticipantId}" + (this.TargetId.HasValue ? $"->{this.TargetId}" : string.Empty);
    }

    /// <summary>
    /// State held during a time interval: speaking, being in an area, gazing at a peer,
    /// standing in an F-formation. EndTime = DateTime.MaxValue means still ongoing.
    /// </summary>
    public class InteractionInterval
    {
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; } = DateTime.MaxValue;

        public string Category { get; set; } = string.Empty;

        public uint ParticipantId { get; set; }

        public uint? TargetId { get; set; }

        public string Label { get; set; } = string.Empty;

        public bool IsOpen => this.EndTime == DateTime.MaxValue;

        public InteractionInterval() { }

        public InteractionInterval(DateTime startTime, DateTime endTime, string category, uint participantId, uint? targetId = null, string label = "")
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.Category = category;
            this.ParticipantId = participantId;
            this.TargetId = targetId;
            this.Label = label ?? string.Empty;
        }

        /// <summary>
        /// Independent copy. See InteractionEvent.Clone: a received message must never be
        /// stored by reference.
        /// </summary>
        public InteractionInterval Clone() => new InteractionInterval(
            this.StartTime, this.EndTime, this.Category, this.ParticipantId, this.TargetId, this.Label);

        /// <summary>
        /// Duration of the part of the interval that falls inside [windowStart, windowEnd].
        /// This single method replaces all the edge handling that used to be duplicated in
        /// the speaking time and the time in area computations.
        /// </summary>
        public double DurationWithin(DateTime windowStart, DateTime windowEnd)
        {
            DateTime start = this.StartTime > windowStart ? this.StartTime : windowStart;
            DateTime end = this.EndTime < windowEnd ? this.EndTime : windowEnd;
            double seconds = (end - start).TotalSeconds;
            return seconds > 0 ? seconds : 0;
        }
    }

    /// <summary>
    /// Sliding store of point events, indexed by participant and by category.
    /// Duplicates are rejected on (participant, category, time, target), which makes the
    /// store idempotent when an upstream component republishes its whole queue at every tick.
    /// </summary>
    public class EventStore
    {
        private static readonly List<InteractionEvent> Empty = new List<InteractionEvent>();

        private readonly Dictionary<uint, Dictionary<string, List<InteractionEvent>>> byParticipant
            = new Dictionary<uint, Dictionary<string, List<InteractionEvent>>>();

        private readonly HashSet<string> knownKeys = new HashSet<string>();

        public void Add(InteractionEvent interactionEvent)
        {
            if (interactionEvent == null)
            {
                return;
            }

            string key = $"{interactionEvent.ParticipantId}|{interactionEvent.Category}|{interactionEvent.OriginatingTime.Ticks}|{interactionEvent.TargetId}|{interactionEvent.Label}";
            if (!this.knownKeys.Add(key))
            {
                return;
            }

            interactionEvent = interactionEvent.Clone();

            if (!this.byParticipant.TryGetValue(interactionEvent.ParticipantId, out var categories))
            {
                categories = new Dictionary<string, List<InteractionEvent>>();
                this.byParticipant[interactionEvent.ParticipantId] = categories;
            }

            if (!categories.TryGetValue(interactionEvent.Category, out var events))
            {
                events = new List<InteractionEvent>();
                categories[interactionEvent.Category] = events;
            }

            if (events.Count == 0 || events[events.Count - 1].OriginatingTime <= interactionEvent.OriginatingTime)
            {
                events.Add(interactionEvent);
            }
            else
            {
                int index = events.FindLastIndex(e => e.OriginatingTime <= interactionEvent.OriginatingTime) + 1;
                events.Insert(index, interactionEvent);
            }
        }

        public void Prune(DateTime oldestAllowed)
        {
            foreach (var categories in this.byParticipant.Values)
            {
                foreach (var events in categories.Values)
                {
                    int cut = 0;
                    while (cut < events.Count && events[cut].OriginatingTime < oldestAllowed)
                    {
                        string key = $"{events[cut].ParticipantId}|{events[cut].Category}|{events[cut].OriginatingTime.Ticks}|{events[cut].TargetId}|{events[cut].Label}";
                        this.knownKeys.Remove(key);
                        cut++;
                    }

                    if (cut > 0)
                    {
                        events.RemoveRange(0, cut);
                    }
                }
            }
        }

        public IReadOnlyList<InteractionEvent> Get(uint participantId, string category)
        {
            if (this.byParticipant.TryGetValue(participantId, out var categories) && categories.TryGetValue(category, out var events))
            {
                return events;
            }

            return Empty;
        }

        /// <summary>Events of one participant and one category inside the window.</summary>
        public IEnumerable<InteractionEvent> Within(uint participantId, string category, DateTime windowStart, DateTime windowEnd)
        {
            foreach (var interactionEvent in this.Get(participantId, category))
            {
                if (interactionEvent.OriginatingTime >= windowStart && interactionEvent.OriginatingTime <= windowEnd)
                {
                    yield return interactionEvent;
                }
            }
        }

        public double CountWithin(uint participantId, string category, DateTime windowStart, DateTime windowEnd)
        {
            double count = 0;
            foreach (var interactionEvent in this.Within(participantId, category, windowStart, windowEnd))
            {
                count += interactionEvent.Intensity;
            }

            return count;
        }

        /// <summary>Events of one participant directed towards a given peer.</summary>
        public double CountDirectedWithin(uint participantId, uint targetId, string category, DateTime windowStart, DateTime windowEnd)
        {
            double count = 0;
            foreach (var interactionEvent in this.Within(participantId, category, windowStart, windowEnd))
            {
                if (interactionEvent.TargetId.HasValue && interactionEvent.TargetId.Value == targetId)
                {
                    count += interactionEvent.Intensity;
                }
            }

            return count;
        }

        public IEnumerable<InteractionEvent> AllWithin(IEnumerable<uint> participantIds, string category, DateTime windowStart, DateTime windowEnd)
        {
            foreach (uint participantId in participantIds)
            {
                foreach (var interactionEvent in this.Within(participantId, category, windowStart, windowEnd))
                {
                    yield return interactionEvent;
                }
            }
        }
    }

    /// <summary>
    /// Sliding store of intervals, indexed by participant and by category.
    /// </summary>
    public class IntervalStore
    {
        private static readonly List<InteractionInterval> Empty = new List<InteractionInterval>();

        private readonly Dictionary<uint, Dictionary<string, List<InteractionInterval>>> byParticipant
            = new Dictionary<uint, Dictionary<string, List<InteractionInterval>>>();

        public void Add(InteractionInterval interval)
        {
            if (interval == null)
            {
                return;
            }

            if (!this.byParticipant.TryGetValue(interval.ParticipantId, out var categories))
            {
                categories = new Dictionary<string, List<InteractionInterval>>();
                this.byParticipant[interval.ParticipantId] = categories;
            }

            if (!categories.TryGetValue(interval.Category, out var intervals))
            {
                intervals = new List<InteractionInterval>();
                categories[interval.Category] = intervals;
            }

            // Copy before storing: the caller's instance belongs to \psi's recycling pool and
            // will be overwritten by the next message otherwise.
            InteractionInterval stored = interval.Clone();

            // An interval is identified by its start; receiving it again (a queue republishing
            // its whole history, or an open interval later closed) replaces it in place.
            int existing = intervals.FindIndex(i => i.StartTime == stored.StartTime && i.TargetId == stored.TargetId);
            if (existing >= 0)
            {
                intervals[existing] = stored;
                return;
            }

            intervals.Add(stored);
        }

        /// <summary>
        /// Closes the currently open interval of a participant, if any.
        /// </summary>
        public void CloseOpen(uint participantId, string category, DateTime endTime)
        {
            foreach (var interval in this.Get(participantId, category))
            {
                if (interval.IsOpen)
                {
                    interval.EndTime = endTime;
                }
            }
        }

        public void Prune(DateTime oldestAllowed)
        {
            foreach (var categories in this.byParticipant.Values)
            {
                foreach (var intervals in categories.Values)
                {
                    intervals.RemoveAll(i => !i.IsOpen && i.EndTime < oldestAllowed);
                }
            }
        }

        public IReadOnlyList<InteractionInterval> Get(uint participantId, string category)
        {
            if (this.byParticipant.TryGetValue(participantId, out var categories) && categories.TryGetValue(category, out var intervals))
            {
                return intervals;
            }

            return Empty;
        }

        public IEnumerable<string> Categories(uint participantId)
        {
            if (this.byParticipant.TryGetValue(participantId, out var categories))
            {
                return categories.Keys;
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Cumulated duration, in seconds, of the state inside the window.
        /// </summary>
        public double DurationWithin(uint participantId, string category, DateTime windowStart, DateTime windowEnd)
        {
            double total = 0;
            foreach (var interval in this.Get(participantId, category))
            {
                total += interval.DurationWithin(windowStart, windowEnd);
            }

            return total;
        }

        public int CountWithin(uint participantId, string category, DateTime windowStart, DateTime windowEnd)
        {
            int count = 0;
            foreach (var interval in this.Get(participantId, category))
            {
                if (interval.EndTime >= windowStart && interval.StartTime <= windowEnd)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
