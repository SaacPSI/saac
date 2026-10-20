// <copyright file="GroupVadComponent.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class GroupVadConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>
        /// Voice activity history kept per participant. The downstream turn taking detection
        /// only needs the recent past; keeping the whole session was what made the original
        /// grow without bound.
        /// </summary>
        public TimeSpan HistoryDuration { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>Publish the Tuple/SortedDictionary streams expected by TurnTakingsDetection.</summary>
        public bool PublishLegacyHistories { get; set; } = true;
    }

    /// <summary>
    /// Collects the voice activity of every participant of the group.
    ///
    /// Replaces IndividualVad, which declared seven receivers, fourteen emitters and fourteen
    /// near identical Process methods, plus four switch based lookups; the group size was
    /// therefore capped at seven and adding an eighth participant meant editing six places.
    /// Here the receivers and emitters are built from ParticipantIds.
    ///
    /// Two defects of the original are fixed:
    ///  1. SortedDictionary.Add(envelope.OriginatingTime, ...) throws ArgumentException when two
    ///     messages share an originating time, which happens as soon as two VAD sources are
    ///     joined or replayed from a store. The indexer is used instead, so a repeated timestamp
    ///     overwrites rather than crashing the pipeline;
    ///  2. the accumulated history was posted by reference on every message, so the consumer and
    ///     the producer shared a mutating dictionary, and the payload grew with the session.
    ///     A bounded copy is posted instead.
    /// </summary>
    public class GroupVadComponent
    {
        private readonly GroupVadConfiguration configuration;
        private readonly MonotonicPoster poster = new MonotonicPoster();
        private readonly Dictionary<uint, SortedDictionary<DateTime, bool>> vadHistory = new Dictionary<uint, SortedDictionary<DateTime, bool>>();
        private readonly Dictionary<uint, SortedDictionary<DateTime, DateTime>> onsetHistory = new Dictionary<uint, SortedDictionary<DateTime, DateTime>>();
        private readonly Dictionary<uint, bool> currentStates = new Dictionary<uint, bool>();

        private readonly ReceiverMap<uint, (int, bool)> vadReceivers;
        private readonly ReceiverMap<uint, (int, DateTime)> onsetReceivers;
        private readonly ReceiverMap<uint, bool> plainVadReceivers;

        private readonly EmitterMap<uint, Tuple<int, SortedDictionary<DateTime, bool>>> historyEmitters;
        private readonly EmitterMap<uint, Tuple<int, SortedDictionary<DateTime, DateTime>>> onsetEmitters;

        public GroupVadComponent(Pipeline pipeline, GroupVadConfiguration configuration, string name = nameof(GroupVadComponent))
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.Name = name;

            foreach (uint participantId in configuration.ParticipantIds)
            {
                this.vadHistory[participantId] = new SortedDictionary<DateTime, bool>();
                this.onsetHistory[participantId] = new SortedDictionary<DateTime, DateTime>();
                this.currentStates[participantId] = false;
            }

            this.vadReceivers = new ReceiverMap<uint, (int, bool)>(
                pipeline, this, configuration.ParticipantIds, (id, value, envelope) => this.StoreVad(id, value.Item2, envelope), $"{name}-VadId");

            this.plainVadReceivers = new ReceiverMap<uint, bool>(
                pipeline, this, configuration.ParticipantIds, this.StoreVad, $"{name}-Vad");

            this.onsetReceivers = new ReceiverMap<uint, (int, DateTime)>(
                pipeline, this, configuration.ParticipantIds, (id, value, envelope) => this.StoreOnset(id, value.Item2, envelope), $"{name}-OnsetId");

            this.historyEmitters = new EmitterMap<uint, Tuple<int, SortedDictionary<DateTime, bool>>>(
                pipeline, this, configuration.ParticipantIds, $"{name}-History");

            this.onsetEmitters = new EmitterMap<uint, Tuple<int, SortedDictionary<DateTime, DateTime>>>(
                pipeline, this, configuration.ParticipantIds, $"{name}-OnsetHistory");

            this.CurrentStatesOut = pipeline.CreateEmitter<Dictionary<uint, bool>>(this, $"{name}-CurrentStates");
            this.AllHistoriesOut = pipeline.CreateEmitter<Dictionary<uint, SortedDictionary<DateTime, bool>>>(this, $"{name}-AllHistories");
            this.AllOnsetsOut = pipeline.CreateEmitter<Dictionary<uint, SortedDictionary<DateTime, DateTime>>>(this, $"{name}-AllOnsets");
            this.SpeakerCountOut = pipeline.CreateEmitter<int>(this, $"{name}-SpeakerCount");
        }

        public string Name { get; }

        public Emitter<Dictionary<uint, bool>> CurrentStatesOut { get; }

        public Emitter<Dictionary<uint, SortedDictionary<DateTime, bool>>> AllHistoriesOut { get; }

        public Emitter<Dictionary<uint, SortedDictionary<DateTime, DateTime>>> AllOnsetsOut { get; }

        /// <summary>0 means silence, 1 a single turn, 2 or more an overlap.</summary>
        public Emitter<int> SpeakerCountOut { get; }

        /// <summary>Receiver of the (id, state) form, equivalent of CheckVADReceiver(i).</summary>
        public Receiver<(int, bool)> GetVadWithIdInput(uint participantId) => this.vadReceivers[participantId];

        /// <summary>Receiver of a plain boolean, when the source does not carry the identifier.</summary>
        public Receiver<bool> GetVadInput(uint participantId) => this.plainVadReceivers[participantId];

        /// <summary>Receiver of the (id, onset time) form, equivalent of CheckLastTrueVADReceiver(i).</summary>
        public Receiver<(int, DateTime)> GetOnsetInput(uint participantId) => this.onsetReceivers[participantId];

        /// <summary>Equivalent of CheckVADemitter(i), for TurnTakingsDetection.CheckVADReceiver.</summary>
        public Emitter<Tuple<int, SortedDictionary<DateTime, bool>>> GetHistoryEmitter(uint participantId) => this.historyEmitters[participantId];

        /// <summary>Equivalent of CheckTrueVADemitter(i), for TurnTakingsDetection.CheckTrueVADReceiver.</summary>
        public Emitter<Tuple<int, SortedDictionary<DateTime, DateTime>>> GetOnsetHistoryEmitter(uint participantId) => this.onsetEmitters[participantId];

        private void StoreVad(uint participantId, bool isActive, Envelope envelope)
        {
            if (!this.vadHistory.TryGetValue(participantId, out var history))
            {
                history = new SortedDictionary<DateTime, bool>();
                this.vadHistory[participantId] = history;
            }

            // Indexer, not Add: a repeated originating time must not throw.
            history[envelope.OriginatingTime] = isActive;
            this.currentStates[participantId] = isActive;
            Prune(history, envelope.OriginatingTime - this.configuration.HistoryDuration);

            if (this.configuration.PublishLegacyHistories)
            {
                this.poster.Post(
                    this.historyEmitters[participantId],
                    new Tuple<int, SortedDictionary<DateTime, bool>>((int)participantId, new SortedDictionary<DateTime, bool>(history)),
                    envelope.OriginatingTime);
            }

            // Group level emitters: written from every participant's receiver, hence guarded.
            this.poster.Post(this.CurrentStatesOut, new Dictionary<uint, bool>(this.currentStates), envelope.OriginatingTime);
            this.poster.Post(this.SpeakerCountOut, this.currentStates.Values.Count(active => active), envelope.OriginatingTime);
            this.poster.Post(this.AllHistoriesOut, this.CopyHistories(), envelope.OriginatingTime);
        }

        private void StoreOnset(uint participantId, DateTime onset, Envelope envelope)
        {
            if (!this.onsetHistory.TryGetValue(participantId, out var history))
            {
                history = new SortedDictionary<DateTime, DateTime>();
                this.onsetHistory[participantId] = history;
            }

            history[envelope.OriginatingTime] = onset;
            Prune(history, envelope.OriginatingTime - this.configuration.HistoryDuration);

            if (this.configuration.PublishLegacyHistories)
            {
                this.poster.Post(
                    this.onsetEmitters[participantId],
                    new Tuple<int, SortedDictionary<DateTime, DateTime>>((int)participantId, new SortedDictionary<DateTime, DateTime>(history)),
                    envelope.OriginatingTime);
            }

            this.poster.Post(this.AllOnsetsOut, this.CopyOnsets(), envelope.OriginatingTime);
        }

        private Dictionary<uint, SortedDictionary<DateTime, bool>> CopyHistories()
        {
            var copy = new Dictionary<uint, SortedDictionary<DateTime, bool>>();
            foreach (var entry in this.vadHistory)
            {
                copy[entry.Key] = new SortedDictionary<DateTime, bool>(entry.Value);
            }

            return copy;
        }

        private Dictionary<uint, SortedDictionary<DateTime, DateTime>> CopyOnsets()
        {
            var copy = new Dictionary<uint, SortedDictionary<DateTime, DateTime>>();
            foreach (var entry in this.onsetHistory)
            {
                copy[entry.Key] = new SortedDictionary<DateTime, DateTime>(entry.Value);
            }

            return copy;
        }

        private static void Prune<TValue>(SortedDictionary<DateTime, TValue> history, DateTime oldestAllowed)
        {
            var expired = history.Keys.TakeWhile(time => time < oldestAllowed).ToList();
            foreach (DateTime time in expired)
            {
                history.Remove(time);
            }
        }
    }
}
