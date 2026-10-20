using System;
using System.Collections.Generic;
using System.Linq;
using SAAC.PsiFormats;

namespace SAAC.CollaborationIndices
{
    /// <summary>Profile of a group, from the profiles of its pairs.</summary>
    public enum GroupProfile
    {
        /// <summary>No pair has a profile.</summary>
        Undetermined = 0,

        /// <summary>The pairs work side by side: independent or one doing everything.</summary>
        Individual = 1,

        /// <summary>The pairs are led: leader and follower, teacher and student.</summary>
        Hierarchical = 2,

        /// <summary>The pairs take turns.</summary>
        Balanced = 3,

        /// <summary>Balanced and hierarchical pairs in equal number.</summary>
        Mixed = 4,
    }

    /// <summary>The indices of one pair over one window, as the profile rules read them.</summary>
    public class PairProfileInput
    {
        /// <summary>Inequality of the speaking times of the pair: 0 equal, 1 one speaks alone.</summary>
        public double SpeechInequality { get; set; }

        /// <summary>Share of the window the two participants spend speaking (mean of their ratios).</summary>
        public double VerbalParticipation { get; set; }

        /// <summary>Normalised number of turn takings with overlap of the pair.</summary>
        public double TurnTakingWithOverlap { get; set; }

        /// <summary>Normalised number of joint visual attention episodes of the pair.</summary>
        public double JointVisualAttention { get; set; }

        /// <summary>Inequality of the task participations of the pair: 0 equal, 1 one acts alone.</summary>
        public double TaskInequality { get; set; }

        /// <summary>Normalised number of formations of the pair.</summary>
        public double Formation { get; set; }

        /// <summary>Synchrony of the pair, 0 to 1.</summary>
        public double Synchrony { get; set; }

        /// <summary>Number of looks of each at the other.</summary>
        public double GazeOnPeers { get; set; }

        /// <summary>Who of the two speaks most, acts most, is watched most, leads the joint attention: id + 1, 0 for none.</summary>
        public double TalkingMost { get; set; }

        public double TaskingMost { get; set; }

        public double WatchedMost { get; set; }

        public double LeadVisualAttention { get; set; }
    }

    /// <summary>How well one profile fits a pair.</summary>
    public class ProfileConfidence
    {
        public CollaborativeProfile Profile { get; set; }

        /// <summary>Number of criteria met, plus the bonuses; 0 when the condition of the profile is not met.</summary>
        public double Value { get; set; }

        /// <summary>Value divided by the number of criteria: the confidence, 0 to about 1.</summary>
        public double Confidence { get; set; }

        public bool ConditionValidated { get; set; }

        /// <summary>The range each criterion must be in, in the order of <see cref="CollaborationProfileRules.Criteria"/>.</summary>
        public string[] Ranges { get; set; } = new string[0];

        /// <summary>Whether each criterion is in its range.</summary>
        public bool[] Matches { get; set; } = new bool[0];
    }

    /// <summary>Profile of one pair at one tick.</summary>
    public class PairProfileResult
    {
        public ParticipantPair Pair { get; set; }

        /// <summary>The profile with the highest confidence, or None when none reaches the minimum.</summary>
        public CollaborativeProfile Profile { get; set; }

        public List<ProfileConfidence> Confidences { get; set; } = new List<ProfileConfidence>();

        public PairProfileInput Input { get; set; } = new PairProfileInput();

        /// <summary>Looks of each at the other, divided by the reference.</summary>
        public double GazeOnPeersIndex { get; set; }
    }

    public class CollaborationProfileRulesConfiguration
    {
        /// <summary>At or below this verbal participation the pair hardly speaks: its speech equality means nothing.</summary>
        public double MinimumVerbalParticipation { get; set; } = 0.15;

        /// <summary>Number of looks of a pair at each other that counts as 1: 8, 9 and 10 for windows of 20, 30 and 45 s.</summary>
        public double GazeOnPeersReference { get; set; } = 8;

        /// <summary>Gaze index from which a pair that does not speak is sociable rather than solitary.</summary>
        public double SociableGazeThreshold { get; set; } = 0.125;

        /// <summary>A pair has a profile when its best confidence reaches this.</summary>
        public double MinimumConfidence { get; set; } = 0.5;

        /// <summary>What each supporting clue adds to the value of a profile.</summary>
        public double Bonus { get; set; } = 0.05;

        /// <summary>
        /// False (default) applies the rules as they are written. True reproduces the two
        /// defects of the legacy UpdateConfidenceOnCollaborationProfiles_MultipleUsers, to
        /// compare with files it produced:
        ///  - the two turn takers profiles always had a confidence of 0 (their case of the
        ///    switch was written with other names than the emitters it was compared with);
        ///  - the profile published was the one before the best in the enumeration (the index
        ///    of the best confidence was cast to the enumeration, which starts with None and
        ///    lists the profiles in another order): leader and follower came out as
        ///    independent sociable, everything and nothing as none.
        /// </summary>
        public bool ReproduceLegacyDefects { get; set; } = false;

