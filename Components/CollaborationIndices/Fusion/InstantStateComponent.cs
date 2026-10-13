// <copyright file="InstantStateComponent.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Psi;
using SAAC.Groups;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Settings of <see cref="InstantStateComponent"/>.
    /// </summary>
    public class InstantStateConfiguration
    {
        /// <summary>Gets or sets the participants.</summary>
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>Gets or sets the vertical axis of the head poses: Y for the frame of a Unity scene, Z for the standardised frame.</summary>
        public Vector3 UpAxis { get; set; } = Vector3.UnitY;

        /// <summary>
        /// Gets or sets how long a look must have lasted to be part of the state: shorter, it
        /// is a glance or tracking noise, as for the gaze episodes.
        /// </summary>
        public TimeSpan MinimumLookDuration { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>Gets or sets how long a detection stays among the events of the state.</summary>
        public TimeSpan EventLifetime { get; set; } = TimeSpan.FromSeconds(4);

        /// <summary>
        /// Gets or sets how long the last head pose of a participant stands for where it is:
        /// a participant that has not been seen for longer has no place.
        /// </summary>
        public TimeSpan PlaceLifetime { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Gets or sets how long the last voice activity sample of a participant stands: a
        /// participant whose voice activity stopped coming is not speaking.
        /// </summary>
        public TimeSpan VoiceLifetime { get; set; } = TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// What the group is doing at each instant (<see cref="GroupInstant"/>), to be drawn:
    /// the counterpart of the sociogram of the indices, which describes the last sliding
    /// window.
    ///
    /// Nothing here is a statistic. Each signal is taken as it is at the tick:
    ///  - the place and the heading of a participant: its last head pose;
    ///  - whether it speaks: its voice activity, which is known at once, whereas a speech
    ///    interval is only known once the recogniser has answered;
    ///  - who or what it is looking at: the looks that are running, from the start and the
    ///    end of each look (<see cref="GazeEpisodeDetector.LookStartOut"/>), once they have
    ///    lasted the minimum duration; two participants looking at each other are in mutual
    ///    gaze, two participants looking at the same object share it;
    ///  - the formation of a pair: from its start to its end;
    ///  - what a participant holds: from a grab to the release or the placing of the object.
    /// The detections themselves (joint visual attention, mutual gaze, turn taking, action
    /// on an object, handover) are listed with the state for a few seconds: a detector
    /// names an event when it is complete, which may be some time after it began.
    ///
    /// Every input is optional. <see cref="GroupInstant.Signals"/> names the signals that
    /// are known to be there, and what depends on another one is left out of the state:
    /// the places, the voices and the looks once the first one has arrived (an eye tracker
    /// that works reports looks at once); the formations as soon as their input is connected
    /// and the heads they are computed from arrive, whether or not a pair stands in one; the
    /// task as soon as its input is connected, since nobody may touch an object for a long time.
    /// </summary>
    public class InstantStateComponent : IProducer<GroupInstant>
    {
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly InstantStateConfiguration configuration;
        private readonly ReceiverMap<uint, Tuple<Vector3, Vector3>> poseReceivers;
        private readonly ReceiverMap<uint, bool> voiceReceivers;
        private readonly Dictionary<uint, (DateTime Time, double X, double Y, double HeadingX, double HeadingY)> places = new Dictionary<uint, (DateTime, double, double, double, double)>();
        private readonly Dictionary<uint, (DateTime Time, bool Speaking)> voices = new Dictionary<uint, (DateTime, bool)>();
        private readonly Dictionary<(uint Gazer, uint Gazed), DateTime> peerLooks = new Dictionary<(uint, uint), DateTime>();
        private readonly Dictionary<(uint Gazer, string Object), DateTime> objectLooks = new Dictionary<(uint, string), DateTime>();
        private readonly Dictionary<ParticipantPair, string> formations = new Dictionary<ParticipantPair, string>();
        private readonly Dictionary<uint, List<string>> held = new Dictionary<uint, List<string>>();
        private readonly List<InstantEvent> events = new List<InstantEvent>();
        private readonly HashSet<string> signals = new HashSet<string>();
        private DateTime lastTick = DateTime.MinValue;
        private int detectionInputs = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstantStateComponent"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="configuration">The participants and the settings.</param>
        /// <param name="name">Name of the component.</param>
        public InstantStateComponent(Pipeline pipeline, InstantStateConfiguration configuration, string name = nameof(InstantStateComponent))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.poseReceivers = new ReceiverMap<uint, Tuple<Vector3, Vector3>>(pipeline, this, configuration.ParticipantIds, this.ReceivePose, $"{name}-HeadPose");
            this.voiceReceivers = new ReceiverMap<uint, bool>(pipeline, this, configuration.ParticipantIds, this.ReceiveVoice, $"{name}-Voice");
            this.LookStartIn = pipeline.CreateReceiver<InteractionEvent>(this, (look, envelope) => this.ReceiveLook(look, true), $"{name}-LookStart");
            this.LookEndIn = pipeline.CreateReceiver<InteractionEvent>(this, (look, envelope) => this.ReceiveLook(look, false), $"{name}-LookEnd");
            this.FormationStartIn = pipeline.CreateReceiver<InteractionEvent>(this, (formation, envelope) => this.ReceiveFormation(formation, true), $"{name}-FormationStart");
            this.FormationEndIn = pipeline.CreateReceiver<InteractionEvent>(this, (formation, envelope) => this.ReceiveFormation(formation, false), $"{name}-FormationEnd");
            this.TaskActionIn = pipeline.CreateReceiver<InteractionEvent>(this, this.ReceiveTaskAction, $"{name}-TaskAction");
            this.DetectionIn = pipeline.CreateReceiver<InteractionEvent>(this, (detection, envelope) => this.Remember(detection, envelope.OriginatingTime), $"{name}-Detection");
            this.CancelledDetectionIn = pipeline.CreateReceiver<InteractionEvent>(this, (detection, envelope) => this.Forget(detection), $"{name}-CancelledDetection");
            this.TickIn = pipeline.CreateReceiver<bool>(this, (_, envelope) => this.Publish(envelope.OriginatingTime), $"{name}-Tick");
            this.Out = pipeline.CreateEmitter<GroupInstant>(this, $"{name}-Instant");

            foreach (uint id in configuration.ParticipantIds)
            {
                this.held[id] = new List<string>();
            }
        }

        /// <summary>Gets the state of the group, one message per tick.</summary>
        public Emitter<GroupInstant> Out { get; }

        /// <summary>Gets the clock of the state: any stream on the time of the data, at the rate the state is wanted.</summary>
        public Receiver<bool> TickIn { get; }

        /// <summary>Gets the input of the starts of the looks (<see cref="GazeEpisodeDetector.LookStartOut"/>).</summary>
        public Receiver<InteractionEvent> LookStartIn { get; }

        /// <summary>Gets the input of the ends of the looks (<see cref="GazeEpisodeDetector.LookEndOut"/>).</summary>
        public Receiver<InteractionEvent> LookEndIn { get; }

        /// <summary>Gets the input of the starts of the formations (<see cref="FFormationDetector.FormationStartOut"/>).</summary>
        public Receiver<InteractionEvent> FormationStartIn { get; }

        /// <summary>Gets the input of the ends of the formations (<see cref="FFormationDetector.FormationEndOut"/>).</summary>
        public Receiver<InteractionEvent> FormationEndIn { get; }

        /// <summary>
        /// Gets the input of the actions on the objects of the task (<see cref="TaskActionDetector.Out"/>):
        /// they are listed among the events, and a grab puts the object in the hands of the
        /// participant until it is released or placed.
        /// </summary>
        public Receiver<InteractionEvent> TaskActionIn { get; }

        /// <summary>
        /// Gets the input of any other detection to list among the events of the state: joint
        /// visual attention, mutual gaze, turn taking, handover. A detection stays listed for
        /// the lifetime of an event after it was detected, whenever it happened.
        /// </summary>
        public Receiver<InteractionEvent> DetectionIn { get; }

        /// <summary>
        /// Gets the input of the detections that are withdrawn (VoiceTurnTakingDetector.CancelledOut):
        /// they are no longer listed.
        /// </summary>
        public Receiver<InteractionEvent> CancelledDetectionIn { get; }

        /// <summary>
        /// Another input like <see cref="DetectionIn"/>: a receiver reads one stream, and each
        /// detector publishes its own. To call before the pipeline runs.
        /// </summary>
        /// <returns>The receiver.</returns>
        public Receiver<InteractionEvent> AddDetectionInput()
        {
            this.detectionInputs++;
            return this.pipeline.CreateReceiver<InteractionEvent>(this, (detection, envelope) => this.Remember(detection, envelope.OriginatingTime), $"{this.name}-Detection{this.detectionInputs}");
        }

        /// <summary>The input of the head pose of a participant: its position and the direction it faces.</summary>
        /// <param name="participantId">The participant.</param>
        /// <returns>The receiver.</returns>
        public Receiver<Tuple<Vector3, Vector3>> GetHeadPoseInput(uint participantId) => this.poseReceivers[participantId];

        /// <summary>The input of the voice activity of a participant: true while it speaks.</summary>
        /// <param name="participantId">The participant.</param>
        /// <returns>The receiver.</returns>
        public Receiver<bool> GetVoiceInput(uint participantId) => this.voiceReceivers[participantId];

        private void ReceivePose(uint participantId, Tuple<Vector3, Vector3> pose, Envelope envelope)
        {
            if (pose == null)
            {
                return;
            }

            this.signals.Add(GroupInstant.Places);
            (double x, double y) = SocialGraph.OnFloor(pose.Item1, this.configuration.UpAxis);
            (double headingX, double headingY) = SocialGraph.OnFloor(pose.Item2, this.configuration.UpAxis);
            double length = Math.Sqrt((headingX * headingX) + (headingY * headingY));
            if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y))
            {
                return;
            }

            // A head that looks straight up or down faces nowhere on the floor.
            bool faces = length > 1e-3 && !double.IsNaN(length) && !double.IsInfinity(length);
            this.places[participantId] = (envelope.OriginatingTime, x, y, faces ? headingX / length : 0.0, faces ? headingY / length : 0.0);
        }

        private void ReceiveVoice(uint participantId, bool speaking, Envelope envelope)
        {
            this.signals.Add(GroupInstant.Voice);
            this.voices[participantId] = (envelope.OriginatingTime, speaking);
        }

        private void ReceiveLook(InteractionEvent look, bool starts)
        {
            if (look == null || !this.configuration.ParticipantIds.Contains(look.ParticipantId))
            {
                return;
            }

            this.signals.Add(GroupInstant.Gaze);
            if (look.TargetId.HasValue)
            {
                var key = (look.ParticipantId, look.TargetId.Value);
                if (!starts)
                {
                    this.peerLooks.Remove(key);
                }
                else if (!this.peerLooks.ContainsKey(key) && this.configuration.ParticipantIds.Contains(key.Item2))
                {
                    this.peerLooks[key] = look.OriginatingTime;
                }
            }
            else if (!string.IsNullOrEmpty(look.Label))
            {
                var key = (look.ParticipantId, look.Label);
                if (!starts)
                {
                    this.objectLooks.Remove(key);
                }
                else if (!this.objectLooks.ContainsKey(key))
                {
                    this.objectLooks[key] = look.OriginatingTime;
                }
            }
        }

        private void ReceiveFormation(InteractionEvent formation, bool starts)
        {
            if (formation == null || !formation.TargetId.HasValue)
            {
                return;
            }

            this.signals.Add(GroupInstant.Formations);
            var pair = new ParticipantPair(formation.ParticipantId, formation.TargetId.Value);
            if (starts)
            {
                this.formations[pair] = formation.Label ?? string.Empty;
            }
            else
            {
                this.formations.Remove(pair);
            }
        }

        private void ReceiveTaskAction(InteractionEvent action, Envelope envelope)
        {
            if (action == null)
            {
                return;
            }

            this.signals.Add(GroupInstant.Task);
            this.Remember(action, envelope.OriginatingTime);
            if (!this.held.TryGetValue(action.ParticipantId, out List<string> objects))
            {
                return;
            }

            // The label of a task action is "object@place".
            string name = ObjectOf(action.Label);
            if (action.Category == IndexCategories.Grab)
            {
                if (name.Length > 0 && !objects.Contains(name))
                {
                    objects.Add(name);
                }
            }
            else if (action.Category == IndexCategories.Ungrab || action.Category == IndexCategories.Place)
            {
                objects.Remove(name);
            }
        }

        private void Forget(InteractionEvent detection)
        {
            if (detection == null)
            {
                return;
            }

            string person = SocialGraph.NodeId(detection.ParticipantId);
            string other = detection.TargetId.HasValue ? SocialGraph.NodeId(detection.TargetId.Value) : string.Empty;
            this.events.RemoveAll(listed => listed.Time == detection.OriginatingTime && listed.Category == (detection.Category ?? string.Empty) && listed.Person == person && listed.Other == other);
        }

        private void Remember(InteractionEvent detection, DateTime detectedAt)
        {
            if (detection == null)
            {
                return;
            }

            // A detector may date what it publishes before the instant it publishes it: a turn
            // taking decided from a transcription that came late is dated like that transcription.
            // The detection reaches the state now, and is listed from now.
            this.events.Add(new InstantEvent
            {
                Time = detection.OriginatingTime,
                DetectedAt = detectedAt < this.lastTick ? this.lastTick : detectedAt,
                Category = detection.Category ?? string.Empty,
                Person = SocialGraph.NodeId(detection.ParticipantId),
                Other = detection.TargetId.HasValue ? SocialGraph.NodeId(detection.TargetId.Value) : string.Empty,
                Label = detection.Label ?? string.Empty,
            });
        }

        private void Publish(DateTime now)
        {
            // A session that starts again, or a replay moved backwards: what is running is forgotten.
            if (now < this.lastTick)
            {
                this.places.Clear();
                this.voices.Clear();
                this.peerLooks.Clear();
                this.objectLooks.Clear();
                this.formations.Clear();
                this.events.Clear();
                foreach (List<string> objects in this.held.Values)
                {
                    objects.Clear();
                }
            }

            this.lastTick = now;

            // A formation or an action may not happen for a long time: their signals are there
            // as soon as something can detect them.
            if (((IReceiver)this.FormationStartIn).Source != null && this.signals.Contains(GroupInstant.Places))
            {
                this.signals.Add(GroupInstant.Formations);
            }

            if (((IReceiver)this.TaskActionIn).Source != null)
            {
                this.signals.Add(GroupInstant.Task);
            }

            var state = new GroupInstant { Signals = this.signals.OrderBy(signal => signal).ToList() };
            TimeSpan minimumLook = this.configuration.MinimumLookDuration;

            foreach (uint id in this.configuration.ParticipantIds)
            {
                var person = new PersonInstant { Id = SocialGraph.NodeId(id) };

                // The streams of the participants are not read in step: the clock follows one of
                // them, and the last pose or voice sample of another may be a little after the tick.
                if (this.places.TryGetValue(id, out var place) && (now - place.Time).Duration() <= this.configuration.PlaceLifetime)
                {
                    person.HasPlace = true;
                    person.X = place.X;
                    person.Y = place.Y;
                    person.HeadingX = place.HeadingX;
                    person.HeadingY = place.HeadingY;
                }

                person.Speaking = this.voices.TryGetValue(id, out var voice) && voice.Speaking && (now - voice.Time).Duration() <= this.configuration.VoiceLifetime;
                person.LooksAtPersons = this.peerLooks
                    .Where(look => look.Key.Gazer == id && now - look.Value >= minimumLook)
                    .Select(look => SocialGraph.NodeId(look.Key.Gazed))
                    .OrderBy(other => other, Comparer<string>.Create(GraphMetrics.CompareIds))
                    .ToList();
                person.LooksAtObjects = this.objectLooks
                    .Where(look => look.Key.Gazer == id && now - look.Value >= minimumLook)
                    .Select(look => look.Key.Object)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToList();
                person.Holds = new List<string>(this.held[id]);
                state.Persons.Add(person);
            }

            // A pair is part of the state when it has something in common at this instant.
            foreach (ParticipantPair pair in Combinatorics.Pairs(this.configuration.ParticipantIds))
            {
                PersonInstant first = state.FindPerson(SocialGraph.NodeId(pair.A))!;
                PersonInstant second = state.FindPerson(SocialGraph.NodeId(pair.B))!;
                var common = new PairInstant
                {
                    First = first.Id,
                    Second = second.Id,
                    Formation = this.formations.TryGetValue(pair, out string? kind) ? kind : string.Empty,
                    MutualGaze = first.LooksAtPersons.Contains(second.Id) && second.LooksAtPersons.Contains(first.Id),
                    SharedObjects = first.LooksAtObjects.Intersect(second.LooksAtObjects).ToList(),
                };

                if (common.Formation.Length > 0 || common.MutualGaze || common.SharedObjects.Count > 0)
                {
                    state.Pairs.Add(common);
                }
            }

            this.events.RemoveAll(detection => now - detection.DetectedAt > this.configuration.EventLifetime);
            state.Events = this.events.Where(detection => detection.DetectedAt <= now).OrderBy(detection => detection.DetectedAt).ToList();
            this.Out.Post(state, now);
        }

        private static string ObjectOf(string? label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return string.Empty;
            }

            int at = label!.IndexOf('@');
            return at < 0 ? label : label.Substring(0, at);
        }
    }
}
