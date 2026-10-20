using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Psi;
using SAAC.PsiFormats;

namespace SAAC.CollaborationIndices
{
    public class CollaborationProfilesConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        public CollaborationProfileRulesConfiguration Rules { get; set; } = new CollaborationProfileRulesConfiguration();

        /// <summary>Identifier of the session, written in the files.</summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>One row per pair and per profile at each tick: confidence, criteria, ranges. Null writes nothing.</summary>
        public TextWriter PairWriter { get; set; }

        /// <summary>One row at each tick: the profile of each pair and of the group. Null writes nothing.</summary>
        public TextWriter GroupWriter { get; set; }

        /// <summary>One row each time the profile of the group changes: how long the previous one lasted. Null writes nothing.</summary>
        public TextWriter DurationWriter { get; set; }
    }

    /// <summary>The profiles of one tick.</summary>
    public class CollaborationProfiles
    {
        public DateTime OriginatingTime { get; set; }

        public Dictionary<ParticipantPair, PairProfileResult> Pairs { get; set; } = new Dictionary<ParticipantPair, PairProfileResult>();

        public GroupProfile Group { get; set; }
    }

    /// <summary>
    /// Collaboration profile of each pair and of the group, at each tick of the indices.
    ///
    /// The component reads the snapshot of the indices (indices.SnapshotOut), which holds every
    /// value of a tick, and applies <see cref="CollaborationProfileRules"/>. It needs these
    /// indicators: VerbalParticipation, SpeechEquality, TalkingMost, TurnTaking,
    /// JointVisualAttention, GazeOnPeers, TaskEquality, TaskingMost, Formation and Synchrony;
    /// a missing index counts as 0. Who leads the joint attention inside each pair is not in
    /// the snapshot and comes from the joint attention component
    /// (LeadVisualAttentionByPairOut), optionally.
    ///
    /// Replaces the nine UpdateConfidenceOnCollaborationProfiles_MultipleUsers and the three
    /// CollaborationProfileMerging of the legacy script: one instance per window, for any
    /// number of participants.
    /// </summary>
    public class CollaborationProfilesComponent : IProducer<CollaborationProfiles>
    {
        private const string PairHeader = "utc_timestamp_ms;session_id;puzzle_id;pair_id;profile_name;profile_normalized_value;profile_value;condition_validated;speech_equality_index;speech_equality_range;speech_equality_bool;ttov_index;ttov_range;ttov_bool;jva_index;jva_index_range;jva_bool;task_equality_index;task_equality_range;task_equality_bool;verbal_participation_index;verbal_participation_range;verbal_participation_bool;formation_index;formation_range;formation_bool;synch_score_index;synch_score_range;synch_score_bool;gazeonpeers_index";

        private readonly CollaborationProfilesConfiguration configuration;
        private readonly List<ParticipantPair> pairs;
        private Dictionary<ParticipantPair, double> leadVisualAttention = new Dictionary<ParticipantPair, double>();
        private int phaseId;
        private GroupProfile? currentGroup;
        private DateTime currentGroupSince;

        public CollaborationProfilesComponent(Pipeline pipeline, CollaborationProfilesConfiguration configuration, string name = nameof(CollaborationProfilesComponent))
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.pairs = Combinatorics.Pairs(configuration.ParticipantIds).ToList();

            this.SnapshotIn = pipeline.CreateReceiver<IndexSnapshot>(this, this.ReceiveSnapshot, $"{name}-Snapshot");
            this.LeadVisualAttentionByPairIn = pipeline.CreateReceiver<Dictionary<ParticipantPair, double>>(
                this, (lead, _) => this.leadVisualAttention = new Dictionary<ParticipantPair, double>(lead), $"{name}-LeadVisualAttention");
            this.PhaseIdIn = pipeline.CreateReceiver<int>(this, (id, _) => this.phaseId = id, $"{name}-PhaseId");

            this.Out = pipeline.CreateEmitter<CollaborationProfiles>(this, $"{name}-Profiles");
            this.GroupProfileOut = pipeline.CreateEmitter<GroupProfile>(this, $"{name}-GroupProfile");

            this.configuration.PairWriter?.WriteLine(PairHeader);
            this.configuration.GroupWriter?.WriteLine("utc_timestamp_ms;session_id;puzzle_id;" + string.Join(";", this.pairs.Select(p => $"profile_{p}")) + ";profile_Group");
            this.configuration.DurationWriter?.WriteLine("utc_timestamp_ms;session_id;puzzle_id;group_creation_time_ms;group_end_time_ms;group_duration_seconds;profile_Group");
        }

        /// <summary>The snapshot of the indices of one tick (indices.SnapshotOut).</summary>
        public Receiver<IndexSnapshot> SnapshotIn { get; }

        /// <summary>Who leads the joint attention inside each pair (JointVisualAttentionComponent.LeadVisualAttentionByPairOut). Optional.</summary>
        public Receiver<Dictionary<ParticipantPair, double>> LeadVisualAttentionByPairIn { get; }

        /// <summary>Identifier of the current phase or puzzle, written in the files. Optional.</summary>
        public Receiver<int> PhaseIdIn { get; }

        /// <summary>The profile of each pair and of the group, at each tick.</summary>
        public Emitter<CollaborationProfiles> Out { get; }

        /// <summary>The profile of the group, at each tick.</summary>
        public Emitter<GroupProfile> GroupProfileOut { get; }

        /// <summary>The indices of a pair in a snapshot, as the rules read them.</summary>
        public static PairProfileInput InputOf(IndexSnapshot snapshot, ParticipantPair pair, IReadOnlyList<uint> participantIds, double leadVisualAttention = 0)
        {
            double Pair(string name)
                => snapshot.Pair.TryGetValue(name, out var values) && values.TryGetValue(pair, out double value) ? value : 0;
            double Individual(string name, uint id)
                => snapshot.Individual.TryGetValue(name, out var values) && values.TryGetValue(id, out double value) ? value : 0;
            double Looks(uint from, uint to)
                => snapshot.DirectedPair.TryGetValue(IndexNames.GazeOnPeers, out var values) && values.TryGetValue(new DirectedParticipantPair(from, to), out double value) ? value : 0;
            double Watched(uint id) => participantIds.Where(other => other != id).Sum(other => Looks(other, id));

            // Who of the two is looked at most by the group; 0 when level.
            double watchedA = Watched(pair.A), watchedB = Watched(pair.B);
            double watchedMost = watchedA > watchedB ? pair.A + 1 : (watchedB > watchedA ? pair.B + 1 : 0);

            return new PairProfileInput
            {
                SpeechInequality = Pair(IndexNames.SpeechEquality),
                VerbalParticipation = (Individual(IndexNames.VerbalParticipation, pair.A) + Individual(IndexNames.VerbalParticipation, pair.B)) / 2.0,
                TurnTakingWithOverlap = Pair(IndexNames.TurnTakingWithOverlap),
                JointVisualAttention = Pair(IndexNames.JointVisualAttention),
                TaskInequality = Pair(IndexNames.TaskEquality),
                Formation = Pair(IndexNames.Formation),
                Synchrony = Pair(IndexNames.Synchrony),
                GazeOnPeers = Looks(pair.A, pair.B) + Looks(pair.B, pair.A),
                TalkingMost = Pair(IndexNames.TalkingMost),
                TaskingMost = Pair(IndexNames.TaskingMost),
                WatchedMost = watchedMost,
                LeadVisualAttention = leadVisualAttention,
            };
        }

        private static string Number(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

        private static string Milliseconds(DateTime time)
            => (time.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture);

        private void ReceiveSnapshot(IndexSnapshot snapshot, Envelope envelope)
        {
            DateTime time = envelope.OriginatingTime;
            var profiles = new CollaborationProfiles { OriginatingTime = time };
            foreach (ParticipantPair pair in this.pairs)
            {
                this.leadVisualAttention.TryGetValue(pair, out double lead);
                PairProfileInput input = InputOf(snapshot, pair, this.configuration.ParticipantIds, lead);
                profiles.Pairs[pair] = CollaborationProfileRules.Evaluate(pair, input, this.configuration.Rules);
            }

            profiles.Group = CollaborationProfileRules.Merge(profiles.Pairs.Values.Select(p => p.Profile));

            this.Write(profiles, time);
            this.Out.Post(profiles, time);
            this.GroupProfileOut.Post(profiles.Group, time);
        }

        private void Write(CollaborationProfiles profiles, DateTime time)
        {
            string prefix = $"{Milliseconds(time)};{this.configuration.SessionId};{this.phaseId + 1}";

            if (this.configuration.PairWriter != null)
            {
                foreach (PairProfileResult pair in profiles.Pairs.Values)
                {
                    PairProfileInput input = pair.Input;
                    double[] values =
                    {
                        input.SpeechInequality, input.TurnTakingWithOverlap, input.JointVisualAttention, input.TaskInequality,
                        input.VerbalParticipation, input.Formation, input.Synchrony,
                    };
                    foreach (ProfileConfidence confidence in pair.Confidences)
                    {
                        var cells = new List<string>
                        {
                            prefix, pair.Pair.ToString(), confidence.Profile.ToString(), Number(confidence.Confidence), Number(confidence.Value), confidence.ConditionValidated.ToString(),
                        };
                        for (int i = 0; i < values.Length; i++)
                        {
                            cells.Add(Number(values[i]));
                            cells.Add(confidence.Ranges[i]);
                            cells.Add(confidence.Matches[i].ToString());
                        }

                        cells.Add(Number(pair.GazeOnPeersIndex));
                        this.configuration.PairWriter.WriteLine(string.Join(";", cells));
                    }
                }
            }

            this.configuration.GroupWriter?.WriteLine($"{prefix};{string.Join(";", this.pairs.Select(p => profiles.Pairs[p].Profile))};{profiles.Group}");

            // The group keeps a profile for a while: one row when it changes, with how long it lasted.
            if (this.currentGroup == null)
            {
                this.currentGroup = profiles.Group;
                this.currentGroupSince = time;
            }
            else if (this.currentGroup.Value != profiles.Group)
            {
                this.configuration.DurationWriter?.WriteLine(
                    $"{prefix};{Milliseconds(this.currentGroupSince)};{Milliseconds(time)};{Number((time - this.currentGroupSince).TotalSeconds)};{this.currentGroup.Value}");
                this.currentGroup = profiles.Group;
                this.currentGroupSince = time;
            }
        }
    }
}
