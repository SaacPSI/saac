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
        /// it ends, or later: a short utterance said over a longer one of somebody else is
        /// received first and looks, alone, like a turn taking, and a recogniser delivers a
        /// long utterance after the short answer that followed it. Whether an interval takes
        /// the turn, and from whom, is therefore decided once the speech it depends on can no
        /// longer come: when every other participant has since been heard starting to speak,
        /// or after this delay. Zero decides at once, with the false turn takings of the
        /// utterances said over a longer one, and without the utterances received late.
        /// </summary>
        public TimeSpan ConfirmationDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>Speech kept to be compared with the speech that follows.</summary>
        public TimeSpan HistoryDuration { get; set; } = TimeSpan.FromSeconds(60);
    }

    /// <summary>
    /// Turn takings and overlaps, from the speech intervals of the participants.
    ///
    /// For an interval U of speaker s and an interval V of another speaker p:
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
    /// The intervals of a participant are received in order, each one when it ends or some
    /// time after. Those of two participants are not: the interval that ended first may be
    /// received last. The result does not depend on that order:
    ///
    ///  - an overlap is published when the second interval of the pair is received,
    ///    whichever of the two it is;
    ///  - whether U takes the turn is decided when U is confirmed (see ConfirmationDelay),
    ///    from all the speech received by then: a longer interval that contains U makes it an
    ///    overlap, and an interval that ended before U started, received after U, is whose
    ///    turn it was.
    ///
    /// An interval received after the ones it bears on were confirmed changes nothing to
    /// them: the delay has to cover the time the recogniser takes.
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

        /// <summary>Speech intervals of every participant, in the order they are received (VerbalizationDetectorComponent.Out).</summary>
        public Receiver<InteractionInterval> SpeechIn { get; }

        /// <summary>Any clock on the time of the data, for instance the clock of the indices.</summary>
        public Receiver<bool> TickIn { get; }

        /// <summary>Events of categories TurnTakingWithOverlap, TurnTakingWithoutOverlap and Overlap.</summary>
        public Emitter<InteractionEvent> Out { get; }

        /// <summary>
        /// An interval is said over another one that starts no later, ends no earlier and
        /// is longer: its speaker does not take the floor.
        /// </summary>
        private static bool SaidOver(InteractionInterval inner, InteractionInterval outer)
        {
            return outer.StartTime <= inner.StartTime
                && inner.EndTime <= outer.EndTime
                && (outer.StartTime < inner.StartTime || inner.EndTime < outer.EndTime);
        }

        private void ReceiveSpeech(InteractionInterval speech, Envelope envelope)
        {
            if (speech == null || speech.IsOpen || speech.EndTime <= speech.StartTime || !this.configuration.ParticipantIds.Contains(speech.ParticipantId))
            {
                return;
            }

            InteractionInterval current = speech.Clone();
            DateTime now = envelope.OriginatingTime;
            uint speaker = current.ParticipantId;

            // Each pair of intervals is compared once, when the second of the two is
            // received, whether it is the one said over the other or the one that contains it.
            var overlaps = new List<InteractionEvent>();
            foreach (InteractionInterval other in this.history)
            {
                if (other.ParticipantId == speaker || !this.Overlap(current, other))
                {
                    continue;
                }

                if (SaidOver(other, current))
                {
                    overlaps.Add(new InteractionEvent(other.StartTime, IndexCategories.Overlap, other.ParticipantId, speaker));
                }
                else if (SaidOver(current, other))
                {
                    overlaps.Add(new InteractionEvent(current.StartTime, IndexCategories.Overlap, speaker, other.ParticipantId));
                }
            }

            foreach (InteractionEvent detected in overlaps.OrderBy(e => e.OriginatingTime))
            {
                this.poster.Post(this.Out, detected, now);
            }

            // Whether the interval takes the turn is decided when it is confirmed. Whose turn
            // it was according to the speech received so far is kept with it: the history may
            // forget that interval before the confirmation.
            this.pending.Add(new Pending
            {
                Speech = current,
                Previous = this.PreviousSpeech(current, null),
                Deadline = current.EndTime + this.configuration.ConfirmationDelay,
            });

            if (!this.latestStart.TryGetValue(speaker, out DateTime latest) || current.StartTime > latest)
            {
                this.latestStart[speaker] = current.StartTime;
            }

            this.history.Add(current);
            this.PublishConfirmed(now);

            // Keep the last interval of each participant whatever its age: it is what tells
            // whose turn it was after a long silence. Keep too what may overlap an interval
            // that is still to be confirmed.
            DateTime oldest = now - this.configuration.HistoryDuration;
            var lastOfEach = this.history.GroupBy(h => h.ParticipantId).Select(g => g.OrderByDescending(h => h.EndTime).First()).ToList();
            this.history.RemoveAll(h => h.EndTime < oldest && !lastOfEach.Contains(h) && !this.pending.Any(p => h.EndTime > p.Speech.StartTime));
        }

        private bool Overlap(InteractionInterval a, InteractionInterval b)
        {
            DateTime sharedStart = a.StartTime > b.StartTime ? a.StartTime : b.StartTime;
            DateTime sharedEnd = a.EndTime < b.EndTime ? a.EndTime : b.EndTime;
            return sharedEnd - sharedStart > this.configuration.MinimumOverlap;
        }

        /// <summary>
        /// Whose turn it was when an interval starts: the last interval, of anybody, that
        /// ended before it starts, among the speech received so far and the one already known.
        /// </summary>
        private InteractionInterval PreviousSpeech(InteractionInterval current, InteractionInterval known)
        {
            DateTime limit = current.StartTime + this.configuration.MinimumOverlap;
            InteractionInterval previous = known;
            foreach (InteractionInterval other in this.history)
            {
                if (!ReferenceEquals(other, current) && other.EndTime <= limit && (previous == null || other.EndTime > previous.EndTime))
                {
                    previous = other;
                }
            }

            return previous;
        }

        /// <summary>
        /// The turn taking of an interval, from all the speech received so far; null when
        /// its speaker takes no turn.
        /// </summary>
        private InteractionEvent TurnTaking(Pending turn)
        {
            InteractionInterval current = turn.Speech;
            uint speaker = current.ParticipantId;
            InteractionInterval floorHolder = null;

            foreach (InteractionInterval other in this.history)
            {
                if (other.ParticipantId == speaker || !this.Overlap(current, other))
                {
                    continue;
                }

                if (SaidOver(current, other))
                {
                    // Said over somebody who goes on: no floor is taken.
                    return null;
                }

                // Several participants may still be speaking when the interval starts: the
                // floor is taken from the last of them to have taken it.
                if (other.StartTime < current.StartTime && (floorHolder == null || other.StartTime > floorHolder.StartTime))
                {
                    floorHolder = other;
                }
            }

            if (floorHolder != null)
            {
                return new InteractionEvent(current.StartTime, IndexCategories.TurnTakingWithOverlap, speaker, floorHolder.ParticipantId);
            }

            InteractionInterval previous = this.PreviousSpeech(current, turn.Previous);
            if (previous != null
                && previous.ParticipantId != speaker
                && (!this.configuration.MaximumSilence.HasValue || current.StartTime - previous.EndTime <= this.configuration.MaximumSilence.Value))
            {
                return new InteractionEvent(current.StartTime, IndexCategories.TurnTakingWithoutOverlap, speaker, previous.ParticipantId);
            }

            return null;
        }

        /// <summary>
        /// Decides the intervals that can no longer be contradicted, and publishes the turn
        /// takings among them: the delay has passed, or every other participant has started
        /// to speak since, so that an earlier interval of theirs would already have been
        /// received.
        /// </summary>
        private void PublishConfirmed(DateTime now)
        {
            if (this.pending.Count == 0)
            {
                return;
            }

            var taken = new List<InteractionEvent>();
            foreach (Pending turn in this.pending.Where(p => now >= p.Deadline || this.EveryOtherHasSpokenSince(p)).ToList())
            {
                this.pending.Remove(turn);
                InteractionEvent detected = this.TurnTaking(turn);
                if (detected != null)
                {
                    taken.Add(detected);
                }
            }

            foreach (InteractionEvent detected in taken.OrderBy(e => e.OriginatingTime))
            {
                this.poster.Post(this.Out, detected, now);
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
            public InteractionInterval Previous;
            public DateTime Deadline;
        }
    }
}