        /// <summary>The gaze reference of a window.</summary>
        public static double GazeOnPeersReferenceFor(TimeSpan window)
            => window.TotalSeconds < 25 ? 8 : (window.TotalSeconds < 35 ? 9 : 10);
    }

    /// <summary>
    /// The collaboration profiles of a pair, and of a group from its pairs.
    ///
    /// Seven criteria are read for a pair, each one placed in a range: speech equality
    /// ("null" when the pair hardly speaks), turn takings with overlap, joint visual
    /// attention, task equality, verbal participation, formations and synchrony. A profile
    /// is a range expected for each criterion and a condition; when the condition holds its
    /// value is the number of criteria in their expected range, plus bonuses, and its
    /// confidence that value divided by the number of criteria. The profile of the pair is
    /// the one with the highest confidence, provided it reaches the minimum.
    ///
    /// Ported from UpdateConfidenceOnCollaborationProfiles_MultipleUsers and
    /// CollaborationProfileMerging; see CollaborationProfileRulesConfiguration.ReproduceLegacyDefects
    /// for what was not kept.
    /// </summary>
    public static class CollaborationProfileRules
    {
        /// <summary>The criteria, in the order of ProfileConfidence.Ranges and Matches.</summary>
        public static readonly string[] Criteria =
        {
            "SpeechEquality", "TurnTakingWithOverlap", "JointVisualAttention", "TaskEquality", "VerbalParticipation", "Formation", "Synchrony",
        };

        // The profiles in the order of the legacy confidence array, with the range expected for each criterion.
        private static readonly (CollaborativeProfile Profile, string[] Ranges)[] Profiles =
        {
            (CollaborativeProfile.EverythingNothing, new[] { "null", "0-25", "0-25", "50-100", "0-25", "0-25", "0-25" }),
            (CollaborativeProfile.IndependentSolitary, new[] { "null", "0-25", "0-25", "0-25", "0-25", "0-25", "25-50" }),
            (CollaborativeProfile.IndependentSociable, new[] { "null", "0-25", "25-50", "0-25", "0-25", "25-75", "25-50" }),
            (CollaborativeProfile.LeaderFollower, new[] { "50-75", "0-25", "25-50", "50-75", "25-50", "25-75", "50-75" }),
            (CollaborativeProfile.TeacherStudent, new[] { "25-50", "25-50", "50-75", "25-50", "25-50", "25-75", "50-75" }),
            (CollaborativeProfile.TurnTakersAccurate, new[] { "0-25", "50-75", "25-50", "25-50", "25-50", "75-100", "75-100" }),
            (CollaborativeProfile.TurnTakersNonAccurate, new[] { "0-25", "50-75", "50-75", "0-25", "25-50", "75-100", "75-100" }),
        };

        /// <summary>The profile of a pair from its indices.</summary>
        public static PairProfileResult Evaluate(ParticipantPair pair, PairProfileInput input, CollaborationProfileRulesConfiguration configuration = null)
        {
            configuration = configuration ?? new CollaborationProfileRulesConfiguration();
            bool hardlySpeaks = input.VerbalParticipation <= configuration.MinimumVerbalParticipation;
            double[] values =
            {
                input.SpeechInequality, input.TurnTakingWithOverlap, input.JointVisualAttention, input.TaskInequality,
                input.VerbalParticipation, input.Formation, input.Synchrony,
            };
            double gaze = configuration.GazeOnPeersReference > 0 ? input.GazeOnPeers / configuration.GazeOnPeersReference : 0;

            var result = new PairProfileResult { Pair = pair, Input = input, GazeOnPeersIndex = gaze };
            foreach (var profile in Profiles)
            {
                var matches = new bool[Criteria.Length];
                for (int i = 0; i < Criteria.Length; i++)
                {
                    matches[i] = IsInRange(i, values[i], profile.Ranges[i], hardlySpeaks);
                }

                bool speech = matches[0], turnTaking = matches[1], attention = matches[2], task = matches[3], verbal = matches[4];
                int criteria = matches.Count(m => m);
                double value = 0;
                double divisor = Criteria.Length;
                bool validated = false;

                switch (profile.Profile)
                {
                    case CollaborativeProfile.EverythingNothing:
                        validated = speech && task;
                        value = validated ? criteria : 0;
                        break;

                    case CollaborativeProfile.IndependentSolitary:
                    case CollaborativeProfile.IndependentSociable:
                        // The gaze tells the two apart, and counts as an eighth criterion when it agrees.
                        bool sociable = profile.Profile == CollaborativeProfile.IndependentSociable;
                        bool gazeAgrees = sociable ? gaze >= configuration.SociableGazeThreshold : gaze < configuration.SociableGazeThreshold;
                        validated = verbal && (attention || gazeAgrees);
                        value = validated ? criteria : 0;
                        if (validated && gazeAgrees)
                        {
                            value += 1;
                            divisor += 1;
                        }

                        break;

                    case CollaborativeProfile.LeaderFollower:
                        validated = speech || task;
                        value = validated ? criteria : 0;
                        if (validated)
                        {
                            // The one who speaks most also acts most, leads the attention, and the one watched most leads it.
                            value += input.TalkingMost != 0 && input.TalkingMost == input.TaskingMost ? configuration.Bonus : 0;
                            value += input.TalkingMost != 0 && input.TalkingMost == input.LeadVisualAttention ? configuration.Bonus : 0;
                            value += input.WatchedMost != 0 && input.WatchedMost == input.LeadVisualAttention ? configuration.Bonus : 0;
                        }

                        break;

                    case CollaborativeProfile.TeacherStudent:
                        validated = speech || task;
                        value = validated ? criteria : 0;
                        if (validated)
                        {
                            // The one who speaks most is not the one who acts most, and leads the attention.
                            value += input.TalkingMost != 0 && input.TalkingMost != input.TaskingMost ? configuration.Bonus : 0;
                            value += input.TalkingMost != 0 && input.TalkingMost == input.LeadVisualAttention ? configuration.Bonus : 0;
                        }

                        break;

                    case CollaborativeProfile.TurnTakersAccurate:
                    case CollaborativeProfile.TurnTakersNonAccurate:
                        if (!configuration.ReproduceLegacyDefects)
                        {
                            validated = speech || task;
                            value = validated ? criteria : 0;
                            value += validated && turnTaking ? configuration.Bonus : 0;
                        }

                        break;
                }

                result.Confidences.Add(new ProfileConfidence
                {
                    Profile = profile.Profile,
                    Value = value,
                    Confidence = value / divisor,
                    ConditionValidated = validated,
                    Ranges = profile.Ranges,
                    Matches = matches,
                });
            }

            // The first of the best, in the order of the list.
            int best = 0;
            for (int i = 1; i < result.Confidences.Count; i++)
            {
                if (result.Confidences[i].Confidence > result.Confidences[best].Confidence)
                {
                    best = i;
                }
            }

            if (result.Confidences[best].Confidence < configuration.MinimumConfidence)
            {
                result.Profile = CollaborativeProfile.None;
            }
            else
            {
                result.Profile = configuration.ReproduceLegacyDefects ? (CollaborativeProfile)best : result.Confidences[best].Profile;
            }

            return result;
        }

