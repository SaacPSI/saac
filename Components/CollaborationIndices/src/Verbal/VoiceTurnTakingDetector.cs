// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.CollaborationIndices
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;

    public class VoiceTurnTakingDetectorConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>
        /// How long a participant must have been speaking for its speech to be an intervention.
        /// Shorter voice activity (a breath, a noise, a word) neither takes the floor nor holds
        /// it. It is also how long after its start an intervention is known.
        /// </summary>
        public TimeSpan MinimumSpeechDuration { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>The longest pause that does not end an intervention.</summary>
        public TimeSpan MaximumPause { get; set; } = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// Longest silence between two speakers for the second one to take the turn from the
        /// first. Null (default) sets no limit.
        /// </summary>
        public TimeSpan? MaximumSilence { get; set; } = null;

        /// <summary>
        /// Whether the transcription validates the interventions: an intervention no
        /// transcribed speech of its participant confirms within <see cref="TranscriptionDelay"/>
        /// after its end was not speech, and what was published for it is withdrawn on
        /// CancelledOut. Needs SpeechIn. False publishes on the voice activity alone.
        /// </summary>
        public bool ValidateWithTranscription { get; set; } = false;

        /// <summary>How long after the end of an intervention its transcription is awaited: the time a recogniser may take.</summary>
        public TimeSpan TranscriptionDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>A participant whose voice activity stopped arriving for this long no longer holds the others back.</summary>
        public TimeSpan VoiceLifetime { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>Interventions kept to tell whose turn it was.</summary>
        public TimeSpan HistoryDuration { get; set; } = TimeSpan.FromSeconds(60);
    }

    /// <summary>
    /// Turn takings and overlaps, found on the voice activity of the participants as it
    /// unfolds, and validated by the transcription.
    ///
    /// The voice activity says at once who is speaking; the transcription only gives an
    /// utterance when it is over. An intervention is the voice activity of a participant that
    /// lasts at least MinimumSpeechDuration, pauses shorter than MaximumPause included. The
    /// rules are those of <see cref="TurnTakingDetector"/>, applied to the interventions.
    /// For an intervention U of s:
    ///
    ///  - nobody else is in an intervention when U starts, and the last intervention before
    ///    was of another participant p .......... TurnTakingWithoutOverlap, s takes the turn
    ///                                             from p. Known when U is an intervention,
    ///                                             MinimumSpeechDuration after its start;
    ///  - p is in an intervention when U starts, and U outlasts it
    ///    ...................................... TurnTakingWithOverlap, s takes the turn from
    ///                                             p. Known when p stops. With several
    ///                                             participants still speaking, the turn is
    ///                                             taken from the last to have started;
    ///  - p is in an intervention when U starts, and U stops first
    ///    ...................................... Overlap, s over p. Known when U stops.
    ///
    /// The same participant resuming after a pause takes no turn. An event is stamped with
    /// the start of U, whenever it is published.
    ///
    /// With ValidateWithTranscription the events are published as above, and withdrawn on
    /// CancelledOut when no transcribed speech of the participant overlaps the intervention
    /// within TranscriptionDelay after its end: such voice activity was not speech, and it no
    /// longer counts as whose turn it was. The delay is counted on the time of the data: the
    /// pipeline is to run at the pace of the session.
    ///
    /// The voice activity streams are not synchronised with each other: a sample is handled
    /// once every participant has sent its own up to that time, so that what the others were
    /// doing is known. This relies on microphones that do not hear the other participants:
    /// a voice heard by two microphones is two interventions.
    /// </summary>
    public class VoiceTurnTakingDetector
    {
        private readonly VoiceTurnTakingDetectorConfiguration configuration;
        private readonly ReceiverMap<uint, bool> voiceReceivers;
        private readonly Dictionary<uint, Queue<(DateTime Time, bool Voice)>> queues = new Dictionary<uint, Queue<(DateTime Time, bool Voice)>>();
        private readonly Dictionary<uint, DateTime> lastReceived = new Dictionary<uint, DateTime>();
        private readonly Dictionary<uint, Intervention> current = new Dictionary<uint, Intervention>();
        private readonly List<Intervention> ended = new List<Intervention>();
        private readonly List<InteractionInterval> transcribed = new List<InteractionInterval>();
        private readonly MonotonicPoster poster = new MonotonicPoster(NonMonotonicPolicy.Advance);

        public VoiceTurnTakingDetector(Pipeline pipeline, VoiceTurnTakingDetectorConfiguration configuration, string name = nameof(VoiceTurnTakingDetector))
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.voiceReceivers = new ReceiverMap<uint, bool>(pipeline, this, configuration.ParticipantIds, this.ReceiveVoice, $"{name}-Voice");
            this.SpeechIn = pipeline.CreateReceiver<InteractionInterval>(this, this.ReceiveSpeech, $"{name}-Speech");
            this.Out = pipeline.CreateEmitter<InteractionEvent>(this, $"{name}-TurnTaking");
            this.CancelledOut = pipeline.CreateEmitter<InteractionEvent>(this, $"{name}-Cancelled");

            foreach (uint id in configuration.ParticipantIds)
            {
                this.queues[id] = new Queue<(DateTime Time, bool Voice)>();
                this.current[id] = null;
            }
        }

        /// <summary>Transcribed speech of every participant (VerbalizationDetectorComponent.Out), which validates the interventions.</summary>
        public Receiver<InteractionInterval> SpeechIn { get; }

        /// <summary>Events of categories TurnTakingWithOverlap, TurnTakingWithoutOverlap and Overlap, as soon as they are known.</summary>
        public Emitter<InteractionEvent> Out { get; }

        /// <summary>The events of Out that are withdrawn: their intervention was not confirmed by the transcription.</summary>
        public Emitter<InteractionEvent> CancelledOut { get; }

        /// <summary>The input of the voice activity of a participant: true while it speaks.</summary>
        /// <param name="participantId">The participant.</param>
        /// <returns>The receiver.</returns>
        public Receiver<bool> GetVoiceInput(uint participantId) => this.voiceReceivers[participantId];

        private void ReceiveVoice(uint participantId, bool voice, Envelope envelope)
        {
            this.queues[participantId].Enqueue((envelope.OriginatingTime, voice));
            this.lastReceived[participantId] = envelope.OriginatingTime;

            // What every participant has sent up to: a participant that stopped sending does not hold the others back.
            DateTime latest = this.lastReceived.Values.Max();
            DateTime upTo = this.configuration.ParticipantIds
                .Where(id => this.lastReceived.TryGetValue(id, out DateTime time) && latest - time <= this.configuration.VoiceLifetime)
                .Min(id => this.lastReceived[id]);

            // The samples of all the participants, in the order of their times.
            while (true)
            {
                uint? next = null;
                foreach (var queue in this.queues)
                {
                    if (queue.Value.Count > 0 && (next == null || queue.Value.Peek().Time < this.queues[next.Value].Peek().Time))
                    {
                        next = queue.Key;
                    }
                }

                if (next == null || this.queues[next.Value].Peek().Time > upTo)
                {
                    break;
                }

                (DateTime time, bool speaking) = this.queues[next.Value].Dequeue();
                this.Handle(next.Value, speaking, time);
            }
        }

        private void Handle(uint participantId, bool voice, DateTime time)
        {
            this.Expire(time);
            if (!voice)
            {
                return;
            }

            Intervention run = this.current[participantId];
            if (run == null)
            {
                run = new Intervention { Who = participantId, Start = time, LastVoice = time };
                this.current[participantId] = run;
            }
            else
            {
                run.LastVoice = time;
            }

            if (!run.Established && time - run.Start >= this.configuration.MinimumSpeechDuration)
            {
                this.Establish(run, time);
            }
        }

        /// <summary>
        /// What time alone decides: a voice that stopped for longer than a pause has ended, and
        /// an intervention the transcription did not confirm in time was not speech.
        /// </summary>
        private void Expire(DateTime time)
        {
            foreach (Intervention run in this.current.Values.Where(r => r != null && time - r.LastVoice > this.configuration.MaximumPause).OrderBy(r => r.LastVoice).ToList())
            {
                this.Close(run, time);
            }

            if (this.configuration.ValidateWithTranscription)
            {
                foreach (Intervention unconfirmed in this.ended.Where(i => !i.Validated && !i.Cancelled && time - i.LastVoice > this.configuration.TranscriptionDelay).ToList())
                {
                    unconfirmed.Cancelled = true;
                    foreach (InteractionEvent published in unconfirmed.Published)
                    {
                        this.poster.Post(this.CancelledOut, published, time);
                    }
                }
            }

            // The last intervention is kept whatever its age: it is whose turn it was after a long silence.
            DateTime oldest = time - this.configuration.HistoryDuration;
            Intervention last = this.ended.Where(i => !i.Cancelled).OrderByDescending(i => i.LastVoice).FirstOrDefault();
            this.ended.RemoveAll(i => i.LastVoice < oldest && !ReferenceEquals(i, last));
            this.transcribed.RemoveAll(t => t.EndTime < oldest);
        }

        /// <summary>
        /// The voice of a participant has lasted long enough to be an intervention: who was
        /// speaking when it started is known, and decides what it is or what it waits for.
        /// </summary>
        private void Establish(Intervention run, DateTime time)
        {
            run.Established = true;

            // The interventions of the others that were running when this one started, and are still.
            foreach (Intervention other in this.current.Values)
            {
                if (other != null && other.Who != run.Who && other.Established && other.Start <= run.Start)
                {
                    run.Over.Add(other);
                }
            }

            // Those that stopped since were running too: the floor may have been theirs.
            IEnumerable<Intervention> stopped = this.ended.Where(o => o.Who != run.Who && !o.Cancelled && o.Start <= run.Start && o.LastVoice > run.Start);
            run.FloorHolder = run.Over.Concat(stopped).Where(o => o.Start < run.Start).OrderByDescending(o => o.Start).FirstOrDefault();

            if (run.Over.Count == 0)
            {
                this.Decide(run, time);
            }
        }

        /// <summary>The intervention is said over nobody: it takes the turn, or goes on with its own.</summary>
        private void Decide(Intervention run, DateTime time)
        {
            run.Decided = true;
            if (run.FloorHolder != null)
            {
                this.Publish(run, new InteractionEvent(run.Start, IndexCategories.TurnTakingWithOverlap, run.Who, run.FloorHolder.Who), time);
                return;
            }

            Intervention previous = this.ended.Where(o => !o.Cancelled && o.LastVoice <= run.Start).OrderByDescending(o => o.LastVoice).FirstOrDefault();
            if (previous != null
                && previous.Who != run.Who
                && (!this.configuration.MaximumSilence.HasValue || run.Start - previous.LastVoice <= this.configuration.MaximumSilence.Value))
            {
                this.Publish(run, new InteractionEvent(run.Start, IndexCategories.TurnTakingWithoutOverlap, run.Who, previous.Who), time);
            }
        }

        /// <summary>The voice of a participant has stopped for longer than a pause.</summary>
        private void Close(Intervention run, DateTime time)
        {
            this.current[run.Who] = null;
            if (!run.Established)
            {
                // Too short to be an intervention.
                return;
            }

            if (!run.Decided)
            {
                // It stops while somebody who was speaking before it goes on: it was said over them.
                run.Decided = true;
                foreach (Intervention other in run.Over)
                {
                    this.Publish(run, new InteractionEvent(run.Start, IndexCategories.Overlap, run.Who, other.Who), time);
                }
            }

            // Those that were waiting for it to stop have outlasted it.
            foreach (Intervention waiting in this.current.Values.Where(w => w != null && w.Established && !w.Decided).ToList())
            {
                if (waiting.Over.Remove(run) && waiting.Over.Count == 0)
                {
                    this.Decide(waiting, time);
                }
            }

            run.Validated = this.transcribed.Any(t => t.ParticipantId == run.Who && t.StartTime < run.LastVoice && t.EndTime > run.Start);
            this.ended.Add(run);
        }

        private void Publish(Intervention run, InteractionEvent detected, DateTime time)
        {
            run.Published.Add(detected);
            this.poster.Post(this.Out, detected, time);
        }

        private void ReceiveSpeech(InteractionInterval speech, Envelope envelope)
        {
            if (speech == null || speech.IsOpen || speech.EndTime <= speech.StartTime || !this.configuration.ParticipantIds.Contains(speech.ParticipantId))
            {
                return;
            }

            // The transcription may come before the voice activity it confirms has been handled: it is kept.
            this.transcribed.Add(speech.Clone());
            foreach (Intervention run in this.ended)
            {
                if (run.Who == speech.ParticipantId && speech.StartTime < run.LastVoice && speech.EndTime > run.Start)
                {
                    run.Validated = true;
                }
            }
        }

        /// <summary>The voice activity of a participant, from its start to its last sample.</summary>
        private sealed class Intervention
        {
            public uint Who;
            public DateTime Start;
            public DateTime LastVoice;
            public bool Established;
            public bool Decided;
            public bool Validated;
            public bool Cancelled;
            public Intervention FloorHolder;

            public List<Intervention> Over { get; } = new List<Intervention>();

            public List<InteractionEvent> Published { get; } = new List<InteractionEvent>();
        }
    }
}
