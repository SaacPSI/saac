using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class TurnTakingDetectorConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>
        /// Longest silence between two speakers for the second one to take the turn from the
        /// first. Null (default, as the legacy) sets no limit.
        /// </summary>
        public TimeSpan? MaximumSilence { get; set; } = null;

        /// <summary>
        /// Two speech intervals that share less than this are not considered as overlapping:
        /// the bounds of a transcribed utterance are not precise to the millisecond.
        /// </summary>
        public TimeSpan MinimumOverlap { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// How long a turn taking waits to be confirmed. A speech interval is received when
        /// it ends: a short utterance said over a longer one of somebody else is received
        /// first and looks, alone, like a turn taking. The turn taking is therefore held back
        /// until the longer interval can no longer come: when every other participant has
        /// since been heard starting to speak, or after this delay. Zero publishes at once,
        /// with those false turn takings.
        /// </summary>
        public TimeSpan ConfirmationDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>Speech kept to be compared with the speech that follows.</summary>
        public TimeSpan HistoryDuration { get; set; } = TimeSpan.FromSeconds(60);
    }

    /// <summary>
    /// Turn takings and overlaps, from the speech intervals of the participants.
    ///
    /// A speech interval arrives when it ends. It is then compared with the speech of the
    /// others received so far, each pair of intervals being compared once, when the one that
    /// ends last arrives. For the new interval U of speaker s and an interval V of another
    /// speaker p:
    ///
    ///  - V inside U ............................. Overlap, p over s, at the start of V;
    ///  - V running when U starts, U outlasts V .. TurnTakingWithOverlap, s takes the turn
    ///                                             from p, at the start of U. When several
    ///                                             participants are still speaking, the turn
    ///                                             is taken from the last to have started;
    ///  - nobody else speaking when U starts, and the last to have spoken before is another
    ///    participant p ......................... TurnTakingWithoutOverlap, s takes the turn
    ///                                             from p, at the start of U.
    ///
    /// A turn taking is only known for sure once no longer interval of somebody else can
    /// still arrive and turn out to contain U, which would make U an overlap: see
    /// ConfirmationDelay. An overlap is published as soon as the interval that contains it
    /// arrives.
    ///
    /// An event carries who takes the turn (or overlaps) in ParticipantId and whose turn it
    /// was in TargetId, which is what the TurnTaking indicator reads for its pair level. It is
    /// stamped with the moment it happened, whenever it is published.
    ///
    /// TickIn is optional: it lets the confirmations expire while nobody speaks. Without it
    /// they expire when the next speech interval arrives.
    ///
    /// Replaces the legacy TurnTakingsDetection, whose rules could not be reproduced: it
    /// compared the new interval with every past interval of the others each time, counted
    /// some configurations several times, and gave the first speaker of a session one turn
    /// taking per silent participant.
    /// </summary>
    public class TurnTakingDetector
    {
        private readonly TurnTakingDetectorConfiguration configuration;
        private readonly List<InteractionInterval> history = new List<InteractionInterval>();
        private readonly List<Pending> pending = new List<Pending>();
        private readonly Dictionary<uint, DateTime> latestStart = new Dictionary<uint, DateTime>();
        private readonly MonotonicPoster poster = new MonotonicPoster(NonMonotonicPolicy.Advance);

        public TurnTakingDetector(Pipeline pipeline, TurnTakingDetectorConfiguration configuration, string name = nameof(TurnTakingDetector))
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.SpeechIn = pipeline.CreateReceiver<InteractionInterval>(this, this.ReceiveSpeech, $"{name}-Speech");
            this.TickIn = pipeline.CreateReceiver<bool>(this, (_, envelope) => this.PublishConfirmed(envelope.OriginatingTime), $"{name}-Tick");
            this.Out = pipeline.CreateEmitter<InteractionEvent>(this, $"{name}-TurnTaking");
        }

        /// <summary>Speech intervals of every participant, each one received when it ends (VerbalizationDetectorComponent.Out).</summary>
        public Receiver<InteractionInterval> SpeechIn { get; }

        /// <summary>Any clock on the time of the data, for instance the clock of the indices.</summary>
        public Receiver<bool> TickIn { get; }

        /// <summary>Events of categories TurnTakingWithOverlap, TurnTakingWithoutOverlap and Overlap.</summary>
        public Emitter<InteractionEvent> Out { get; }

        private void ReceiveSpeech(InteractionInterval speech, Envelope envelope)
        {
            if (speech == null || speech.IsOpen || speech.EndTime <= speech.StartTime || !this.configuration.ParticipantIds.Contains(speech.ParticipantId))
            {
                return;
            }

            InteractionInterval current = speech.Clone();
            DateTime now = envelope.OriginatingTime;
            uint speaker = current.ParticipantId;
            var events = new List<InteractionEvent>();
            InteractionInterval floorHolder = null;
            bool insideAnother = false;

            foreach (InteractionInterval other in this.history)
            {
                if (other.ParticipantId == speaker || !this.Overlap(current, other))
                {
                    continue;
                }

                if (other.StartTime >= current.StartTime && other.EndTime <= current.EndTime)
                {
                    events.Add(new InteractionEvent(other.StartTime, IndexCategories.Overlap, other.ParticipantId, speaker));
                }
                else if (other.StartTime < current.StartTime && other.EndTime <= current.EndTime)
                {
                    // Several participants may still be speaking when the new interval starts:
                    // the floor is taken from the last of them to have taken it.
                    if (floorHolder == null || other.StartTime > floorHolder.StartTime)
                    {
                        floorHolder = other;
                    }
                }
                else if (other.StartTime < current.StartTime)
                {
                    // The other started before and ends after: the new interval is said over it.
                    insideAnother = true;
                    events.Add(new InteractionEvent(current.StartTime, IndexCategories.Overlap, speaker, other.ParticipantId));
                }
                else
                {
                    // The other started during the new interval and ends after it: it takes the turn.
                    events.Add(new InteractionEvent(other.StartTime, IndexCategories.TurnTakingWithOverlap, other.ParticipantId, speaker));
                }
            }

            // A turn taking held back was in fact said over this interval: it is not one.
            this.pending.RemoveAll(p => p.Speech.ParticipantId != speaker && current.StartTime < p.Speech.StartTime && this.Overlap(current, p.Speech));

            if (insideAnother)
            {
                // Said over somebody who goes on: no floor is taken.
            }
            else if (floorHolder != null)
            {
                this.pending.Add(new Pending
                {
                    Speech = current,
                    Event = new InteractionEvent(current.StartTime, IndexCategories.TurnTakingWithOverlap, speaker, floorHolder.ParticipantId),
                    Deadline = current.EndTime + this.configuration.ConfirmationDelay,
                });
            }
            else
            {
                // Whose turn it was: the last interval, of anybody, that ended before this one starts.
                InteractionInterval previous = this.history
                    .Where(h => h.EndTime <= current.StartTime + this.configuration.MinimumOverlap)
                    .OrderByDescending(h => h.EndTime)
                    .FirstOrDefault();
                if (previous != null
                    && previous.ParticipantId != speaker
                    && (!this.configuration.MaximumSilence.HasValue || current.StartTime - previous.EndTime <= this.configuration.MaximumSilence.Value))
                {
                    this.pending.Add(new Pending
                    {
                        Speech = current,
                        Event = new InteractionEvent(current.StartTime, IndexCategories.TurnTakingWithoutOverlap, speaker, previous.ParticipantId),
                        Deadline = current.EndTime + this.configuration.ConfirmationDelay,
                    });
                }
            }

            foreach (InteractionEvent detected in events.OrderBy(e => e.OriginatingTime))
            {
                this.poster.Post(this.Out, detected, now);
            }

            if (!this.latestStart.TryGetValue(speaker, out DateTime latest) || current.StartTime > latest)
            {
                this.latestStart[speaker] = current.StartTime;
            }

            this.PublishConfirmed(now);

            // Keep the last interval of each participant whatever its age: it is what tells
            // whose turn it was after a long silence.
            this.history.Add(current);
            DateTime oldest = now - this.configuration.HistoryDuration;
            var lastOfEach = this.history.GroupBy(h => h.ParticipantId).Select(g => g.OrderByDescending(h => h.EndTime).First()).ToList();
            this.history.RemoveAll(h => h.EndTime < oldest && !lastOfEach.Contains(h));
        }

        private bool Overlap(InteractionInterval a, InteractionInterval b)
        {
            DateTime sharedStart = a.StartTime > b.StartTime ? a.StartTime : b.StartTime;
            DateTime sharedEnd = a.EndTime < b.EndTime ? a.EndTime : b.EndTime;
            return sharedEnd - sharedStart > this.configuration.MinimumOverlap;
        }

        /// <summary>
        /// Publishes the turn takings without overlap that can no longer be contradicted: the
        /// delay has passed, or every other participant has started to speak since, so that
        /// an earlier interval of theirs would already have been received.
        /// </summary>
        private void PublishConfirmed(DateTime now)
        {
            if (this.pending.Count == 0)
            {
                return;
            }

            var confirmed = this.pending.Where(p => now >= p.Deadline || this.EveryOtherHasSpokenSince(p)).OrderBy(p => p.Event.OriginatingTime).ToList();
            foreach (Pending turn in confirmed)
            {
                this.pending.Remove(turn);
                this.poster.Post(this.Out, turn.Event, now);
            }
        }

        private bool EveryOtherHasSpokenSince(Pending turn)
        {
            foreach (uint participantId in this.configuration.ParticipantIds)
            {
                if (participantId == turn.Speech.ParticipantId)
                {
                    continue;
                }

                if (!this.latestStart.TryGetValue(participantId, out DateTime start) || start <= turn.Speech.StartTime)
                {
                    return false;
                }
            }

            return true;
        }

        private sealed class Pending
        {
            public InteractionInterval Speech = new InteractionInterval();
            public InteractionEvent Event = new InteractionEvent();
            public DateTime Deadline;
        }
    }
}
