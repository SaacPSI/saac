// <copyright file="SpeechHistoryComponent.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;
using SAAC.PsiFormats;

namespace SAAC.CollaborationIndices
{
    public class SpeechHistoryConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>
        /// History kept per participant. TimeSpan.Zero keeps everything, which is the legacy
        /// behaviour and is only safe for short offline replays: see the class remarks.
        /// </summary>
        public TimeSpan HistoryDuration { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>Publish the Tuple/Queue and Tuple/List streams expected by the legacy components.</summary>
        public bool PublishLegacyCollections { get; set; } = true;

        /// <summary>Publish the whole window on every new verbalization, as the legacy version did.</summary>
        public bool PublishSnapshotOnEachEvent { get; set; } = true;
    }

    /// <summary>
    /// Keeps the recent verbalizations and the voice activity state of every participant.
    ///
    /// Replaces IndividualsVerbalization, which was instantiated once per participant while
    /// holding group sized structures inside (a list of seven booleans, seven dictionary
    /// entries, a triad only gaze dictionary). One instance now serves the whole group and the
    /// structures are sized from ParticipantIds.
    ///
    /// The important change is memory: the original enqueued every verbalization of the session
    /// into individualsVerbalizationQueue and re-posted the entire queue on every new one, so
    /// both the memory and the work per message grew linearly with the session, making the cost
    /// quadratic overall. Here the history is a sliding window (HistoryDuration) and every post
    /// is a fresh copy, so nothing downstream can observe a collection mutating under it.
    /// </summary>
    public class SpeechHistoryComponent
    {
        private readonly SpeechHistoryConfiguration configuration;
        private readonly MonotonicPoster poster = new MonotonicPoster();
        private readonly Dictionary<uint, List<SpeakingTimeIDData>> history = new Dictionary<uint, List<SpeakingTimeIDData>>();
        private readonly Dictionary<uint, bool> vadStates = new Dictionary<uint, bool>();
        private readonly Dictionary<uint, bool> speaking = new Dictionary<uint, bool>();

        private readonly ReceiverMap<uint, InteractionInterval> intervalReceivers;
        private readonly ReceiverMap<uint, SpeakingTimeIDData> legacyReceivers;
        private readonly ReceiverMap<uint, bool> vadReceivers;

        private readonly EmitterMap<uint, Tuple<int, Queue<SpeakingTimeIDData>>> legacyQueueEmitters;
        private readonly EmitterMap<uint, Tuple<int, List<SpeakingTimeIDData>>> legacyListEmitters;
        private readonly EmitterMap<uint, (int, bool)> vadWithIdEmitters;
        private readonly EmitterMap<uint, (int, DateTime)> vadOnsetEmitters;

        public SpeechHistoryComponent(Pipeline pipeline, SpeechHistoryConfiguration configuration, string name = nameof(SpeechHistoryComponent))
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.Name = name;

            foreach (uint participantId in configuration.ParticipantIds)
            {
                this.history[participantId] = new List<SpeakingTimeIDData>();
                this.vadStates[participantId] = false;
                this.speaking[participantId] = false;
            }

            this.intervalReceivers = new ReceiverMap<uint, InteractionInterval>(
                pipeline, this, configuration.ParticipantIds, this.ReceiveInterval, $"{name}-Interval");

            this.legacyReceivers = new ReceiverMap<uint, SpeakingTimeIDData>(
                pipeline, this, configuration.ParticipantIds, this.ReceiveLegacy, $"{name}-Legacy");

            this.vadReceivers = new ReceiverMap<uint, bool>(
                pipeline, this, configuration.ParticipantIds, this.ReceiveVad, $"{name}-Vad");

            this.legacyQueueEmitters = new EmitterMap<uint, Tuple<int, Queue<SpeakingTimeIDData>>>(
                pipeline, this, configuration.ParticipantIds, $"{name}-Queue");

            this.legacyListEmitters = new EmitterMap<uint, Tuple<int, List<SpeakingTimeIDData>>>(
                pipeline, this, configuration.ParticipantIds, $"{name}-List");

            this.vadWithIdEmitters = new EmitterMap<uint, (int, bool)>(
                pipeline, this, configuration.ParticipantIds, $"{name}-VadId");

            this.vadOnsetEmitters = new EmitterMap<uint, (int, DateTime)>(
                pipeline, this, configuration.ParticipantIds, $"{name}-VadOnset");

            this.Out = pipeline.CreateEmitter<IEnumerable<InteractionInterval>>(this, $"{name}-Intervals");
            this.VadStatesOut = pipeline.CreateEmitter<Dictionary<uint, bool>>(this, $"{name}-VadStates");
            this.SpeakerCountOut = pipeline.CreateEmitter<int>(this, $"{name}-SpeakerCount");
        }

        public string Name { get; }

        /// <summary>Every verbalization of the window, ready for VerbalParticipationComponent.IntervalsIn.</summary>
        public Emitter<IEnumerable<InteractionInterval>> Out { get; }

        /// <summary>Current voice activity of every participant.</summary>
        public Emitter<Dictionary<uint, bool>> VadStatesOut { get; }

