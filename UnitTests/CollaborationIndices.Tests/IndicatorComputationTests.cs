// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Psi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.CollaborationIndices;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// Indices computed on windows whose content is known, so that the expected value can be
    /// worked out by hand.
    /// </summary>
    [TestClass]
    public class IndicatorComputationTests
    {
        private static readonly IndexCalibration NoCalibration = new IndexCalibration();

        [TestMethod]
        public void VerbalParticipation_IsTheSpeechOfTheLastWindow()
        {
            const double Window = 10.0;
            var recorder = new StreamRecorder();

            IndicesHarness.Run(1, pipeline =>
            {
                var player = new SessionPlayer(pipeline, IndicesHarness.Second);
                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(SyntheticData.Participants)
                    .WithWindow(TimeSpan.FromSeconds(Window))
                    .AddIndicator(Indicators.VerbalParticipation)
                    .Build();

                IndicesHarness.Connect(player, indices);
                IndicesHarness.Record(recorder, indices);
            });

            IReadOnlyList<StreamRecorder.Sample> speakingTimes = recorder.Get("Verbal.SpeakingTimes");
            IReadOnlyList<StreamRecorder.Sample> ratios = recorder.Get("Verbal.Individual");
            IReadOnlyList<StreamRecorder.Sample> group = recorder.Get("Verbal.Group");

            // One window of warm-up, then one value per second until the end of the minute.
            Assert.AreEqual(50, speakingTimes.Count);
            Assert.AreEqual(SyntheticData.At(10.02), speakingTimes[0].Time);
            Assert.AreEqual(SyntheticData.At(59.02), speakingTimes[49].Time);

            for (int i = 0; i < speakingTimes.Count; i++)
            {
                double now = (speakingTimes[i].Time - SyntheticData.Start).TotalSeconds;
                Dictionary<string, double> seconds = StreamRecorder.ParseDictionary(speakingTimes[i].Payload);
                Dictionary<string, double> ratio = StreamRecorder.ParseDictionary(ratios[i].Payload);

                double total = 0;
                foreach (uint id in SyntheticData.Participants)
                {
                    double expected = SyntheticData.ExpectedSpeakingSeconds(id, now - Window, now);
                    Assert.AreEqual(expected, seconds[id.ToString()], 1e-6, $"Speaking time of participant {id} at {now:0.00} s");
                    Assert.AreEqual(expected / Window, ratio[id.ToString()], 1e-6, $"Participation of participant {id} at {now:0.00} s");
                    total += expected;
                }

                Assert.AreEqual(total / (Window * 3), double.Parse(group[i].Payload, System.Globalization.CultureInfo.InvariantCulture), 1e-6, $"Group participation at {now:0.00} s");
            }

            // A value worked out by hand: at 20.02 s the window is [10.02, 20.02]. Participant 0
            // spoke from 14.0 to 20.3, not finished yet so not published; participant 1 from
            // 10.0 to 13.5, of which 3.48 s fall inside the window.
            Dictionary<string, double> atTwenty = StreamRecorder.ParseDictionary(speakingTimes[10].Payload);
            Assert.AreEqual(SyntheticData.At(20.02), speakingTimes[10].Time);
            Assert.AreEqual(0.0, atTwenty["0"], 1e-6);
            Assert.AreEqual(3.48, atTwenty["1"], 1e-6);
            Assert.AreEqual(0.0, atTwenty["2"], 1e-6);
        }

        [TestMethod]
        public void Movement_OfAConstantSpeed_IsThatSpeed()
        {
            var recorder = new StreamRecorder();

            IndicesHarness.Run(1, pipeline =>
            {
                // Participant 0 walks at 0.5 m/s, participant 1 at 0.2 m/s, sampled at 10 Hz.
                var timeline = new Timeline().Ticks("Clock", 0.05, 12, 1.0);
                for (int i = 0; i < 120; i++)
                {
                    double t = i * 0.1;
                    timeline.Position("Head0", t, new Vector3((float)(0.5 * t), 0, 0));
                    timeline.Position("Head1", t + 0.001, new Vector3(0, 0, (float)(0.2 * t)));
                }

                timeline.Play(pipeline);

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1)
                    .WithWindow(TimeSpan.FromSeconds(4))
                    .WithClock(timeline.TicksOf("Clock"))
                    .AddIndicator(Indicators.Movement, options => options.BodyParts = new List<string> { BodyPartNames.Head })
                    .Build();

                PhysicalActivityLevelComponent movement = indices.Get(Indicators.Movement).Component;
                timeline.PositionsOf("Head0").PipeTo(movement.GetPositionInput(0, BodyPartNames.Head));
                timeline.PositionsOf("Head1").PipeTo(movement.GetPositionInput(1, BodyPartNames.Head));

                IndicesHarness.Record(recorder, indices);
            });

            IReadOnlyList<StreamRecorder.Sample> levels = recorder.Get("Movement.Individual");
            Assert.AreEqual(8, levels.Count, "Ticks at 4.05 s to 11.05 s, after 4 s of warm-up.");

            foreach (StreamRecorder.Sample sample in levels)
            {
                Dictionary<string, double> level = StreamRecorder.ParseDictionary(sample.Payload);
                Assert.AreEqual(0.5, level["0"], 1e-4);
                Assert.AreEqual(0.2, level["1"], 1e-4);
            }

            foreach (StreamRecorder.Sample sample in recorder.Get("Movement.Group"))
            {
                Assert.AreEqual(0.35, double.Parse(sample.Payload, System.Globalization.CultureInfo.InvariantCulture), 1e-4, "The group level is the mean of the participants.");
            }
        }

        [TestMethod]
        public void Synchrony_IsOneForIdenticalMovements_AndZeroForOppositeOnes()
        {
            var recorder = new StreamRecorder();

            IndicesHarness.Run(1, pipeline =>
            {
                // Speeds 1 + 0.5 sin(t) for participants 0 and 1, 1 - 0.5 sin(t) for participant 2:
                // 0 and 1 accelerate together, 2 slows down whenever they speed up.
                var timeline = new Timeline().Ticks("Clock", 0.02, 15, 1.0);
                for (int i = 0; i < 300; i++)
                {
                    double t = i * 0.05;
                    float together = (float)(t - (0.5 * Math.Cos(t)));
                    float opposite = (float)(t + (0.5 * Math.Cos(t)));
                    timeline.Position("Head0", t, new Vector3(together, 0, 0));
                    timeline.Position("Head1", t + 0.001, new Vector3(0, together, 0));
                    timeline.Position("Head2", t + 0.002, new Vector3(0, 0, opposite));
                }

                timeline.Play(pipeline);

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1, 2)
                    .WithWindow(TimeSpan.FromSeconds(6))
                    .WithClock(timeline.TicksOf("Clock"))
                    .AddIndicator(Indicators.Synchrony)
                    .Build();

                PhysicalSynchronyComponent synchrony = indices.Get(Indicators.Synchrony).Component;
                foreach (uint id in new uint[] { 0, 1, 2 })
                {
                    timeline.PositionsOf($"Head{id}").PipeTo(synchrony.GetPositionInput(id, BodyPartNames.Head));
                }

                IndicesHarness.Record(recorder, indices);
            });

            IReadOnlyList<StreamRecorder.Sample> pairs = recorder.Get("Synchrony.Pair");
            Assert.IsTrue(pairs.Count >= 8, $"Expected a value per second after the warm-up, got {pairs.Count}.");

            foreach (StreamRecorder.Sample sample in pairs)
            {
                Dictionary<string, double> synchrony = StreamRecorder.ParseDictionary(sample.Payload);

                // Published as (r + 1) / 2: 1 in phase, 0 in opposition.
                Assert.AreEqual(1.0, synchrony["0-1"], 1e-3);
                Assert.AreEqual(0.0, synchrony["0-2"], 1e-3);
                Assert.AreEqual(0.0, synchrony["1-2"], 1e-3);
            }

            foreach (StreamRecorder.Sample sample in recorder.Get("Synchrony.Group"))
            {
                Assert.AreEqual(1.0 / 3.0, double.Parse(sample.Payload, System.Globalization.CultureInfo.InvariantCulture), 1e-3, "The group score is the mean of the pairs.");
            }
        }

        [TestMethod]
        public void TaskParticipation_CountsTheEventsOfTheSlidingWindow()
        {
            var counts = new List<KeyValuePair<double, Dictionary<string, double>>>();

            IndicesHarness.Run(1, pipeline =>
            {
                Timeline timeline = new Timeline()
                    .Ticks("Clock", 0.02, 10, 1.0)
                    .Event("Task", 1.5, IndexCategories.Place, 0)
                    .Event("Task", 2.5, IndexCategories.Place, 0)
                    .Event("Task", 3.5, IndexCategories.Place, 0)
                    .Event("Task", 3.6, IndexCategories.Place, 1)
                    .Event("Task", 7.5, IndexCategories.Place, 0)
                    .Play(pipeline);

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1)
                    .WithWindow(TimeSpan.FromSeconds(3))
                    .WithCalibration(NoCalibration)
                    .WithClock(timeline.TicksOf("Clock"))
                    .AddIndicator(Indicators.TaskParticipation)
                    .Build();

                TaskParticipationComponent task = indices.Get(Indicators.TaskParticipation).Component;
                timeline.EventsOf("Task").PipeTo(task.EventIn);

                task.RawIndividualOut.Do((values, envelope) => counts.Add(new KeyValuePair<double, Dictionary<string, double>>(
                    (envelope.OriginatingTime - SyntheticData.Start).TotalSeconds,
                    values.ToDictionary(v => v.Key.ToString(), v => v.Value))));
            });

            // Window of 3 s ending at each tick: [0.02, 3.02], [1.02, 4.02], ...
            var expectedForParticipant0 = new Dictionary<double, double>
            {
                { 3.02, 2 }, { 4.02, 3 }, { 5.02, 2 }, { 6.02, 1 }, { 7.02, 0 }, { 8.02, 1 }, { 9.02, 1 },
            };

            Assert.AreEqual(expectedForParticipant0.Count, counts.Count);
            foreach (var entry in counts)
            {
                double tick = Math.Round(entry.Key, 2);
                Assert.AreEqual(expectedForParticipant0[tick], entry.Value["0"], $"Participant 0 at {tick} s");
                Assert.AreEqual(tick >= 4 && tick <= 6.5 ? 1 : 0, entry.Value["1"], $"Participant 1 at {tick} s");
            }
        }

        [TestMethod]
        public void JointVisualAttention_CountsEachEpisodeOnce()
        {
            var group = new List<double>();
            var pairs = new List<Dictionary<string, double>>();

            IndicesHarness.Run(1, pipeline =>
            {
                Timeline timeline = new Timeline()
                    .Ticks("Clock", 0.02, 8, 1.0)
                    .Event("Jva", 1.0, IndexCategories.JointVisualAttention, 0, 1)
                    .Event("Jva", 2.0, IndexCategories.JointVisualAttention, 1, 0)
                    .Event("Jva", 3.0, IndexCategories.JointVisualAttention, 2, 0)
                    .Play(pipeline);

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1, 2)
                    .WithWindow(TimeSpan.FromSeconds(5))
                    .WithCalibration(NoCalibration)
                    .WithClock(timeline.TicksOf("Clock"))
                    .AddIndicator(Indicators.JointVisualAttention)
                    .Build();

                JointVisualAttentionComponent jva = indices.Get(Indicators.JointVisualAttention).Component;
                timeline.EventsOf("Jva").PipeTo(jva.EventIn);

                jva.GroupOut.Do(value => group.Add(value));
                jva.PairOut.Do(values => pairs.Add(values.ToDictionary(v => v.Key.ToString(), v => v.Value)));
            });

            // Ticks at 5.02, 6.02 and 7.02 s; the windows start at 0.02, 1.02 and 2.02 s.
            CollectionAssert.AreEqual(new[] { 3.0, 2.0, 1.0 }, group, "Three episodes, then the oldest ones leave the window.");
            Assert.AreEqual(2.0, pairs[0]["0-1"], "An episode counts for its pair whoever initiated it.");
            Assert.AreEqual(1.0, pairs[0]["0-2"]);
            Assert.AreEqual(0.0, pairs[0]["1-2"]);
        }

        [TestMethod]
        public void SpeechEquality_FollowsTheDistributionOfSpeech()
        {
            var gini = new List<double>();
            var leader = new List<double>();

            IndicesHarness.Run(1, pipeline =>
            {
                // First only participant 0 speaks, then all three speak just as long.
                Timeline timeline = new Timeline()
                    .Ticks("Clock", 0.02, 13, 1.0)
                    .Interval("Speech0", 1.0, 3.0, IndexCategories.Speaking, 0)
                    .Interval("Speech0", 7.1, 8.1, IndexCategories.Speaking, 0)
                    .Interval("Speech1", 8.2, 9.2, IndexCategories.Speaking, 1)
                    .Interval("Speech2", 9.3, 10.3, IndexCategories.Speaking, 2)
                    .Play(pipeline);

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1, 2)
                    .WithWindow(TimeSpan.FromSeconds(4))
                    .WithClock(timeline.TicksOf("Clock"))
                    .AddIndicator(Indicators.VerbalParticipation)
                    .AddIndicator(Indicators.SpeechEquality)
                    .AddIndicator(Indicators.TalkingMost)
                    .Build();

                VerbalParticipationComponent verbal = indices.Get(Indicators.VerbalParticipation).Component;
                foreach (uint id in new uint[] { 0, 1, 2 })
                {
                    timeline.IntervalsOf($"Speech{id}").PipeTo(verbal.GetIntervalInput(id));
                }

                indices.Get(Indicators.SpeechEquality).Component.Out.Do(value => gini.Add(value));
                indices.Get(Indicators.TalkingMost).Component.Out.Do(value => leader.Add(value));
            });

            // Ticks from 4.02 s to 12.02 s.
            Assert.AreEqual(9, gini.Count);
            Assert.AreEqual(1.0, gini[0], 1e-9, "At 4.02 s only participant 0 has spoken: fully unequal.");
            Assert.AreEqual(1.0, leader[0], "Participant 0, published as id + 1.");
            Assert.AreEqual(-1.0, gini[3], "At 7.02 s nobody has spoken in the window: the index is undefined.");
            Assert.AreEqual(0.0, leader[3], "Nobody stands out.");
            Assert.AreEqual(0.0, gini[7], 1e-9, "At 11.02 s the three participants have spoken one second each: perfectly equal.");
            Assert.AreEqual(0.0, leader[7], "A tie has no leader.");
        }

        [TestMethod]
        public void Gini_OfKnownDistributions()
        {
            Assert.AreEqual(0.0, GroupStatistics.GiniIndex(new[] { 2.0, 2.0, 2.0 }), 1e-12);
            Assert.AreEqual(1.0, GroupStatistics.GiniIndex(new[] { 0.0, 0.0, 6.0 }), 1e-12);
            Assert.AreEqual(0.5, GroupStatistics.GiniIndex(new[] { 1.0, 3.0 }), 1e-12, "Normalized by its maximum for two participants, (3 - 1) / (3 + 1).");
            Assert.AreEqual(0.0, GroupStatistics.GiniIndex(new[] { 0.0, 0.0 }), "No total, no inequality.");
        }

        [TestMethod]
        public void Normalizer_MapsItsReferenceToTheReferenceScore()
        {
            IIndexNormalizer normalizer = IndexCalibration.Threshold30Seconds().NormalizerFor(IndexNames.TaskParticipation);

            Assert.AreEqual(0.0, normalizer.Normalize(0), 1e-12);
            Assert.AreEqual(0.95, normalizer.Normalize(27), 1e-12, "27 task events is the P95 of the 30 s calibration.");
            Assert.IsInstanceOfType(new IndexCalibration().NormalizerFor(IndexNames.TaskParticipation), typeof(IdentityNormalizer));
        }
    }
}
