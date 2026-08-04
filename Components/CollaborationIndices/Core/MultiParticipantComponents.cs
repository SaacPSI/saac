using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Base class of the indicators built on point events (turn taking, JVA, task actions,
    /// gaze onsets, F-formations).
    ///
    /// Inputs:
    ///  - EventIn: one event, routed by its ParticipantId;
    ///  - EventsIn: a batch of events, which replaces the legacy Queue&lt;T&gt; inputs;
    ///  - GetEventInput(participantId): a dedicated receiver per participant, when the
    ///    upstream stream does not carry the identifier.
    /// The store rejects duplicates, so an upstream component may safely republish its
    /// whole queue at every tick as the legacy pipeline does.
    /// </summary>
    public abstract class MultiParticipantEventComponent<TConfiguration> : IndexComponentBase<TConfiguration>
        where TConfiguration : IndexComponentConfiguration
    {
        private readonly Dictionary<uint, Receiver<InteractionEvent>> participantReceivers = new Dictionary<uint, Receiver<InteractionEvent>>();

        protected readonly EventStore events = new EventStore();

        protected MultiParticipantEventComponent(Pipeline pipeline, TConfiguration configuration, string name)
            : base(pipeline, configuration, name)
        {
            this.EventIn = pipeline.CreateReceiver<InteractionEvent>(this, this.ReceiveEvent, $"{name}-EventIn");
            this.EventsIn = pipeline.CreateReceiver<IEnumerable<InteractionEvent>>(this, this.ReceiveEvents, $"{name}-EventsIn");

            foreach (uint participantId in configuration.ParticipantIds)
            {
                uint participant = participantId;
                this.participantReceivers[participant] = pipeline.CreateReceiver<InteractionEvent>(
                    this,
                    (interactionEvent, envelope) =>
                    {
                        if (interactionEvent != null)
                        {
                            interactionEvent.ParticipantId = participant;
                            this.events.Add(interactionEvent);
                            this.OnDataReceived(envelope.OriginatingTime);
                        }
                    },
                    $"{name}-EventIn-{participant}");
            }
        }

        public Receiver<InteractionEvent> EventIn { get; }

        public Receiver<IEnumerable<InteractionEvent>> EventsIn { get; }

        public Receiver<InteractionEvent> GetEventInput(uint participantId)
        {
            if (!this.participantReceivers.TryGetValue(participantId, out var receiver))
            {
                throw new ArgumentException($"Participant {participantId} is not declared in the configuration of {this.name}.", nameof(participantId));
            }

            return receiver;
        }

        protected override void Prune(DateTime oldestAllowed) => this.events.Prune(oldestAllowed);

        /// <summary>Count of one category, per participant, over the window.</summary>
        protected Dictionary<uint, double> CountsByParticipant(string category, DateTime currentTime)
        {
            DateTime start = this.WindowStart(currentTime);
            var counts = new Dictionary<uint, double>();
            foreach (uint participantId in this.configuration.ParticipantIds)
            {
                counts[participantId] = this.events.CountWithin(participantId, category, start, currentTime);
            }

            return counts;
        }

        /// <summary>Sum of several categories, per participant, over the window.</summary>
        protected Dictionary<uint, double> CountsByParticipant(IEnumerable<string> categories, DateTime currentTime)
        {
            DateTime start = this.WindowStart(currentTime);
            var counts = new Dictionary<uint, double>();
            foreach (uint participantId in this.configuration.ParticipantIds)
            {
                double total = 0;
                foreach (string category in categories)
                {
                    total += this.events.CountWithin(participantId, category, start, currentTime);
                }

                counts[participantId] = total;
            }

            return counts;
        }

        /// <summary>
        /// Count per unordered pair: events where the two participants are involved,
        /// whatever the direction.
        /// </summary>
        protected Dictionary<ParticipantPair, double> CountsByPair(string category, DateTime currentTime)
        {
            DateTime start = this.WindowStart(currentTime);
            var counts = new Dictionary<ParticipantPair, double>();
            foreach (ParticipantPair pair in this.configuration.Pairs())
            {
                counts[pair] = this.events.CountDirectedWithin(pair.A, pair.B, category, start, currentTime)
                             + this.events.CountDirectedWithin(pair.B, pair.A, category, start, currentTime);
            }

            return counts;
        }

        /// <summary>Count per ordered pair (gazer -> gazed).</summary>
        protected Dictionary<DirectedParticipantPair, double> CountsByDirectedPair(string category, DateTime currentTime)
        {
            DateTime start = this.WindowStart(currentTime);
            var counts = new Dictionary<DirectedParticipantPair, double>();
            foreach (DirectedParticipantPair pair in this.configuration.DirectedPairs())
            {
                counts[pair] = this.events.CountDirectedWithin(pair.From, pair.To, category, start, currentTime);
            }

            return counts;
        }

        private void ReceiveEvent(InteractionEvent interactionEvent, Envelope envelope)
        {
            this.events.Add(interactionEvent);
            this.OnDataReceived(envelope.OriginatingTime);
        }

        private void ReceiveEvents(IEnumerable<InteractionEvent> interactionEvents, Envelope envelope)
        {
            if (interactionEvents == null)
            {
                return;
            }

            foreach (var interactionEvent in interactionEvents)
            {
                this.events.Add(interactionEvent);
            }

            this.OnDataReceived(envelope.OriginatingTime);
        }
    }

    /// <summary>
    /// Base class of the indicators built on states held during an interval
    /// (speaking time, time in area, gaze duration, F-formation duration).
    /// The clipping of an interval on the window edges is done once, in
    /// InteractionInterval.DurationWithin, instead of being duplicated per indicator.
    /// </summary>
    public abstract class MultiParticipantIntervalComponent<TConfiguration> : IndexComponentBase<TConfiguration>
        where TConfiguration : IndexComponentConfiguration
    {
        private readonly Dictionary<uint, Receiver<InteractionInterval>> participantReceivers = new Dictionary<uint, Receiver<InteractionInterval>>();

        protected readonly IntervalStore intervals = new IntervalStore();

        protected MultiParticipantIntervalComponent(Pipeline pipeline, TConfiguration configuration, string name)
            : base(pipeline, configuration, name)
        {
            this.IntervalIn = pipeline.CreateReceiver<InteractionInterval>(this, this.ReceiveInterval, $"{name}-IntervalIn");
            this.IntervalsIn = pipeline.CreateReceiver<IEnumerable<InteractionInterval>>(this, this.ReceiveIntervals, $"{name}-IntervalsIn");

            foreach (uint participantId in configuration.ParticipantIds)
            {
                uint participant = participantId;
                this.participantReceivers[participant] = pipeline.CreateReceiver<InteractionInterval>(
                    this,
                    (interval, envelope) =>
                    {
                        if (interval != null)
                        {
                            interval.ParticipantId = participant;
                            this.intervals.Add(interval);
                            this.OnDataReceived(envelope.OriginatingTime);
                        }
                    },
                    $"{name}-IntervalIn-{participant}");
            }
        }

        public Receiver<InteractionInterval> IntervalIn { get; }

        public Receiver<IEnumerable<InteractionInterval>> IntervalsIn { get; }

        public Receiver<InteractionInterval> GetIntervalInput(uint participantId)
        {
            if (!this.participantReceivers.TryGetValue(participantId, out var receiver))
            {
                throw new ArgumentException($"Participant {participantId} is not declared in the configuration of {this.name}.", nameof(participantId));
            }

            return receiver;
        }

        protected override void Prune(DateTime oldestAllowed) => this.intervals.Prune(oldestAllowed);

        /// <summary>Cumulated duration of a state, per participant, over the window (seconds).</summary>
        protected Dictionary<uint, double> DurationsByParticipant(string category, DateTime currentTime)
        {
            DateTime start = this.WindowStart(currentTime);
            var durations = new Dictionary<uint, double>();
            foreach (uint participantId in this.configuration.ParticipantIds)
            {
                durations[participantId] = this.intervals.DurationWithin(participantId, category, start, currentTime);
            }

            return durations;
        }

        private void ReceiveInterval(InteractionInterval interval, Envelope envelope)
        {
            this.intervals.Add(interval);
            this.OnDataReceived(envelope.OriginatingTime);
        }

        private void ReceiveIntervals(IEnumerable<InteractionInterval> intervalList, Envelope envelope)
        {
            if (intervalList == null)
            {
                return;
            }

            foreach (var interval in intervalList)
            {
                this.intervals.Add(interval);
            }

            this.OnDataReceived(envelope.OriginatingTime);
        }
    }
}
