using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Psi;
using SAAC.PsiFormats;

namespace SAAC.CollaborationIndices
{
    public class GazeEpisodeDetectorConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>A gaze shorter than this is a glance or tracking noise, and is dropped.</summary>
        public TimeSpan MinimumDuration { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// A gaze on a peer that ends less than this after the end of the previous gaze on the
        /// same peer is the same look interrupted by a blink, and is not counted again.
        /// </summary>
        public TimeSpan PeerRefractoryPeriod { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// A gaze that never received its end event is abandoned when a new one starts after
        /// this long. Only used when a participant has one gaze at a time.
        /// </summary>
        public TimeSpan StaleGazeTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>Value of ObjectGazeEvent.type for a gaze on an object.</summary>
        public string ObjectType { get; set; } = "object";

        /// <summary>Value of ObjectGazeEvent.type for a gaze on the avatar of a peer.</summary>
        public string PeerType { get; set; } = "avatar";

        /// <summary>Numbering of the peers in ObjectGazeEvent.objectID of the peer gaze streams.</summary>
        public ParticipantIdMap PeerIdMap { get; set; } = ParticipantIdMap.ZeroBased;

        /// <summary>
        /// True (default): each target has its own gaze, from its start event to its end event,
        /// so that two targets hit by the same ray are both followed.
        ///
        /// False reproduces the legacy GazeFilter, which followed one gaze at a time: on
        /// objects a start event was ignored while a gaze was running and any end event closed
        /// it, whatever its object; on peers a start event on another peer closed the running
        /// gaze and an end event only counted for the peer being looked at.
        /// </summary>
        public bool TrackEachTarget { get; set; } = true;
    }

    /// <summary>
    /// Turns the raw gaze events of the eye trackers (the gaze enters or leaves a target) into
    /// gaze episodes: which participant looked at which target, from when to when.
    ///
    /// Two inputs per participant, as in the recorded sessions: the events on objects come
    /// from the headset, the events on peers from the replay of the scene on the server.
    /// Each input only reads the events of its kind.
    ///
    /// Outputs:
    ///  - ObjectGazeOut: an interval per look at an object, to the joint visual attention detector;
    ///  - PeerGazeOut: an event per look at a peer, to the GazeOnPeers indicator;
    ///  - PeerGazeIntervalOut: the same looks with their duration;
    ///  - GetGazingAtPeerEmitter(id): whether the participant is looking at a peer, to the
    ///    AttentionLevel indicator.
    ///
    /// Replaces GazeFilter and the gaze on peers part of GazeMetrics.
    /// </summary>
    public class GazeEpisodeDetector
    {
        private readonly GazeEpisodeDetectorConfiguration configuration;
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly Dictionary<(uint, int), Receiver<ObjectGazeEvent>> objectReceivers = new Dictionary<(uint, int), Receiver<ObjectGazeEvent>>();
        private readonly Dictionary<(uint, int), Receiver<ObjectGazeEvent>> peerReceivers = new Dictionary<(uint, int), Receiver<ObjectGazeEvent>>();
        private readonly EmitterMap<uint, bool> gazingAtPeerEmitters;
        private readonly MonotonicPoster poster = new MonotonicPoster(NonMonotonicPolicy.Advance);

        // Running gazes of each participant: target -> start. One entry at most when a participant has one gaze at a time.
        private readonly Dictionary<uint, Dictionary<string, DateTime>> objectGazes = new Dictionary<uint, Dictionary<string, DateTime>>();
        private readonly Dictionary<uint, Dictionary<string, DateTime>> peerGazes = new Dictionary<uint, Dictionary<string, DateTime>>();

        // End of the last counted gaze of each (gazer, gazed).
        private readonly Dictionary<DirectedParticipantPair, DateTime> lastPeerGazeEnd = new Dictionary<DirectedParticipantPair, DateTime>();

        public GazeEpisodeDetector(Pipeline pipeline, GazeEpisodeDetectorConfiguration configuration, string name = nameof(GazeEpisodeDetector))
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.pipeline = pipeline;
            this.name = name;
            this.gazingAtPeerEmitters = new EmitterMap<uint, bool>(pipeline, this, configuration.ParticipantIds, $"{name}-GazingAtPeer");

            this.ObjectGazeOut = pipeline.CreateEmitter<InteractionInterval>(this, $"{name}-ObjectGaze");
            this.PeerGazeOut = pipeline.CreateEmitter<InteractionEvent>(this, $"{name}-PeerGaze");
            this.PeerGazeIntervalOut = pipeline.CreateEmitter<InteractionInterval>(this, $"{name}-PeerGazeInterval");

            foreach (uint participantId in configuration.ParticipantIds)
            {
                this.objectGazes[participantId] = new Dictionary<string, DateTime>();
                this.peerGazes[participantId] = new Dictionary<string, DateTime>();
            }
        }

        /// <summary>Looks at an object: category GazeOnObject, the object in the label. Published when the look ends.</summary>
        public Emitter<InteractionInterval> ObjectGazeOut { get; }

        /// <summary>Looks at a peer: category GazeOnPeer, gazer and gazed, stamped with the end of the look.</summary>
        public Emitter<InteractionEvent> PeerGazeOut { get; }

        /// <summary>Looks at a peer as intervals: category GazeOnPeer, gazer and gazed.</summary>
        public Emitter<InteractionInterval> PeerGazeIntervalOut { get; }

        /// <summary>
        /// Gaze events of the headset of one participant; only the events on objects are read.
        /// A headset has one stream per eye and publishes on either: each stream gets its own
        /// input, identified by <paramref name="source"/>, and they feed the same gaze.
        /// </summary>
        public Receiver<ObjectGazeEvent> GetObjectGazeInput(uint participantId, int source = 0)
            => this.GetOrCreate(this.objectReceivers, participantId, source, this.ReceiveObjectGaze, "ObjectGaze");

        /// <summary>
        /// Gaze events of one participant on the avatars; only the events on peers are read.
        /// Several streams of the same participant each get their own input.
        /// </summary>
        public Receiver<ObjectGazeEvent> GetPeerGazeInput(uint participantId, int source = 0)
            => this.GetOrCreate(this.peerReceivers, participantId, source, this.ReceivePeerGaze, "PeerGaze");

        /// <summary>True when one participant starts looking at a peer, false when no peer is looked at any more.</summary>
        public Emitter<bool> GetGazingAtPeerEmitter(uint participantId) => this.gazingAtPeerEmitters[participantId];

        private Receiver<ObjectGazeEvent> GetOrCreate(
            Dictionary<(uint, int), Receiver<ObjectGazeEvent>> receivers, uint participantId, int source, Action<uint, ObjectGazeEvent, Envelope> handler, string kind)
        {
            if (!this.configuration.ParticipantIds.Contains(participantId))
            {
                throw new ArgumentException($"No participant {participantId} declared.", nameof(participantId));
            }

            var key = (participantId, source);
            if (!receivers.TryGetValue(key, out var receiver))
            {
                receiver = this.pipeline.CreateReceiver<ObjectGazeEvent>(
                    this, (gaze, envelope) => handler(participantId, gaze, envelope), $"{this.name}-{kind}{source}-{participantId}");
                receivers[key] = receiver;
            }

            return receiver;
        }

        private void ReceiveObjectGaze(uint participantId, ObjectGazeEvent gaze, Envelope envelope)
        {
            if (gaze == null || gaze.type != this.configuration.ObjectType || gaze.objectID == null)
            {
                return;
            }

            Dictionary<string, DateTime> running = this.objectGazes[participantId];
            DateTime time = envelope.OriginatingTime;
            if (this.configuration.TrackEachTarget)
            {
                if (gaze.status)
                {
                    if (!running.ContainsKey(gaze.objectID))
                    {
                        running[gaze.objectID] = time;
                    }
                }
                else if (running.TryGetValue(gaze.objectID, out DateTime start))
                {
                    running.Remove(gaze.objectID);
                    this.PublishObjectGaze(participantId, gaze.objectID, start, time);
                }

                return;
            }

            // One gaze at a time (legacy): the end event names the object, the start event gives the time.
            if (gaze.status)
            {
                if (running.Count == 0)
                {
                    running[gaze.objectID] = time;
                }
            }
            else if (running.Count > 0)
            {
                DateTime start = running.Values.First();
                running.Clear();
                this.PublishObjectGaze(participantId, gaze.objectID, start, time);
            }
        }

        private void ReceivePeerGaze(uint participantId, ObjectGazeEvent gaze, Envelope envelope)
        {
            if (gaze == null || gaze.type != this.configuration.PeerType || gaze.objectID == null)
            {
                return;
            }

            Dictionary<string, DateTime> running = this.peerGazes[participantId];
            DateTime time = envelope.OriginatingTime;
            bool wasGazing = running.Count > 0;

            if (this.configuration.TrackEachTarget)
            {
                if (gaze.status)
                {
                    if (!running.ContainsKey(gaze.objectID))
                    {
                        running[gaze.objectID] = time;
                    }
                }
                else if (running.TryGetValue(gaze.objectID, out DateTime start))
                {
                    running.Remove(gaze.objectID);
                    this.PublishPeerGaze(participantId, gaze.objectID, start, time);
                }
            }
            else if (gaze.status)
            {
                if (running.Count == 0)
                {
                    running[gaze.objectID] = time;
                }
                else
                {
                    var current = running.First();
                    if (current.Key != gaze.objectID)
                    {
                        // The gaze moves to another peer: the running one ends here.
                        running.Clear();
                        this.PublishPeerGaze(participantId, current.Key, current.Value, time);
                        running[gaze.objectID] = time;
                    }
                    else if (time - current.Value > this.configuration.StaleGazeTimeout)
                    {
                        running[gaze.objectID] = time;
                    }
                }
            }
            else if (running.TryGetValue(gaze.objectID, out DateTime start))
            {
                running.Clear();
                this.PublishPeerGaze(participantId, gaze.objectID, start, time);
            }

            bool isGazing = running.Count > 0;
            if (isGazing != wasGazing)
            {
                this.poster.Post(this.gazingAtPeerEmitters[participantId], isGazing, time);
            }
        }

        private void PublishObjectGaze(uint participantId, string objectId, DateTime start, DateTime end)
        {
            if (end - start < this.configuration.MinimumDuration)
            {
                return;
            }

            var interval = new InteractionInterval(start, end, DetectionCategories.GazeOnObject, participantId, null, objectId);
            this.poster.Post(this.ObjectGazeOut, interval, end);
        }

        private void PublishPeerGaze(uint gazer, string peer, DateTime start, DateTime end)
        {
            if (end - start < this.configuration.MinimumDuration
                || !int.TryParse(peer, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sourceId))
            {
                return;
            }

            uint gazed = this.configuration.PeerIdMap.ToParticipantId(sourceId);
            if (gazed == gazer)
            {
                return;
            }

            var pair = new DirectedParticipantPair(gazer, gazed);
            if (this.lastPeerGazeEnd.TryGetValue(pair, out DateTime previousEnd) && end - previousEnd < this.configuration.PeerRefractoryPeriod)
            {
                return;
            }

            this.lastPeerGazeEnd[pair] = end;
            this.poster.Post(this.PeerGazeOut, new InteractionEvent(end, IndexCategories.GazeOnPeer, gazer, gazed), end);
            this.poster.Post(this.PeerGazeIntervalOut, new InteractionInterval(start, end, IndexCategories.GazeOnPeer, gazer, gazed), end);
        }
    }
}
