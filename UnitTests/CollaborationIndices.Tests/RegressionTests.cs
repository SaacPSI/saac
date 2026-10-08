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

// The constructors kept for compatibility are part of what is tested here.
#pragma warning disable CS0618

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// One test per defect found while the component was reviewed, so that none comes back.
    /// </summary>
    [TestClass]
    public class RegressionTests
    {
        [TestMethod]
        public void LegacyFlags_GraphWithoutVerbalIndices_NoLongerThrows()
        {
            // ConnectInternals used to dereference VerbalParticipation, SpeechEquality and the
            // gaze components for the graph whatever the flags: NullReferenceException.
            using (Pipeline pipeline = Pipeline.Create())
            {
                var configuration = new SlidingAverageConfiguration
                {
                    UseVerbalIndices = false,
                    UseVisualIndices = false,
                    UseTaskIndices = false,
                    UseSpatialIndices = false,
                    UsePhysicalIndices = true,
                    GenerateGraph = true,
                    ComputeCollaborationScores = true,
                };

                var indices = new SlidingAverageComputation(pipeline, null, configuration, "Indices");

                Assert.IsNotNull(indices.Graph);
                Assert.IsNotNull(indices.Out);
                Assert.IsNull(indices.VerbalParticipation);
            }
        }

        [TestMethod]
        public void LegacyFlags_IndicesWithoutScore_NoLongerThrow()
        {
            // The group indices used to be piped into CollaborationScore even when it was not created.
            using (Pipeline pipeline = Pipeline.Create())
            {
                var configuration = new SlidingAverageConfiguration
                {
                    ComputeCollaborationScores = false,
                    GenerateGraph = false,
                    IndicesWriter = new StringWriter(),
                };

                var indices = new SlidingAverageComputation(pipeline, null, configuration, "Indices");

                Assert.IsNull(indices.CollaborationScore);
                Assert.IsNotNull(indices.Export);
                Assert.IsNotNull(indices.TaskParticipation);
                Assert.IsNotNull(indices.FFormation);
            }
        }

        [TestMethod]
        public void LegacyFlags_ReproduceTheSelectionOfTheReplacedConstructor()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                var indices = new SlidingAverageComputation(pipeline, null, new SlidingAverageConfiguration(), "Indices");

                Assert.IsNull(indices.TurnTaking, "Turn taking was commented out of the constructor that the builder replaced.");
                Assert.IsNotNull(indices.JointVisualAttention);
                Assert.IsNotNull(indices.AttentionClockIn);

                // Computed and exported, but kept out of the score, as they were.
                List<string> scoreInputs = indices.Outputs.ScoreInputs.Select(output => output.Name).ToList();
                CollectionAssert.DoesNotContain(scoreInputs, IndexNames.JointVisualAttention);
                CollectionAssert.DoesNotContain(scoreInputs, IndexNames.GazeOnPeers);
                CollectionAssert.DoesNotContain(scoreInputs, IndexNames.SpeechEquality);
                CollectionAssert.AreEquivalent(
                    new[] { IndexNames.Movement, IndexNames.Synchrony, IndexNames.VerbalParticipation, IndexNames.TaskParticipation, IndexNames.TaskEquality, IndexNames.Formation },
                    scoreInputs);
            }
        }

        [TestMethod]
        public void LegacyFlags_PhysicalIndicesDisabled_AreNotCreated()
        {
            // They used to be instantiated, and stored, without ever being paced.
            using (Pipeline pipeline = Pipeline.Create())
            {
                var configuration = new SlidingAverageConfiguration { UsePhysicalIndices = false };

                var indices = new SlidingAverageComputation(pipeline, null, configuration, "Indices");

                Assert.IsNull(indices.ActivityLevel);
                Assert.IsNull(indices.Synchrony);
            }
        }

        [TestMethod]
        public void VerbalParticipation_AcceptsAnyParticipantIdentifiers()
        {
            // The component used to read speakingTimes[0] and speakingTimes[1]: KeyNotFoundException
            // as soon as the participants were not numbered 0 and 1.
            var times = new List<Dictionary<uint, double>>();
            var messages = new List<string>();

            IndicesHarness.Run(1, pipeline =>
            {
                Timeline timeline = new Timeline()
                    .Ticks("Clock", 0.02, 8, 1.0)
                    .Interval("Speech", 1.0, 2.5, IndexCategories.Speaking, 7)
                    .Play(pipeline);

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(3, 7)
                    .WithWindow(TimeSpan.FromSeconds(5))
                    .WithClock(timeline.TicksOf("Clock"))
                    .AddIndicator(Indicators.VerbalParticipation)
                    .Build();

                VerbalParticipationComponent verbal = indices.Get(Indicators.VerbalParticipation).Component;
                timeline.IntervalsOf("Speech").PipeTo(verbal.IntervalIn);
                verbal.SpeakingTimesOut.Do(values => times.Add(new Dictionary<uint, double>(values)));
                verbal.SpeakingTimesStringOut.Do(message => messages.Add(message));
            });

            Assert.AreEqual(3, times.Count, "Ticks at 5.02, 6.02 and 7.02 s.");
            Assert.AreEqual(0.0, times[0][3], 1e-9);
            Assert.AreEqual(1.5, times[0][7], 1e-9);
            StringAssert.StartsWith(messages[0], "Speaking Time for User 1: 0 and User 2: 1");
            StringAssert.EndsWith(messages[0], "over the last 5 seconds window");
        }

        [TestMethod]
        public void JointVisualAttention_PublishesItsGroupValueOnce()
        {
            // The group value used to be posted twice with the same originating time, which
            // \psi rejects: the pipeline stopped on the first computation.
            var group = new List<double>();

            IndicesHarness.Run(1, pipeline =>
            {
                Timeline timeline = new Timeline()
                    .Ticks("Clock", 0.02, 5, 1.0)
                    .Event("Jva", 1.5, IndexCategories.JointVisualAttention, 0, 1)
                    .Play(pipeline);

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1)
                    .WithWindow(TimeSpan.FromSeconds(2))
                    .WithCalibration(new IndexCalibration())
                    .WithClock(timeline.TicksOf("Clock"))
                    .AddIndicator(Indicators.JointVisualAttention)
                    .Build();

                JointVisualAttentionComponent jva = indices.Get(Indicators.JointVisualAttention).Component;
                timeline.EventsOf("Jva").PipeTo(jva.EventIn);
                jva.GroupOut.Do(value => group.Add(value));
            });

            CollectionAssert.AreEqual(new[] { 1.0, 1.0, 0.0 }, group, "Ticks at 2.02, 3.02 and 4.02 s; the episode of 1.5 s leaves the 2 s window at the last one.");
        }

        [TestMethod]
        public void Ticks_SlightlyCloserThanTheInterval_AreNotSkipped()
        {
            // The task and spatial indices kept a minimum delay of one interval between two
            // computations while being paced at that very interval: a tick arriving 30 ms early
            // was dropped, and the index published every other second.
            int gateTicks = 0;
            int published = 0;

            IndicesHarness.Run(1, pipeline =>
            {
                Timeline timeline = new Timeline()
                    .Ticks("Clock", 0.02, 20, 0.97)
                    .Play(pipeline);

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1)
                    .WithWindow(TimeSpan.FromSeconds(3))
                    .WithComputationInterval(TimeSpan.FromSeconds(1))
                    .WithClock(timeline.TicksOf("Clock"))
                    .AddIndicator(Indicators.TaskParticipation)
                    .AddIndicator(Indicators.Proximity)
                    .Build();

                indices.Gate.Out.Do(_ => gateTicks++);
                indices.Get(Indicators.TaskParticipation).Component.RawIndividualOut.Do(_ => published++);
            });

            Assert.IsTrue(gateTicks > 10);
            Assert.AreEqual(gateTicks, published, "One publication per tick let through by the gate.");
        }

        [TestMethod]
        public void UndefinedEquality_DoesNotEnterTheScore()
        {
            // Nobody acts on the task: the Gini index is undefined (-1). It used to reach the
            // score as an equality of 1 - (-1) = 2.
            var inputs = new List<Dictionary<string, double>>();
            var dimensions = new List<Dictionary<string, double>>();

            IndicesHarness.Run(1, pipeline =>
            {
                Timeline timeline = new Timeline()
                    .Ticks("Clock", 0.02, 8, 1.0)
                    .Play(pipeline);

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1, 2)
                    .WithWindow(TimeSpan.FromSeconds(3))
                    .WithCalibration(new IndexCalibration())
                    .WithClock(timeline.TicksOf("Clock"))
                    .AddIndicator(Indicators.TaskParticipation)
                    .AddIndicator(Indicators.TaskEquality)
                    .WithCollaborationScore()
                    .Build();

                indices.CollaborationScore.NormalizedIndicesOut.Do(values => inputs.Add(new Dictionary<string, double>(values)));
                indices.CollaborationScore.DimensionsOut.Do(values => dimensions.Add(new Dictionary<string, double>(values)));
            });

            Assert.IsTrue(inputs.Count >= 4);
            foreach (Dictionary<string, double> input in inputs)
            {
                Assert.AreEqual(0.0, input[IndexNames.TaskEquality], "An undefined equality is passed as 0, never as 2.");
            }

            foreach (Dictionary<string, double> dimension in dimensions)
            {
                Assert.IsFalse(dimension.ContainsKey("Dominance"), "The only index of the dimension is not usable, so the dimension is left out of the score.");
                Assert.IsTrue(dimension.ContainsKey("Engagement"));
            }
        }

        [TestMethod]
        public void MovementPerSample_WithASingleSample_IsZeroRatherThanNaN()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                var component = new PhysicalActivityLevelComponent(pipeline, new PhysicalActivityLevelConfiguration
                {
                    ParticipantIds = new List<uint> { 0 },
                    BodyParts = new List<string> { BodyPartNames.Head },
                    Unit = MovementUnit.DisplacementPerSample,
                    MinimumSampleCount = 1,
                });

                double level = component.ComputeActivityLevel(0, TimeSpan.FromSeconds(3), SyntheticData.Start);

                Assert.AreEqual(0.0, level);
            }
        }

        [TestMethod]
        public void Components_NoLongerNeedADatasetPipeline()
        {
            // They used to call server.GetSession in their constructor, which made them
            // impossible to create in a plain pipeline.
            using (Pipeline pipeline = Pipeline.Create())
            {
                var participants = new List<uint> { 0, 1 };

                Assert.IsNotNull(new PhysicalActivityLevelComponent(pipeline, new PhysicalActivityLevelConfiguration { ParticipantIds = participants }));
                Assert.IsNotNull(new PhysicalSynchronyComponent(pipeline, new PhysicalSynchronyConfiguration { ParticipantIds = participants }));
                Assert.IsNotNull(new VerbalParticipationComponent(pipeline, new VerbalParticipationConfiguration { ParticipantIds = participants }));
                Assert.IsNotNull(new EqualityIndexComponent(pipeline, new EqualityIndexConfiguration { ParticipantIds = participants }));
            }
        }
    }
}
