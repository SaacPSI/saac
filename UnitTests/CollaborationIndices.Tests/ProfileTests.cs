// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Psi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.CollaborationIndices;
using SAAC.PsiFormats;

namespace CollaborationIndices.Tests
{
    /// <summary>The collaboration profiles of the pairs and of the group, from the indices.</summary>
    [TestClass]
    public class ProfileTests
    {
        private static readonly ParticipantPair Pair01 = new ParticipantPair(0, 1);

        // One participant does everything and nobody speaks.
        private static PairProfileInput EverythingNothing => new PairProfileInput
        {
            VerbalParticipation = 0.05, SpeechInequality = 0.3, TurnTakingWithOverlap = 0, JointVisualAttention = 0, TaskInequality = 0.9, Formation = 0, Synchrony = 0.2,
        };

        // One participant speaks and acts more, and leads the attention.
        private static PairProfileInput LeaderFollower => new PairProfileInput
        {
            VerbalParticipation = 0.4, SpeechInequality = 0.6, TurnTakingWithOverlap = 0.1, JointVisualAttention = 0.4, TaskInequality = 0.6, Formation = 0.5, Synchrony = 0.6,
            TalkingMost = 1, TaskingMost = 1, LeadVisualAttention = 1, WatchedMost = 1,
        };

        // Both speak as much, take turns, and stay together.
        private static PairProfileInput TurnTakers => new PairProfileInput
        {
            VerbalParticipation = 0.4, SpeechInequality = 0.1, TurnTakingWithOverlap = 0.6, JointVisualAttention = 0.4, TaskInequality = 0.4, Formation = 0.9, Synchrony = 0.9,
        };

        [TestMethod]
        public void PairProfile_IsTheBestFittingOne()
        {
            PairProfileResult everything = CollaborationProfileRules.Evaluate(Pair01, EverythingNothing);
            Assert.AreEqual(CollaborativeProfile.EverythingNothing, everything.Profile);
            Assert.AreEqual(1.0, Confidence(everything, CollaborativeProfile.EverythingNothing), 1e-9, "The seven criteria are met.");
            Assert.AreEqual(6.0 / 8.0, Confidence(everything, CollaborativeProfile.IndependentSolitary), 1e-9, "Five criteria, and the gaze as an eighth one.");

            PairProfileResult leader = CollaborationProfileRules.Evaluate(Pair01, LeaderFollower);
            Assert.AreEqual(CollaborativeProfile.LeaderFollower, leader.Profile);
            Assert.AreEqual(7.15 / 7.0, Confidence(leader, CollaborativeProfile.LeaderFollower), 1e-9, "Seven criteria and three bonuses of 0.05.");
            Assert.AreEqual(0.0, Confidence(leader, CollaborativeProfile.EverythingNothing), 1e-9, "The condition of the profile is not met: the pair speaks.");

            PairProfileResult turnTakers = CollaborationProfileRules.Evaluate(Pair01, TurnTakers);
            Assert.AreEqual(CollaborativeProfile.TurnTakersAccurate, turnTakers.Profile);
            Assert.AreEqual(7.05 / 7.0, Confidence(turnTakers, CollaborativeProfile.TurnTakersAccurate), 1e-9);
        }

        [TestMethod]
        public void PairProfile_IsNoneBelowTheMinimumConfidence()
        {
            // In the ranges of no profile in particular.
            var input = new PairProfileInput
            {
                VerbalParticipation = 0.9, SpeechInequality = 0.9, TurnTakingWithOverlap = 0.9, JointVisualAttention = 0.9, TaskInequality = 0.9, Formation = 0.1, Synchrony = 0.1,
            };

            PairProfileResult result = CollaborationProfileRules.Evaluate(Pair01, input);
            Assert.AreEqual(CollaborativeProfile.None, result.Profile);
            Assert.IsTrue(result.Confidences.All(c => c.Confidence < 0.5));
            Assert.AreEqual(7, result.Confidences.Count);
        }