        /// <summary>How many participants are speaking at once: 0 silence, 1 turn, 2+ overlap.</summary>
        public Emitter<int> SpeakerCountOut { get; }

        public Receiver<InteractionInterval> GetSpeakingInput(uint participantId) => this.intervalReceivers[participantId];

        public Receiver<SpeakingTimeIDData> GetLegacySpeakingInput(uint participantId) => this.legacyReceivers[participantId];

        public Receiver<bool> GetVadInput(uint participantId) => this.vadReceivers[participantId];

        /// <summary>Legacy queue stream, for FusionSpeechProcessing.CheckReceiver.</summary>
        public Emitter<Tuple<int, Queue<SpeakingTimeIDData>>> GetLegacyQueueEmitter(uint participantId) => this.legacyQueueEmitters[participantId];

        /// <summary>Legacy list stream, for TurnTakingsDetection.CheckSpeakingDataReceiver.</summary>
        public Emitter<Tuple<int, List<SpeakingTimeIDData>>> GetLegacyListEmitter(uint participantId) => this.legacyListEmitters[participantId];

        /// <summary>(id, state) stream, for the components expecting the identifier alongside the value.</summary>
        public Emitter<(int, bool)> GetVadWithIdEmitter(uint participantId) => this.vadWithIdEmitters[participantId];

        /// <summary>(id, time) emitted on each rising edge of the voice activity.</summary>
        public Emitter<(int, DateTime)> GetVadOnsetEmitter(uint participantId) => this.vadOnsetEmitters[participantId];

        private void ReceiveInterval(uint participantId, InteractionInterval interval, Envelope envelope)
        {
            if (interval == null)
            {
                return;
            }

            var legacy = new SpeakingTimeIDData(
                (int)participantId,
                interval.StartTime,
                interval.EndTime,
                (interval.EndTime - interval.StartTime).TotalSeconds,
                interval.Label ?? string.Empty,
                false);

            this.Store(participantId, legacy, envelope.OriginatingTime);
        }

        private void ReceiveLegacy(uint participantId, SpeakingTimeIDData data, Envelope envelope)
        {
            if (data != null)
            {
                this.Store(participantId, data, envelope.OriginatingTime);
            }
        }

        private void Store(uint participantId, SpeakingTimeIDData data, DateTime originatingTime)
        {
            if (!this.history.TryGetValue(participantId, out var participantHistory))
            {
                participantHistory = new List<SpeakingTimeIDData>();
                this.history[participantId] = participantHistory;
            }

            participantHistory.Add(data);
            this.Prune(originatingTime);

            if (!this.configuration.PublishSnapshotOnEachEvent)
            {
                return;
            }

            // Fresh copies: the legacy version posted the same List and Queue instances over and
            // over while continuing to mutate them, so a downstream component could observe a
            // collection changing between the moment it was posted and the moment it was read.
            if (this.configuration.PublishLegacyCollections)
            {
                this.poster.Post(
                    this.legacyListEmitters[participantId],
                    new Tuple<int, List<SpeakingTimeIDData>>((int)participantId, new List<SpeakingTimeIDData>(participantHistory)),
                    originatingTime);

                this.poster.Post(
                    this.legacyQueueEmitters[participantId],
                    new Tuple<int, Queue<SpeakingTimeIDData>>((int)participantId, new Queue<SpeakingTimeIDData>(participantHistory)),
                    originatingTime);
            }

            this.poster.Post(this.Out, this.SnapshotIntervals(), originatingTime);
        }

        private void ReceiveVad(uint participantId, bool isActive, Envelope envelope)
        {
            bool wasActive = this.vadStates.TryGetValue(participantId, out bool previous) && previous;
            this.vadStates[participantId] = isActive;

            if (isActive && !wasActive)
            {
                this.poster.Post(this.vadOnsetEmitters[participantId], ((int)participantId, envelope.OriginatingTime), envelope.OriginatingTime);
            }

            this.poster.Post(this.vadWithIdEmitters[participantId], ((int)participantId, isActive), envelope.OriginatingTime);

            // Group level emitters: written from every participant, hence guarded.
            this.poster.Post(this.VadStatesOut, new Dictionary<uint, bool>(this.vadStates), envelope.OriginatingTime);
            this.poster.Post(this.SpeakerCountOut, this.vadStates.Values.Count(active => active), envelope.OriginatingTime);
        }

        private IEnumerable<InteractionInterval> SnapshotIntervals()
        {
            var intervals = new List<InteractionInterval>();
            foreach (var entry in this.history)
            {
                foreach (SpeakingTimeIDData data in entry.Value)
                {
                    intervals.Add(new InteractionInterval(
                        data.StartOriginatingTime,
                        data.EndOriginatingTime,
                        IndexCategories.Speaking,
                        entry.Key,
                        null,
                        data.Text ?? string.Empty));
                }
            }

            return intervals;
        }

        private void Prune(DateTime currentTime)
        {
            if (this.configuration.HistoryDuration <= TimeSpan.Zero)
            {
                return;
            }

            DateTime oldest = currentTime - this.configuration.HistoryDuration;
            foreach (var entry in this.history)
            {
                entry.Value.RemoveAll(data => data.EndOriginatingTime < oldest);
            }
        }
    }
}
