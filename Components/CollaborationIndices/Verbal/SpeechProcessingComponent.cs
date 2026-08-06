// <copyright file="SpeechProcessingComponent.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using SAAC.PsiFormats;

namespace SAAC.CollaborationIndices.Verbal
{
    [Serializable]
    public class SpeakerEnergyProfile
    {
        private readonly List<double> history = new List<double>();
        private readonly int maxSamples;
        private readonly int minimumSamples;
        private int start;

        public SpeakerEnergyProfile(int maxSamples = 300, int minimumSamples = 20)
        {
            if (maxSamples < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSamples));
            }

            this.maxSamples = maxSamples;
            this.minimumSamples = minimumSamples;
        }

        public int Count => this.history.Count - this.start;

        public bool IsReady => this.Count >= this.minimumSamples;

        public void AddSample(double logEnergy)
        {
            this.history.Add(logEnergy);

            // Amortised removal from the front: compact only when half the list is dead.
            if (this.Count > this.maxSamples)
            {
                this.start++;
                if (this.start > this.history.Count / 2)
                {
                    this.history.RemoveRange(0, this.start);
                    this.start = 0;
                }
            }
        }

        /// <summary>
        /// Percentile of the retained samples. <paramref name="percentile"/> is in [0, 1].
        ///
        /// Note for whoever ported this: the previous version was named Percentile90 but used
        /// 0.8, i.e. it returned the 80th percentile. Pass 0.8 explicitly if you need to
        /// reproduce the old numbers.
        /// </summary>
        public double Percentile(double percentile)
        {
            int count = this.Count;
            if (count == 0)
            {
                return 0;
            }

            var sorted = new double[count];
            this.history.CopyTo(this.start, sorted, 0, count);
            Array.Sort(sorted);

            int index = (int)(Math.Max(0.0, Math.Min(1.0, percentile)) * (count - 1));
            return sorted[index];
        }

        public void Clear()
        {
            this.history.Clear();
            this.start = 0;
        }
    }

    /// <summary>
    /// Everything the component keeps about one participant.
    /// </summary>
    public class ParticipantSpeechState
    {
        public ParticipantSpeechState(int id, SpeechProcessingConfiguration configuration)
        {
            this.Id = id;
            this.Profile = new SpeakerEnergyProfile(configuration.EnergyProfileSampleCount, configuration.MinimumProfileSamples);
        }

        public int Id { get; }

        public SpeakerEnergyProfile Profile { get; }

        /// <summary>Log energy over time, pruned to EnergyLogRetention.</summary>
        public SortedList<DateTime, double> EnergyLog { get; } = new SortedList<DateTime, double>();

        public (DateTime Time, bool Value) LastVad { get; set; } = (DateTime.MinValue, true);

        public (DateTime Time, bool Value) CurrentVad { get; set; } = (DateTime.MinValue, false);

        public AudioBuffer AudioBuffer { get; set; }

        public bool HasAudio { get; set; }

        public Queue<SpeakingTimeIDData> SpeakingQueue { get; set; } = new Queue<SpeakingTimeIDData>();

        public List<SpeakingTimeIDData> SpeakingList { get; set; } = new List<SpeakingTimeIDData>();
    }

    public class SpeechProcessingConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>Number of energy samples retained per speaker profile.</summary>
        public int EnergyProfileSampleCount { get; set; } = 300;

        /// <summary>Samples required before a profile is considered usable.</summary>
        public int MinimumProfileSamples { get; set; } = 20;

        /// <summary>How long the per-participant energy log is kept.</summary>
        public TimeSpan EnergyLogRetention { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Tolerance applied around the declared phase boundaries.</summary>
        public TimeSpan PhaseMargin { get; set; } = TimeSpan.FromSeconds(5);
    }

    public class SpeechProcessing
    {
        private readonly Pipeline pipeline;
        private readonly SpeechProcessingConfiguration config;

        private readonly Dictionary<int, ParticipantSpeechState> states = new Dictionary<int, ParticipantSpeechState>();
        private readonly Dictionary<int, Receiver<AudioBuffer>> audioReceivers = new Dictionary<int, Receiver<AudioBuffer>>();
        private readonly Dictionary<int, Receiver<float>> logReceivers = new Dictionary<int, Receiver<float>>();
        private readonly Dictionary<int, Receiver<bool>> vadReceivers = new Dictionary<int, Receiver<bool>>();
        private readonly Dictionary<int, Receiver<Tuple<int, Queue<SpeakingTimeIDData>>>> speakingReceivers = new Dictionary<int, Receiver<Tuple<int, Queue<SpeakingTimeIDData>>>>();
        private readonly Dictionary<int, Receiver<Tuple<int, List<SpeakingTimeIDData>>>> speakingListReceivers = new Dictionary<int, Receiver<Tuple<int, List<SpeakingTimeIDData>>>>();
        private readonly Dictionary<int, Receiver<Dictionary<int, Queue<TTData>>>> ttovReceivers = new Dictionary<int, Receiver<Dictionary<int, Queue<TTData>>>>();
        private readonly Dictionary<int, Receiver<Dictionary<int, Queue<TTData>>>> ttoutReceivers = new Dictionary<int, Receiver<Dictionary<int, Queue<TTData>>>>();
        private readonly Dictionary<int, Receiver<Dictionary<int, Queue<TTData>>>> ovReceivers = new Dictionary<int, Receiver<Dictionary<int, Queue<TTData>>>>();
        private readonly Dictionary<int, Emitter<Dictionary<int, Queue<SpeakingTimeIDData>>>> speakingQueueEmitters = new Dictionary<int, Emitter<Dictionary<int, Queue<SpeakingTimeIDData>>>>();

        private readonly List<(DateTime Time, int Id, string Text)> textEntries = new List<(DateTime, int, string)>();

        private bool topologyFrozen;
        private bool sessionF2FStarted;
        private bool sessionXRStarted;
        private bool sessionInterviewStarted;

        public DateTime lastMessage;

        public SpeechProcessing(Pipeline pipeline, SpeechProcessingConfiguration configuration = null)
        {
            this.pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            this.config = configuration ?? new SpeechProcessingConfiguration();

            // Multiplexed inputs: one receiver each, whatever the number of participants,
            // because the participant id travels inside the message.
            this.audioIn = pipeline.CreateReceiver<AudioBuffer>(this, this.ProcessSessionAudio, nameof(this.audioIn));
            this.SpeakingAnyIn = pipeline.CreateReceiver<Tuple<int, Queue<SpeakingTimeIDData>>>(this, this.ProcessSpeakingData, nameof(this.SpeakingAnyIn));
            this.SpeakingListAnyIn = pipeline.CreateReceiver<Tuple<int, List<SpeakingTimeIDData>>>(this, this.ProcessSpeakingDataList, nameof(this.SpeakingListAnyIn));
            this.ttovAllIn = pipeline.CreateReceiver<Dictionary<int, Queue<TTData>>>(this, this.ProcessTTovIn, nameof(this.ttovAllIn));
            this.ttoutAllIn = pipeline.CreateReceiver<Dictionary<int, Queue<TTData>>>(this, this.ProcessTToutIn, nameof(this.ttoutAllIn));
            this.ovAllIn = pipeline.CreateReceiver<Dictionary<int, Queue<TTData>>>(this, this.ProcessOvIn, nameof(this.ovAllIn));

            this.ttovAllOut = pipeline.CreateEmitter<Dictionary<int, Queue<TTData>>>(this, nameof(this.ttovAllOut));
            this.ttoutAllOut = pipeline.CreateEmitter<Dictionary<int, Queue<TTData>>>(this, nameof(this.ttoutAllOut));
            this.ovAllOut = pipeline.CreateEmitter<Dictionary<int, Queue<TTData>>>(this, nameof(this.ovAllOut));
            this.TimestampOut = pipeline.CreateEmitter<string>(this, nameof(this.TimestampOut));
            this.audioOut = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.audioOut));
            this.audioMultiChannelOut = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.audioMultiChannelOut));
            this.audioVROut = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.audioVROut));
            this.audioInterviewOut = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.audioInterviewOut));
            this.speakingTimeDictOut = pipeline.CreateEmitter<Dictionary<int, List<SpeakingTimeIDData>>>(this, nameof(this.speakingTimeDictOut));

            // Pre-declared participants get their receivers and emitters up front.
            foreach (int participantId in this.config.ParticipantIds ?? new List<uint>())
            {
                this.GetAudioReceiver(participantId);
                this.GetLogReceiver(participantId);
                this.GetVadReceiver(participantId);
                this.GetSpeakingReceiver(participantId);
                this.GetSpeakingListReceiver(participantId);
                this.GetTTovReceiver(participantId);
                this.GetTToutReceiver(participantId);
                this.GetOvReceiver(participantId);
                this.GetSpeakingQueueEmitter(participantId);
            }

            pipeline.PipelineRun += (_, __) => this.topologyFrozen = true;
        }

        // ---------------------------------------------------------------------------------
        // Shared state
        // ---------------------------------------------------------------------------------

        public IReadOnlyDictionary<int, ParticipantSpeechState> States => this.states;

        public IEnumerable<int> KnownParticipantIds => this.states.Keys;

        public Dictionary<int, List<SpeakingTimeIDData>> speakingTimeDictList { get; } = new Dictionary<int, List<SpeakingTimeIDData>>();

        public Dictionary<int, Queue<SpeakingTimeIDData>> speakingTimeDictQueue { get; } = new Dictionary<int, Queue<SpeakingTimeIDData>>();

        public Dictionary<int, Queue<TTData>> ttovDict { get; } = new Dictionary<int, Queue<TTData>>();

        public Dictionary<int, Queue<TTData>> ttoutDict { get; } = new Dictionary<int, Queue<TTData>>();

        public Dictionary<int, Queue<TTData>> ovDict { get; } = new Dictionary<int, Queue<TTData>>();

        public ParticipantSpeechState GetOrCreateState(int participantId)
        {
            if (!this.states.TryGetValue(participantId, out var state))
            {
                state = new ParticipantSpeechState(participantId, this.config);
                this.states[participantId] = state;
            }

            return state;
        }

        // ---------------------------------------------------------------------------------
        // Per participant receivers, created on demand
        // ---------------------------------------------------------------------------------

        private TReceiver GetOrCreate<TReceiver>(
            Dictionary<int, TReceiver> registry,
            int participantId,
            string kind,
            Func<TReceiver> factory)
        {
            if (registry.TryGetValue(participantId, out TReceiver existing))
            {
                return existing;
            }

            if (this.topologyFrozen)
            {
                throw new InvalidOperationException(
                    $"Cannot create the {kind} receiver of participant {participantId}: the pipeline is already " +
                    "running and \\psi does not allow the topology to change. Either declare the participant in " +
                    "FusionSpeechProcessingConfiguration.ParticipantIds before starting, or route this participant " +
                    "through one of the multiplexed inputs (SpeakingAnyIn, SpeakingListAnyIn, ttovAllIn, ...).");
            }

            TReceiver created = factory();
            registry[participantId] = created;
            this.GetOrCreateState(participantId);
            return created;
        }

        public Receiver<AudioBuffer> GetAudioReceiver(int participantId)
            => this.GetOrCreate(this.audioReceivers, participantId, "audio", () =>
                this.pipeline.CreateReceiver<AudioBuffer>(
                    this,
                    (buffer, envelope) => this.ProcessAudio(participantId, buffer, envelope),
                    $"audioIn-{participantId}"));

        public Receiver<float> GetLogReceiver(int participantId)
            => this.GetOrCreate(this.logReceivers, participantId, "log energy", () =>
                this.pipeline.CreateReceiver<float>(
                    this,
                    (value, envelope) => this.ProcessLog(participantId, value, envelope),
                    $"logIn-{participantId}"));

        public Receiver<bool> GetVadReceiver(int participantId)
            => this.GetOrCreate(this.vadReceivers, participantId, "VAD", () =>
                this.pipeline.CreateReceiver<bool>(
                    this,
                    (value, envelope) => this.ProcessVad(participantId, value, envelope),
                    $"vadIn-{participantId}"));

        public Receiver<Tuple<int, Queue<SpeakingTimeIDData>>> GetSpeakingReceiver(int participantId)
            => this.GetOrCreate(this.speakingReceivers, participantId, "speaking", () =>
                this.pipeline.CreateReceiver<Tuple<int, Queue<SpeakingTimeIDData>>>(
                    this, this.ProcessSpeakingData, $"speakingIn-{participantId}"));

        public Receiver<Tuple<int, List<SpeakingTimeIDData>>> GetSpeakingListReceiver(int participantId)
            => this.GetOrCreate(this.speakingListReceivers, participantId, "speaking list", () =>
                this.pipeline.CreateReceiver<Tuple<int, List<SpeakingTimeIDData>>>(
                    this, this.ProcessSpeakingDataList, $"speakingListIn-{participantId}"));

        public Receiver<Dictionary<int, Queue<TTData>>> GetTTovReceiver(int participantId)
            => this.GetOrCreate(this.ttovReceivers, participantId, "turn taking with overlap", () =>
                this.pipeline.CreateReceiver<Dictionary<int, Queue<TTData>>>(
                    this, this.ProcessTTovIn, $"ttovIn-{participantId}"));

        public Receiver<Dictionary<int, Queue<TTData>>> GetTToutReceiver(int participantId)
            => this.GetOrCreate(this.ttoutReceivers, participantId, "turn taking without overlap", () =>
                this.pipeline.CreateReceiver<Dictionary<int, Queue<TTData>>>(
                    this, this.ProcessTToutIn, $"ttoutIn-{participantId}"));

        public Receiver<Dictionary<int, Queue<TTData>>> GetOvReceiver(int participantId)
            => this.GetOrCreate(this.ovReceivers, participantId, "overlap", () =>
                this.pipeline.CreateReceiver<Dictionary<int, Queue<TTData>>>(
                    this, this.ProcessOvIn, $"ovIn-{participantId}"));

        public Emitter<Dictionary<int, Queue<SpeakingTimeIDData>>> GetSpeakingQueueEmitter(int participantId)
        {
            if (this.speakingQueueEmitters.TryGetValue(participantId, out var emitter))
            {
                return emitter;
            }

            if (this.topologyFrozen)
            {
                throw new InvalidOperationException(
                    $"Cannot create the speaking queue emitter of participant {participantId} after the pipeline " +
                    "has started. Declare the participant in ParticipantIds, or consume speakingTimeDictOut, " +
                    "which carries every participant.");
            }

            emitter = this.pipeline.CreateEmitter<Dictionary<int, Queue<SpeakingTimeIDData>>>(
                this, $"speakingTimeQueueOut-{participantId}");
            this.speakingQueueEmitters[participantId] = emitter;
            this.GetOrCreateState(participantId);
            return emitter;
        }

        // Legacy names, kept so that existing wiring code compiles unchanged.
        public Receiver<AudioBuffer> CheckAudioReceiver(int id) => this.GetAudioReceiver(id);

        public Receiver<float> CheckLogReceiver(int id) => this.GetLogReceiver(id);

        public Receiver<bool> CheckVadReceiver(int id) => this.GetVadReceiver(id);

        public Receiver<Tuple<int, Queue<SpeakingTimeIDData>>> CheckReceiver(int id) => this.GetSpeakingReceiver(id);

        public Receiver<Tuple<int, List<SpeakingTimeIDData>>> CheckListReceiver(int id) => this.GetSpeakingListReceiver(id);

        public Receiver<Dictionary<int, Queue<TTData>>> CheckTTovReceiver(int id) => this.GetTTovReceiver(id);

        public Receiver<Dictionary<int, Queue<TTData>>> CheckTToutReceiver(int id) => this.GetTToutReceiver(id);

        public Receiver<Dictionary<int, Queue<TTData>>> CheckOvReceiver(int id) => this.GetOvReceiver(id);

        public Emitter<Dictionary<int, Queue<SpeakingTimeIDData>>> CheckQueueEmitter(int id) => this.GetSpeakingQueueEmitter(id);

        // ---------------------------------------------------------------------------------
        // Handlers
        // ---------------------------------------------------------------------------------

        private void ProcessLog(int participantId, float value, Envelope envelope)
        {
            ParticipantSpeechState state = this.GetOrCreateState(participantId);

            // Indexer assignment, not Add: SortedList.Add throws on a duplicate key, and two
            // messages sharing an originating time would take the pipeline down.
            state.EnergyLog[envelope.OriginatingTime] = value;
            state.Profile.AddSample(value);
            state.CurrentVad = (envelope.OriginatingTime, false);

            this.PruneEnergyLog(state, envelope.OriginatingTime);
        }

        private void PruneEnergyLog(ParticipantSpeechState state, DateTime currentTime)
        {
            if (this.config.EnergyLogRetention <= TimeSpan.Zero)
            {
                return;
            }

            DateTime oldestAllowed = currentTime - this.config.EnergyLogRetention;
            while (state.EnergyLog.Count > 0 && state.EnergyLog.Keys[0] < oldestAllowed)
            {
                state.EnergyLog.RemoveAt(0);
            }
        }

        private void ProcessVad(int participantId, bool value, Envelope envelope)
            => this.GetOrCreateState(participantId).LastVad = (envelope.OriginatingTime, value);

        private void ProcessAudio(int participantId, AudioBuffer buffer, Envelope envelope)
        {
            ParticipantSpeechState state = this.GetOrCreateState(participantId);
            state.AudioBuffer = buffer;
            state.HasAudio = true;
        }

        private void ProcessSpeakingData(Tuple<int, Queue<SpeakingTimeIDData>> message, Envelope envelope)
        {
            if (message == null || message.Item2 == null)
            {
                return;
            }

            int participantId = message.Item1;
            ParticipantSpeechState state = this.GetOrCreateState(participantId);

            state.SpeakingQueue = message.Item2;
            this.speakingTimeDictQueue[participantId] = message.Item2;

            // Only participants wired to a dedicated emitter get one; the rest are still
            // present in speakingTimeDictQueue and in speakingTimeDictOut.
            if (this.speakingQueueEmitters.TryGetValue(participantId, out var emitter))
            {
                emitter.Post(this.speakingTimeDictQueue, envelope.OriginatingTime);
            }

            SpeakingTimeIDData last = message.Item2.LastOrDefault();
            if (last != null && !string.IsNullOrEmpty(last.Text))
            {
                this.textEntries.Add((envelope.OriginatingTime, participantId, last.Text));
            }
        }

        private void ProcessSpeakingDataList(Tuple<int, List<SpeakingTimeIDData>> message, Envelope envelope)
        {
            if (message == null || message.Item2 == null)
            {
                return;
            }

            this.GetOrCreateState(message.Item1).SpeakingList = message.Item2;
            this.speakingTimeDictList[message.Item1] = message.Item2;
            this.speakingTimeDictOut.Post(this.speakingTimeDictList, envelope.OriginatingTime);
        }

        // The nine near-identical ProcessTTov1In / ProcessTTout2In / ... handlers collapse into
        // one merge routine, so a new participant needs no new code at all.
        private void ProcessTTovIn(Dictionary<int, Queue<TTData>> dictionary, Envelope envelope)
            => MergeAndPost(dictionary, this.ttovDict, this.ttovAllOut, envelope.OriginatingTime);

        private void ProcessTToutIn(Dictionary<int, Queue<TTData>> dictionary, Envelope envelope)
            => MergeAndPost(dictionary, this.ttoutDict, this.ttoutAllOut, envelope.OriginatingTime);

        private void ProcessOvIn(Dictionary<int, Queue<TTData>> dictionary, Envelope envelope)
            => MergeAndPost(dictionary, this.ovDict, this.ovAllOut, envelope.OriginatingTime);

        private static void MergeAndPost(
            Dictionary<int, Queue<TTData>> source,
            Dictionary<int, Queue<TTData>> target,
            Emitter<Dictionary<int, Queue<TTData>>> emitter,
            DateTime originatingTime)
        {
            if (source == null)
            {
                return;
            }

            foreach (var entry in source)
            {
                Queue<TTData> sourceQueue = entry.Value;
                if (sourceQueue == null || sourceQueue.Count == 0)
                {
                    continue;
                }

                if (target.TryGetValue(entry.Key, out Queue<TTData> targetQueue))
                {
                    if (!QueuesAreEqual(sourceQueue, targetQueue))
                    {
                        target[entry.Key] = sourceQueue.DeepClone();
                    }
                }
                else
                {
                    // Clone on first insertion too, otherwise the component holds a reference
                    // to a queue that the upstream component keeps mutating.
                    target[entry.Key] = sourceQueue.DeepClone();
                }
            }

            emitter.Post(target, originatingTime);
        }

        private static bool QueuesAreEqual(Queue<TTData> a, Queue<TTData> b)
            => a.Count == b.Count && a.SequenceEqual(b);

        /// <summary>
        /// Routes the session audio to the F2F, XR or interview output depending on the phase.
        /// </summary>
        private void ProcessSessionAudio(AudioBuffer buffer, Envelope envelope)
        {
            DateTime time = envelope.OriginatingTime;

            if (time != this.lastMessage)
            {
                this.audioOut.Post(buffer, time);
            }

            this.lastMessage = time;
        }

        // ---------------------------------------------------------------------------------
        // Streams
        // ---------------------------------------------------------------------------------

        /// <summary>Session audio, routed to F2F / XR / interview by phase.</summary>
        public Receiver<AudioBuffer> audioIn { get; }

        /// <summary>
        /// Speaking queues of any participant. The id is carried in the message, so a single
        /// receiver serves any number of participants, including ones unknown at construction.
        /// </summary>
        public Receiver<Tuple<int, Queue<SpeakingTimeIDData>>> SpeakingAnyIn { get; }

        /// <summary>Speaking lists of any participant.</summary>
        public Receiver<Tuple<int, List<SpeakingTimeIDData>>> SpeakingListAnyIn { get; }

        public Receiver<Dictionary<int, Queue<TTData>>> ttovAllIn { get; }

        public Receiver<Dictionary<int, Queue<TTData>>> ttoutAllIn { get; }

        public Receiver<Dictionary<int, Queue<TTData>>> ovAllIn { get; }

        public Emitter<Dictionary<int, Queue<TTData>>> ttovAllOut { get; }

        public Emitter<Dictionary<int, Queue<TTData>>> ttoutAllOut { get; }

        public Emitter<Dictionary<int, Queue<TTData>>> ovAllOut { get; }

        public Emitter<AudioBuffer> audioOut { get; }

        /// <summary>Interleaved audio of every declared participant, N channels.</summary>
        public Emitter<AudioBuffer> audioMultiChannelOut { get; }

        public Emitter<AudioBuffer> audioVROut { get; }

        public Emitter<AudioBuffer> audioInterviewOut { get; }

        public Emitter<string> TimestampOut { get; }

        /// <summary>Speaking lists of every participant seen so far.</summary>
        public Emitter<Dictionary<int, List<SpeakingTimeIDData>>> speakingTimeDictOut { get; }
    }
}
