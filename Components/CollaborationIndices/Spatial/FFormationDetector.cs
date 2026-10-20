using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>Kinds of formation of two participants.</summary>
    public static class FormationTypes
    {
        public const string FaceToFace = "FaceToFace";
        public const string LShape = "LShape";
        public const string SideBySide = "SideBySide";
    }

    public class FFormationDetectorConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>Period, in data time, at which the distances and the formations are evaluated.</summary>
        public TimeSpan SamplingInterval { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>A participant whose last pose is older than this is not evaluated.</summary>
        public TimeSpan MaximumPoseAge { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// A formation starts once it has been observed for this long without interruption,
        /// and ends once it has not been observed for this long.
        /// </summary>
        public TimeSpan TransitionDuration { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// If true (default), angles are measured on the horizontal plane: a participant who
        /// looks down at a table still faces the person across it. If false they are
        /// measured in space, pitch of the head included, as the legacy detector did.
        /// </summary>
        public bool UseHorizontalPlane { get; set; } = true;

        /// <summary>Vertical axis of the frame of the poses (Y for a Unity scene).</summary>
        public Vector3 UpAxis { get; set; } = Vector3.UnitY;

        /// <summary>Largest distance of a face to face formation, in metres.</summary>
        public float FaceToFaceDistance { get; set; } = 2.0f;

        /// <summary>Largest distance of an L shape or side by side formation, in metres.</summary>
        public float CloseDistance { get; set; } = 1.5f;

        /// <summary>Face to face: the two headings are opposed by more than this, in degrees.</summary>
        public float FaceToFaceMinimumAngle { get; set; } = 150f;

        /// <summary>L shape: the angle between the two headings is in this range, in degrees.</summary>
        public float LShapeMinimumAngle { get; set; } = 60f;

        public float LShapeMaximumAngle { get; set; } = 120f;

        /// <summary>Side by side: the two headings differ by less than this, in degrees.</summary>
        public float SideBySideMaximumAngle { get; set; } = 45f;
    }

    /// <summary>
    /// Interpersonal distances and F-formations (Kendon) of each pair of participants, from
    /// the position and the direction of their heads.
    ///
    /// At each sampling instant, for a pair (a, b) at distance d, with h the angle between
    /// the two headings and v(d) the angle within which each must have the other in front
    /// (90 degrees up to 0.5 m, 75 up to 1 m, 60 up to 1.5 m, 45 up to 2 m):
    ///
    ///  - face to face: d under 2 m, each has the other within v(d), h over 150 degrees;
    ///  - L shape:      d under 1.5 m, each has the other within v(d), h between 60 and 120;
    ///  - side by side: d under 1.5 m, b is on the side of a (between 60 and 120 degrees of
    ///                  its heading), h under 45 degrees.
    ///
    /// The first that holds is the observation of the instant. A formation starts after
    /// TransitionDuration of the same observation and ends after TransitionDuration without
    /// it; the end is stamped with the last instant it was observed.
    ///
    /// Outputs: the distance of each pair (to the Proximity indicator) and FormationEndOut
    /// (to the Formation indicator, which counts the formations that ended in the window).
    ///
    /// Replaces the legacy FFormationDetect, with the same rules and thresholds. Differences:
    ///  - every participant is evaluated on its latest pose. The legacy refreshed the position
    ///    of a single participant per second, so its distances were up to several seconds old;
    ///  - the observation must be continuous for a formation to start. In the legacy an
    ///    observation followed, any time later, by a second one of the same kind was enough;
    ///  - angles are measured on the horizontal plane by default;
    ///  - the formations of three participants (triangle) are not detected: nothing read them.
    /// </summary>
    public class FFormationDetector
    {
        private readonly FFormationDetectorConfiguration configuration;
        private readonly ReceiverMap<uint, Tuple<Vector3, Vector3>> poseReceivers;
        private readonly EmitterMap<ParticipantPair, double> distanceEmitters;
        private readonly List<ParticipantPair> pairs;
        private readonly Dictionary<uint, (DateTime Time, Vector3 Position, Vector3 Forward)> poses = new Dictionary<uint, (DateTime, Vector3, Vector3)>();
        private readonly Dictionary<ParticipantPair, PairState> states = new Dictionary<ParticipantPair, PairState>();
        private readonly MonotonicPoster poster = new MonotonicPoster(NonMonotonicPolicy.Advance);
        private DateTime lastSample = DateTime.MinValue;

        public FFormationDetector(Pipeline pipeline, FFormationDetectorConfiguration configuration, string name = nameof(FFormationDetector))
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.pairs = new List<ParticipantPair>(Combinatorics.Pairs(configuration.ParticipantIds));
            this.poseReceivers = new ReceiverMap<uint, Tuple<Vector3, Vector3>>(pipeline, this, configuration.ParticipantIds, this.ReceivePose, $"{name}-HeadPose");
            this.distanceEmitters = new EmitterMap<ParticipantPair, double>(pipeline, this, this.pairs, $"{name}-Distance");
            this.DistancesOut = pipeline.CreateEmitter<Dictionary<ParticipantPair, double>>(this, $"{name}-Distances");
            this.FormationStartOut = pipeline.CreateEmitter<InteractionEvent>(this, $"{name}-FormationStart");
            this.FormationEndOut = pipeline.CreateEmitter<InteractionEvent>(this, $"{name}-FormationEnd");
            this.FormationOut = pipeline.CreateEmitter<InteractionInterval>(this, $"{name}-Formation");

            foreach (ParticipantPair pair in this.pairs)
            {
                this.states[pair] = new PairState();
            }
        }

        /// <summary>Distance of every pair at each sampling instant, in the unit of the positions.</summary>
        public Emitter<Dictionary<ParticipantPair, double>> DistancesOut { get; }

        /// <summary>A formation begins: category FormationStart, the two participants, the kind in the label.</summary>
        public Emitter<InteractionEvent> FormationStartOut { get; }

        /// <summary>A formation ended: category FormationEnd, the two participants, the kind in the label, stamped with its last observation.</summary>
        public Emitter<InteractionEvent> FormationEndOut { get; }

        /// <summary>The formations that ended, with their duration: category Formation, the kind in the label.</summary>
        public Emitter<InteractionInterval> FormationOut { get; }

        /// <summary>Head of one participant: position and forward direction, both in the same frame.</summary>
        public Receiver<Tuple<Vector3, Vector3>> GetHeadPoseInput(uint participantId) => this.poseReceivers[participantId];

        /// <summary>Distance of one pair at each sampling instant.</summary>
        public Emitter<double> GetDistanceEmitter(ParticipantPair pair) => this.distanceEmitters[pair];

        public Emitter<double> GetDistanceEmitter(uint participantA, uint participantB) => this.GetDistanceEmitter(new ParticipantPair(participantA, participantB));

        /// <summary>
        /// Forward direction of a Unity transform from its Euler angles in degrees
        /// (X pitch, Y yaw), in the frame of the scene.
        /// </summary>
        public static Vector3 ForwardFromUnityEuler(Vector3 eulerDegrees)
        {
            double pitch = eulerDegrees.X * Math.PI / 180.0;
            double yaw = eulerDegrees.Y * Math.PI / 180.0;
            return new Vector3(
                (float)(Math.Cos(pitch) * Math.Sin(yaw)),
                (float)(-Math.Sin(pitch)),
                (float)(Math.Cos(pitch) * Math.Cos(yaw)));
        }

        /// <summary>Angle within which each participant must have the other in front, for a distance.</summary>
        public static float VisibilityAngle(float distance)
        {
            if (distance <= 0.5f)
            {
                return 90f;
            }

            if (distance <= 1.0f)
            {
                return 75f;
            }

            if (distance <= 1.5f)
            {
                return 60f;
            }

            return distance <= 2.0f ? 45f : 30f;
        }

        /// <summary>Kind of formation of two poses, or null.</summary>
        public string Classify(Vector3 positionA, Vector3 forwardA, Vector3 positionB, Vector3 forwardB)
        {
            Vector3 toB = positionB - positionA;
            if (this.configuration.UseHorizontalPlane)
            {
                toB = this.Flatten(toB);
                forwardA = this.Flatten(forwardA);
                forwardB = this.Flatten(forwardB);
            }

            float distance = toB.Length();
            if (distance < 1e-6f || forwardA.LengthSquared() < 1e-12f || forwardB.LengthSquared() < 1e-12f)
            {
                return null;
            }

            double headings = AngleBetween(forwardA, forwardB);
            double aSeesB = AngleBetween(forwardA, toB);
            double bSeesA = AngleBetween(forwardB, -toB);
            float visibility = VisibilityAngle(distance);
            bool mutuallyVisible = aSeesB <= visibility && bSeesA <= visibility;

            if (distance < this.configuration.FaceToFaceDistance && mutuallyVisible && headings > this.configuration.FaceToFaceMinimumAngle)
            {
                return FormationTypes.FaceToFace;
            }

            if (distance < this.configuration.CloseDistance && mutuallyVisible
                && headings > this.configuration.LShapeMinimumAngle && headings < this.configuration.LShapeMaximumAngle)
            {
                return FormationTypes.LShape;
            }

            // b on the side of a: the direction to b is between 60 and 120 degrees of the heading of a.
            if (distance < this.configuration.CloseDistance
                && Math.Abs(Math.Cos(aSeesB * Math.PI / 180.0)) < 0.5
                && headings < this.configuration.SideBySideMaximumAngle)
            {
                return FormationTypes.SideBySide;
            }

            return null;
        }

        private static double AngleBetween(Vector3 a, Vector3 b)
        {
            double dot = Vector3.Dot(Vector3.Normalize(a), Vector3.Normalize(b));
            return Math.Acos(Math.Max(-1.0, Math.Min(1.0, dot))) * 180.0 / Math.PI;
        }

        private Vector3 Flatten(Vector3 vector)
        {
            Vector3 up = Vector3.Normalize(this.configuration.UpAxis);
            return vector - (Vector3.Dot(vector, up) * up);
        }

        private void ReceivePose(uint participantId, Tuple<Vector3, Vector3> pose, Envelope envelope)
        {
            if (pose == null)
            {
                return;
            }

            DateTime time = envelope.OriginatingTime;
            this.poses[participantId] = (time, pose.Item1, pose.Item2);
            if (this.lastSample != DateTime.MinValue && time - this.lastSample < this.configuration.SamplingInterval)
            {
                return;
            }

            this.lastSample = time;
            this.Sample(time);
        }

        private void Sample(DateTime time)
        {
            var distances = new Dictionary<ParticipantPair, double>();
            foreach (ParticipantPair pair in this.pairs)
            {
                string observed = null;
                if (this.poses.TryGetValue(pair.A, out var a) && this.poses.TryGetValue(pair.B, out var b)
                    && time - a.Time <= this.configuration.MaximumPoseAge && time - b.Time <= this.configuration.MaximumPoseAge)
                {
                    double distance = Vector3.Distance(a.Position, b.Position);
                    distances[pair] = distance;
                    this.poster.Post(this.distanceEmitters[pair], distance, time);
                    observed = this.Classify(a.Position, a.Forward, b.Position, b.Forward);
                }

                this.Update(pair, this.states[pair], observed, time);
            }

            if (distances.Count > 0)
            {
                this.poster.Post(this.DistancesOut, distances, time);
            }
        }

        private void Update(ParticipantPair pair, PairState state, string observed, DateTime time)
        {
            if (state.Current != null && observed == state.Current)
            {
                state.LastSeen = time;
            }

            // What is observed and is not the running formation is a candidate, for as long
            // as it is observed without interruption.
            if (observed != null && observed != state.Current)
            {
                if (state.Candidate != observed)
                {
                    state.Candidate = observed;
                    state.CandidateSince = time;
                }
            }
            else
            {
                state.Candidate = null;
            }

            bool candidateConfirmed = state.Candidate != null && time - state.CandidateSince >= this.configuration.TransitionDuration;
            bool currentLost = state.Current != null && time - state.LastSeen > this.configuration.TransitionDuration;

            if (state.Current != null && (candidateConfirmed || currentLost))
            {
                this.poster.Post(this.FormationEndOut, new InteractionEvent(state.LastSeen, IndexCategories.FormationEnd, pair.A, pair.B, 1.0, state.Current), time);
                this.poster.Post(this.FormationOut, new InteractionInterval(state.Start, state.LastSeen, IndexCategories.Formation, pair.A, pair.B, state.Current), time);
                state.Current = null;
            }

            if (candidateConfirmed)
            {
                state.Current = state.Candidate;
                state.Start = state.CandidateSince;
                state.LastSeen = time;
                state.Candidate = null;
                this.poster.Post(this.FormationStartOut, new InteractionEvent(state.Start, DetectionCategories.FormationStart, pair.A, pair.B, 1.0, state.Current), time);
            }
        }

        private class PairState
        {
            public string Current;
            public DateTime Start;
            public DateTime LastSeen;
            public string Candidate;
            public DateTime CandidateSince;
        }
    }
}