        [TestMethod]
        public void SpeechEquality_MeansNothingWhenThePairHardlySpeaks()
        {
            var input = TurnTakers;
            Assert.AreEqual("0-25", CollaborationProfileRules.RangeOf(0, input.SpeechInequality, hardlySpeaks: false));
            Assert.AreEqual("null", CollaborationProfileRules.RangeOf(0, input.SpeechInequality, hardlySpeaks: true));
            Assert.AreEqual("25-75", CollaborationProfileRules.RangeOf(5, 0.6, false), "The formations have three ranges.");
            Assert.AreEqual("50-75", CollaborationProfileRules.RangeOf(6, 0.6, false));

            input.VerbalParticipation = 0.15;
            PairProfileResult result = CollaborationProfileRules.Evaluate(Pair01, input);
            Assert.IsFalse(result.Confidences.Single(c => c.Profile == CollaborativeProfile.TurnTakersAccurate).Matches[0], "At 0.15 of verbal participation the equality is not read.");
        }

        [TestMethod]
        public void LegacyDefects_CanBeReproduced()
        {
            var legacy = new CollaborationProfileRulesConfiguration { ReproduceLegacyDefects = true };

            Assert.AreEqual(CollaborativeProfile.IndependentSociable, CollaborationProfileRules.Evaluate(Pair01, LeaderFollower, legacy).Profile, "The label before the best in the enumeration.");
            Assert.AreEqual(CollaborativeProfile.None, CollaborationProfileRules.Evaluate(Pair01, EverythingNothing, legacy).Profile);

            PairProfileResult turnTakers = CollaborationProfileRules.Evaluate(Pair01, TurnTakers, legacy);
            Assert.AreEqual(0.0, Confidence(turnTakers, CollaborativeProfile.TurnTakersAccurate), 1e-9, "The turn takers profiles were never evaluated.");
            Assert.AreEqual(0.0, Confidence(turnTakers, CollaborativeProfile.TurnTakersNonAccurate), 1e-9);
        }

        [TestMethod]
        public void GroupProfile_MergesThePairs()
        {
            const CollaborativeProfile none = CollaborativeProfile.None;
            const CollaborativeProfile solitary = CollaborativeProfile.IndependentSolitary;
            const CollaborativeProfile leader = CollaborativeProfile.LeaderFollower;
            const CollaborativeProfile teacher = CollaborativeProfile.TeacherStudent;
            const CollaborativeProfile turns = CollaborativeProfile.TurnTakersAccurate;

            Assert.AreEqual(GroupProfile.Undetermined, CollaborationProfileRules.Merge(new[] { none, none, none }));
            Assert.AreEqual(GroupProfile.Individual, CollaborationProfileRules.Merge(new[] { solitary, none, none }));
            Assert.AreEqual(GroupProfile.Individual, CollaborationProfileRules.Merge(new[] { solitary, CollaborativeProfile.EverythingNothing, CollaborativeProfile.IndependentSociable }));
            Assert.AreEqual(GroupProfile.Hierarchical, CollaborationProfileRules.Merge(new[] { leader, solitary, none }), "One hierarchical pair and no balanced one.");
            Assert.AreEqual(GroupProfile.Hierarchical, CollaborationProfileRules.Merge(new[] { leader, teacher, turns }));
            Assert.AreEqual(GroupProfile.Balanced, CollaborationProfileRules.Merge(new[] { turns, solitary, none }));
            Assert.AreEqual(GroupProfile.Balanced, CollaborationProfileRules.Merge(new[] { turns, CollaborativeProfile.TurnTakersNonAccurate, leader }));
            Assert.AreEqual(GroupProfile.Mixed, CollaborationProfileRules.Merge(new[] { turns, leader, solitary }));
        }