        /// <summary>Family of a pair profile.</summary>
        public static GroupProfile FamilyOf(CollaborativeProfile profile)
        {
            switch (profile)
            {
                case CollaborativeProfile.TurnTakersAccurate:
                case CollaborativeProfile.TurnTakersNonAccurate:
                    return GroupProfile.Balanced;
                case CollaborativeProfile.LeaderFollower:
                case CollaborativeProfile.TeacherStudent:
                    return GroupProfile.Hierarchical;
                case CollaborativeProfile.IndependentSociable:
                case CollaborativeProfile.IndependentSolitary:
                case CollaborativeProfile.EverythingNothing:
                    return GroupProfile.Individual;
                default:
                    return GroupProfile.Undetermined;
            }
        }

        /// <summary>
        /// Profile of the group from the profiles of its pairs: undetermined when no pair has
        /// one; individual when no pair is hierarchical or balanced; hierarchical with two
        /// hierarchical pairs, or one and no balanced pair; balanced with two balanced pairs,
        /// or one and no hierarchical pair; mixed with one of each.
        /// </summary>
        public static GroupProfile Merge(IEnumerable<CollaborativeProfile> pairProfiles)
        {
            List<GroupProfile> families = pairProfiles.Select(FamilyOf).ToList();
            int individual = families.Count(f => f == GroupProfile.Individual);
            int hierarchical = families.Count(f => f == GroupProfile.Hierarchical);
            int balanced = families.Count(f => f == GroupProfile.Balanced);

            if (individual + hierarchical + balanced == 0)
            {
                return GroupProfile.Undetermined;
            }

            if (hierarchical == 0 && balanced == 0)
            {
                return GroupProfile.Individual;
            }

            if (hierarchical >= 2 || (hierarchical == 1 && balanced == 0))
            {
                return GroupProfile.Hierarchical;
            }

            if (balanced >= 2 || (balanced == 1 && hierarchical == 0))
            {
                return GroupProfile.Balanced;
            }

            return GroupProfile.Mixed;
        }

        /// <summary>Range of a value: quarters, except for the formations (0-25, 25-75, 75-100).</summary>
        public static string RangeOf(int criterion, double value, bool hardlySpeaks)
        {
            if (criterion == 0 && hardlySpeaks)
            {
                return "null";
            }

            if (value <= 0.25)
            {
                return "0-25";
            }

            if (criterion == 5)
            {
                return value <= 0.75 ? "25-75" : "75-100";
            }

            return value <= 0.5 ? "25-50" : (value <= 0.75 ? "50-75" : "75-100");
        }

        private static bool IsInRange(int criterion, double value, string expected, bool hardlySpeaks)
        {
            // Task equality of at least one half, whatever its quarter.
            if (expected == "50-100")
            {
                return value >= 0.5;
            }

            return RangeOf(criterion, value, hardlySpeaks) == expected;
        }
    }
}
