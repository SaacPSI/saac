using System;
using System.Collections.Generic;
using Microsoft.Psi;
using SAAC.PsiFormats;

namespace SAAC.CollaborationIndices
{
    public class TaskActionDetectorConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>
        /// A piece only changes colour on purpose in this area; a colour event received while
        /// the participant is elsewhere is dropped. Null keeps every colour event. The name
        /// is the one the area presence detector publishes.
        /// </summary>
        public string ColorArea { get; set; } = "IterationTable";

        /// <summary>
        /// The scene sends bursts of colour events for one gesture: an event that follows the
        /// previous colour event of the participant by less than this is dropped.
        /// </summary>
        public TimeSpan ColorDebounce { get; set; } = TimeSpan.FromMilliseconds(100);
    }

    /// <summary>
    /// Task actions of the participants, from the piece and generator streams of the headsets.
    ///
    /// Each piece status becomes an event whose category is the action (Grab, Ungrab, Place,
    /// Unplace, Color, Uncolor, and Spawn or Destroy which no indicator counts) and whose
    /// label is "object@place". For a grab or a release the place is the area the
    /// participant stands in, which is what the inefficient action rule and the handover
    /// detector compare; for the other actions it is the location carried by the status.
    /// Each generator interaction becomes a GeneratorInteraction event.
    ///
    /// The participant of an event is the one whose input received it: the identifiers
    /// inside the recorded statuses are those of the headsets and start at 1.
    ///
    /// Out goes to the TaskParticipation indicator.
    ///
    /// Replaces the legacy InteractionFiltering. The colour events were debounced there by
    /// blocking the receiver for 100 ms of machine time; here the delay is measured on the
    /// time of the data.
    /// </summary>
    public class TaskActionDetector
    {
        private readonly TaskActionDetectorConfiguration configuration;
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly ReceiverMap<uint, PieceStatus> pieceReceivers;
        private readonly Dictionary<(uint, int), Receiver<GeneratorInteraction>> generatorReceivers = new Dictionary<(uint, int), Receiver<GeneratorInteraction>>();
        private readonly ReceiverMap<uint, string> areaReceivers;
        private readonly EmitterMap<uint, InteractionEvent> actionEmitters;
        private readonly Dictionary<uint, string> currentArea = new Dictionary<uint, string>();
        private readonly Dictionary<uint, DateTime> lastColorEvent = new Dictionary<uint, DateTime>();
        private readonly MonotonicPoster poster = new MonotonicPoster(NonMonotonicPolicy.Advance);

        public TaskActionDetector(Pipeline pipeline, TaskActionDetectorConfiguration configuration, string name = nameof(TaskActionDetector))
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.pipeline = pipeline;
            this.name = name;
            this.pieceReceivers = new ReceiverMap<uint, PieceStatus>(pipeline, this, configuration.ParticipantIds, this.ReceivePiece, $"{name}-Piece");
            this.areaReceivers = new ReceiverMap<uint, string>(pipeline, this, configuration.ParticipantIds, (id, area, _) => this.currentArea[id] = area, $"{name}-CurrentArea");
            this.actionEmitters = new EmitterMap<uint, InteractionEvent>(pipeline, this, configuration.ParticipantIds, $"{name}-Actions");
            this.Out = pipeline.CreateEmitter<InteractionEvent>(this, $"{name}-Actions");
        }

        /// <summary>Task actions of every participant.</summary>
        public Emitter<InteractionEvent> Out { get; }

        /// <summary>Piece statuses of the headset of one participant.</summary>
        public Receiver<PieceStatus> GetPieceInput(uint participantId) => this.pieceReceivers[participantId];

        /// <summary>
        /// Generator interactions of the headset of one participant. A headset publishes one
        /// stream per generator: each stream gets its own input, identified by the generator.
        /// </summary>
        public Receiver<GeneratorInteraction> GetGeneratorInput(uint participantId, int generator = 1)
        {
            if (!this.configuration.ParticipantIds.Contains(participantId))
            {
                throw new ArgumentException($"No participant {participantId} declared.", nameof(participantId));
            }

            var key = (participantId, generator);
            if (!this.generatorReceivers.TryGetValue(key, out var receiver))
            {
                receiver = this.pipeline.CreateReceiver<GeneratorInteraction>(
                    this, (interaction, envelope) => this.ReceiveGenerator(participantId, interaction, envelope), $"{this.name}-Generator{generator}-{participantId}");
                this.generatorReceivers[key] = receiver;
            }

            return receiver;
        }

        /// <summary>Area one participant is in (AreaPresenceDetector.GetCurrentAreaEmitter).</summary>
        public Receiver<string> GetCurrentAreaInput(uint participantId) => this.areaReceivers[participantId];

        /// <summary>Task actions of one participant.</summary>
        public Emitter<InteractionEvent> GetActionEmitter(uint participantId) => this.actionEmitters[participantId];

        private void ReceivePiece(uint participantId, PieceStatus status, Envelope envelope)
        {
            if (status == null)
            {
                return;
            }

            DateTime time = envelope.OriginatingTime;
            this.currentArea.TryGetValue(participantId, out string area);
            string place = status.currentLocation.ToString();

            switch (status.type)
            {
                case State.Grab:
                case State.Ungrab:
                    place = area ?? place;
                    break;

                case State.Colored:
                case State.Uncolored:
                    if (this.configuration.ColorArea != null && area != this.configuration.ColorArea)
                    {
                        return;
                    }

                    if (this.lastColorEvent.TryGetValue(participantId, out DateTime last) && time - last < this.configuration.ColorDebounce)
                    {
                        this.lastColorEvent[participantId] = time;
                        return;
                    }

                    this.lastColorEvent[participantId] = time;
                    break;
            }

            string category = SerializableClassAdapters.CategoryOf(status.type);
            this.Publish(new InteractionEvent(time, category, participantId, null, 1.0, $"{status.objectID}@{place}"), time);
        }

        private void ReceiveGenerator(uint participantId, GeneratorInteraction interaction, Envelope envelope)
        {
            if (interaction == null)
            {
                return;
            }

            DateTime time = envelope.OriginatingTime;
            string label = $"Generator{interaction.generatorID}:{interaction.interactionType}:{interaction.objectID}";
            this.Publish(new InteractionEvent(time, IndexCategories.GeneratorInteraction, participantId, null, 1.0, label), time);
        }

        private void Publish(InteractionEvent action, DateTime time)
        {
            this.poster.Post(this.actionEmitters[action.ParticipantId], action, time);
            this.poster.Post(this.Out, action.Clone(), time);
        }
    }

    public class ObjectHandoverDetectorConfiguration
    {
        /// <summary>Longest time between the release by one participant and the grab by another.</summary>
        public TimeSpan MaximumDelay { get; set; } = TimeSpan.FromSeconds(2);
    }

    /// <summary>
    /// Object handovers: a participant grabs an object that another participant released at
    /// the same place less than MaximumDelay before. The object and the place are compared
    /// through the label of the actions ("object@place"), as published by the task action
    /// detector.
    ///
    /// An event carries the giver in ParticipantId and the receiver in TargetId, and is
    /// stamped with the grab. Same rule as the legacy ObjectHandover.
    /// </summary>
    public class ObjectHandoverDetector
    {
        private readonly ObjectHandoverDetectorConfiguration configuration;
        private readonly List<InteractionEvent> releases = new List<InteractionEvent>();
        private readonly MonotonicPoster poster = new MonotonicPoster(NonMonotonicPolicy.Advance);

        public ObjectHandoverDetector(Pipeline pipeline, ObjectHandoverDetectorConfiguration configuration = null, string name = nameof(ObjectHandoverDetector))
        {
            this.configuration = configuration ?? new ObjectHandoverDetectorConfiguration();
            this.ActionsIn = pipeline.CreateReceiver<InteractionEvent>(this, this.ReceiveAction, $"{name}-Actions");
            this.Out = pipeline.CreateEmitter<InteractionEvent>(this, $"{name}-Handover");
        }

        /// <summary>Task actions of every participant (TaskActionDetector.Out).</summary>
        public Receiver<InteractionEvent> ActionsIn { get; }

        /// <summary>Handovers: category ObjectHandover, giver and receiver, "object@place" in the label.</summary>
        public Emitter<InteractionEvent> Out { get; }

        private void ReceiveAction(InteractionEvent action, Envelope envelope)
        {
            if (action == null)
            {
                return;
            }

            DateTime time = action.OriginatingTime;
            this.releases.RemoveAll(r => time - r.OriginatingTime > this.configuration.MaximumDelay);

            if (action.Category == IndexCategories.Ungrab)
            {
                this.releases.Add(action.Clone());
            }
            else if (action.Category == IndexCategories.Grab)
            {
                InteractionEvent release = this.releases.FindLast(r =>
                    r.ParticipantId != action.ParticipantId && r.Label == action.Label && r.OriginatingTime <= time);
                if (release != null)
                {
                    this.releases.Remove(release);
                    this.poster.Post(
                        this.Out,
                        new InteractionEvent(time, DetectionCategories.ObjectHandover, release.ParticipantId, action.ParticipantId, 1.0, action.Label),
                        envelope.OriginatingTime);
                }
            }
        }
    }
}
