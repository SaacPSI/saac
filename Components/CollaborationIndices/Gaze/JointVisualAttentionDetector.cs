using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class JointVisualAttentionDetectorConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>
        /// Two looks at the same object are joint when one starts at most this long after the
        /// other ended (or while it lasts).
        /// </summary>
        public TimeSpan MaximumDelay { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// If true, a joint look also requires that the heads of the two participants point
        /// to the same place: see <see cref="ConvergenceDistance"/>. This is what tells apart
        /// two objects of the same name, when the identifier of a gazed object is its kind
        /// ("SM_droit(Clone)") and not the instance. Needs the head poses.
        /// </summary>
        public bool RequireConvergingHeads { get; set; } = false;

        /// <summary>
        /// The head directions of two participants converge when the two half lines pass
        /// within this distance of each other, in front of both participants.
        /// </summary>
        public float ConvergenceDistance { get; set; } = 1.0f;

        /// <summary>A head pose older than this is not used: the convergence cannot be established.</summary>
        public TimeSpan MaximumPoseAge { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>Looks kept to be matched with the looks of the others.</summary>
        public TimeSpan HistoryDuration { get; set; } = TimeSpan.FromSeconds(60);
    }

    /// <summary>
    /// Joint visual attention: two participants look at the same object within a short delay.
    ///
    /// Each look at an object arrives when it ends. It is then compared with the looks of
    /// every other participant at the same object: for each of them, the earliest look that
    /// overlaps it or ended less than <see cref="JointVisualAttentionDetectorConfiguration.MaximumDelay"/>
    /// before it started makes an episode. The participant who looked first is the initiator
    /// (ParticipantId), the other the responder (TargetId), and the episode is stamped with
    /// the moment the responder started looking. With three participants on the same object
    /// each pair gives an episode.
    ///
    /// Replaces the joint attention part of the legacy GazeMetrics. Differences:
    ///  - the initiator is the participant who looked first. The legacy named initiator the
    ///    participant with the lowest index, whatever the order of the looks;
    ///  - the legacy required the two participants to "look at the same zone", but computed
    ///    it from the components of the head quaternion read as angles in degrees: the test
    ///    did not depend on where the heads pointed. Here the test is optional and uses the
    ///    head directions;
    ///  - the legacy published nothing for the pairs when the three participants looked at
    ///    the object and the test failed for one pair.
    /// </summary>
    public class JointVisualAttentionDetector
    {
        private readonly JointVisualAttentionDetectorConfiguration configuration;
        private readonly ReceiverMap<uint, Tuple<Vector3, Vector3>> poseReceivers;
        private readonly Dictionary<uint, List<InteractionInterval>> history = new Dictionary<uint, List<InteractionInterval>>();
        private readonly Dictionary<uint, (DateTime Time, Vector3 Position, Vector3 Forward)> poses = new Dictionary<uint, (DateTime, Vector3, Vector3)>();
        private readonly MonotonicPoster poster = new MonotonicPoster(NonMonotonicPolicy.Advance);

        public JointVisualAttentionDetector(Pipeline pipeline, JointVisualAttentionDetectorConfiguration configuration, string name = nameof(JointVisualAttentionDetector))
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.ObjectGazeIn = pipeline.CreateReceiver<InteractionInterval>(this, this.ReceiveObjectGaze, $"{name}-ObjectGaze");
            this.poseReceivers = new ReceiverMap<uint, Tuple<Vector3, Vector3>>(pipeline, this, configuration.ParticipantIds, this.ReceivePose, $"{name}-HeadPose");
            this.Out = pipeline.CreateEmitter<InteractionEvent>(this, $"{name}-JointVisualAttention");
            this.RejectedOut = pipeline.CreateEmitter<InteractionEvent>(this, $"{name}-Rejected");

            foreach (uint participantId in configuration.ParticipantIds)
            {
                this.history[participantId] = new List<InteractionInterval>();
            }
        }

        /// <summary>Looks at an object of every participant, the object in the label (GazeEpisodeDetector.ObjectGazeOut).</summary>
        public Receiver<InteractionInterval> ObjectGazeIn { get; }

        /// <summary>Episodes: category JointVisualAttention, initiator and responder, the object in the label.</summary>
        public Emitter<InteractionEvent> Out { get; }

        /// <summary>Episodes discarded because the heads did not converge, for inspection.</summary>
        public Emitter<InteractionEvent> RejectedOut { get; }

        /// <summary>Head of one participant: position and forward direction, both in the same frame.</summary>
        public Receiver<Tuple<Vector3, Vector3>> GetHeadPoseInput(uint participantId) => this.poseReceivers[participantId];

        /// <summary>
        /// True when the half lines (p1, d1) and (p2, d2) pass within maxDistance of each other
        /// at a point in front of both origins. False for parallel directions.
        /// </summary>
        public static bool Converge(Vector3 p1, Vector3 d1, Vector3 p2, Vector3 d2, float maxDistance)
        {
            if (d1.LengthSquared() < 1e-12f || d2.LengthSquared() < 1e-12f)
            {
                return false;
            }

            d1 = Vector3.Normalize(d1);
            d2 = Vector3.Normalize(d2);
            Vector3 w0 = p1 - p2;
            float b = Vector3.Dot(d1, d2);
            float d = Vector3.Dot(d1, w0);
            float e = Vector3.Dot(d2, w0);
            float denominator = 1f - (b * b);
            if (denominator < 1e-6f)
            {
                return false;
            }

            float s = ((b * e) - d) / denominator;
            float t = (e - (b * d)) / denominator;
            if (s <= 0 || t <= 0)
            {
                return false;
            }

            return Vector3.Distance(p1 + (s * d1), p2 + (t * d2)) < maxDistance;
        }

        private void ReceivePose(uint participantId, Tuple<Vector3, Vector3> pose, Envelope envelope)
        {
            if (pose != null)
            {
                this.poses[participantId] = (envelope.OriginatingTime, pose.Item1, pose.Item2);
            }
        }

        private void ReceiveObjectGaze(InteractionInterval gaze, Envelope envelope)
        {
            if (gaze == null || gaze.IsOpen || !this.history.ContainsKey(gaze.ParticipantId))
            {
                return;
            }

            DateTime now = envelope.OriginatingTime;
            InteractionInterval current = gaze.Clone();
            foreach (var entry in this.history)
            {
                entry.Value.RemoveAll(g => g.EndTime < now - this.configuration.HistoryDuration);
                if (entry.Key == current.ParticipantId)
                {
                    continue;
                }

                // The looks of a participant are stored in the order they ended, which is also
                // the order they started as long as they do not overlap: the first match is the
                // earliest.
                InteractionInterval other = entry.Value.Find(g =>
                    g.Label == current.Label
                    && g.EndTime > current.StartTime - this.configuration.MaximumDelay
                    && g.StartTime < current.EndTime + this.configuration.MaximumDelay);
                if (other == null)
                {
                    continue;
                }

                bool currentFirst = current.StartTime <= other.StartTime;
                uint initiator = currentFirst ? current.ParticipantId : other.ParticipantId;
                uint responder = currentFirst ? other.ParticipantId : current.ParticipantId;
                DateTime joint = currentFirst ? other.StartTime : current.StartTime;
                var episode = new InteractionEvent(joint, IndexCategories.JointVisualAttention, initiator, responder, 1.0, current.Label);

                if (this.configuration.RequireConvergingHeads && !this.HeadsConverge(current.ParticipantId, other.ParticipantId, now))
                {
                    this.poster.Post(this.RejectedOut, episode, now);
                    continue;
                }

                this.poster.Post(this.Out, episode, now);
            }

            this.history[current.ParticipantId].Add(current);
        }

        private bool HeadsConverge(uint a, uint b, DateTime now)
        {
            if (!this.poses.TryGetValue(a, out var poseA) || !this.poses.TryGetValue(b, out var poseB))
            {
                return false;
            }

            if (now - poseA.Time > this.configuration.MaximumPoseAge || now - poseB.Time > this.configuration.MaximumPoseAge)
            {
                return false;
            }

            return Converge(poseA.Position, poseA.Forward, poseB.Position, poseB.Forward, this.configuration.ConvergenceDistance);
        }
    }
}