        [TestMethod]
        public void Component_ReadsTheSnapshotAndWritesTheFiles()
        {
            var participants = new List<uint> { 0, 1, 2 };
            var published = new List<CollaborationProfiles>();
            var pairWriter = new StringWriter();
            var groupWriter = new StringWriter();
            var durationWriter = new StringWriter();

            // Pair 0-1 leads and follows for two ticks, then nothing fits any more; the other pairs have nothing.
            IndexSnapshot leading = Snapshot(participants, Pair01, LeaderFollower);
            IndexSnapshot nothing = Snapshot(participants, Pair01, new PairProfileInput());
            nothing.Individual[IndexNames.VerbalParticipation][0] = 0.9;
            nothing.Individual[IndexNames.VerbalParticipation][1] = 0.9;
            nothing.Pair[IndexNames.SpeechEquality][Pair01] = 0.9;
            nothing.Pair[IndexNames.TurnTakingWithOverlap][Pair01] = 0.9;
            nothing.Pair[IndexNames.JointVisualAttention][Pair01] = 0.9;
            nothing.Pair[IndexNames.TaskEquality][Pair01] = 0.9;

            IndicesHarness.Run(1, pipeline =>
            {
                // One source for both inputs: two generators would race, and the lead could
                // reach the component after the first snapshot.
                var steps = Generators.Sequence(pipeline, new[] { (0, SyntheticData.At(20)), (1, SyntheticData.At(21)), (2, SyntheticData.At(22)), (3, SyntheticData.At(23)) });
                var lead = steps.Where(step => step == 0).Select(_ => new Dictionary<ParticipantPair, double> { { Pair01, 1.0 } });
                var snapshots = steps.Where(step => step > 0).Select(step => step < 3 ? leading : nothing);

                var component = new CollaborationProfilesComponent(pipeline, new CollaborationProfilesConfiguration
                {
                    ParticipantIds = participants,
                    SessionId = "S",
                    PairWriter = pairWriter,
                    GroupWriter = groupWriter,
                    DurationWriter = durationWriter,
                });
                snapshots.PipeTo(component.SnapshotIn);
                lead.PipeTo(component.LeadVisualAttentionByPairIn);
                component.Out.Do(p => published.Add(p.DeepClone()));
            });

            Assert.AreEqual(3, published.Count);
            Assert.AreEqual(CollaborativeProfile.LeaderFollower, published[0].Pairs[Pair01].Profile);
            Assert.AreEqual(7.15 / 7.0, Confidence(published[0].Pairs[Pair01], CollaborativeProfile.LeaderFollower), 1e-9, "Speaking, acting, watched and leading: the same participant.");
            Assert.AreEqual(GroupProfile.Hierarchical, published[0].Group);
            Assert.AreEqual(CollaborativeProfile.None, published[2].Pairs[Pair01].Profile);

            string[] groupRows = Lines(groupWriter);
            Assert.AreEqual("utc_timestamp_ms;session_id;puzzle_id;profile_0-1;profile_0-2;profile_1-2;profile_Group", groupRows[0]);
            Assert.AreEqual(4, groupRows.Length);
            StringAssert.EndsWith(groupRows[1], ";S;1;LeaderFollower;IndependentSolitary;IndependentSolitary;Hierarchical");

            Assert.AreEqual(1 + (3 * 3 * 7), Lines(pairWriter).Length, "One row per tick, pair and profile.");
            string[] durations = Lines(durationWriter);
            Assert.AreEqual(2, durations.Length, "The group changed profile once.");
            StringAssert.EndsWith(durations[1], ";2;Hierarchical", "Hierarchical for two seconds.");
        }

        private static double Confidence(PairProfileResult result, CollaborativeProfile profile)
            => result.Confidences.Single(c => c.Profile == profile).Confidence;

        private static string[] Lines(StringWriter writer)
            => writer.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>A snapshot where one pair has the given indices and the others nothing.</summary>
        private static IndexSnapshot Snapshot(List<uint> participants, ParticipantPair pair, PairProfileInput input)
        {
            var snapshot = new IndexSnapshot();
            void SetPair(string name, double value) => snapshot.Pair[name] = Combinatorics.Pairs(participants).ToDictionary(p => p, p => p.Equals(pair) ? value : 0.0);

            SetPair(IndexNames.SpeechEquality, input.SpeechInequality);
            SetPair(IndexNames.TurnTakingWithOverlap, input.TurnTakingWithOverlap);
            SetPair(IndexNames.JointVisualAttention, input.JointVisualAttention);
            SetPair(IndexNames.TaskEquality, input.TaskInequality);
            SetPair(IndexNames.Formation, input.Formation);
            SetPair(IndexNames.Synchrony, input.Synchrony);
            SetPair(IndexNames.TalkingMost, input.TalkingMost);
            SetPair(IndexNames.TaskingMost, input.TaskingMost);
            snapshot.Individual[IndexNames.VerbalParticipation] = participants.ToDictionary(id => id, id => pair.Contains(id) ? input.VerbalParticipation : 0.0);

            // The group looks at participant 0 only when the pair has somebody watched.
            snapshot.DirectedPair[IndexNames.GazeOnPeers] = new Dictionary<DirectedParticipantPair, double>
            {
                { new DirectedParticipantPair(1, 0), input.WatchedMost > 0 ? 1 : 0 },
            };
            return snapshot;
        }
    }
}
