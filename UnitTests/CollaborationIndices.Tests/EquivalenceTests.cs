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
using SAAC.PipelineServices;

// The constructors kept for compatibility are part of what is tested here.
#pragma warning disable CS0618

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// Same input, before and after the builder.
    ///
    /// The files of the Golden folder were recorded with the component as it was before the
    /// builder existed (indicators hard coded in SlidingAverageComputation), on the synthetic
    /// session of <see cref="SyntheticData"/>. The same session is replayed here through the
    /// constructor kept for compatibility, through the builder and through a configuration
    /// object, and every stream of every indicator must come out identical.
    ///
    /// The score and the CSV export are deliberately left out of the comparison: they used to
    /// mix the values of two consecutive ticks, which has been fixed, so they differ by design.
    /// They are covered by FusionAlignmentTests instead.
    /// </summary>
    [TestClass]
    public class EquivalenceTests
    {
        private static readonly TimeSpan TenSeconds = TimeSpan.FromSeconds(10);

        [TestMethod]
        public void LegacyConstructor_PhysicalOnly_MatchesReference()
        {
            StreamRecorder recorder = RunSingle(false, false, (pipeline, writer) =>
                new SlidingAverageComputation(pipeline, null, LegacyConfiguration(false, false, TenSeconds, writer), "Indices"));

            AssertMatchesReference("PhysicalOnly", recorder);
        }

        [TestMethod]
        public void Builder_PhysicalOnly_MatchesReference()
        {
            StreamRecorder recorder = RunSingle(false, false, (pipeline, writer) =>
                Declare(pipeline, false, false, TenSeconds).WithCsvExport(writer).Build());

            AssertMatchesReference("PhysicalOnly", recorder);
        }

        [TestMethod]
        public void LegacyConstructor_PhysicalVerbalWithPhases_MatchesReference()
        {
            StreamRecorder recorder = RunSingle(true, true, (pipeline, writer) =>
                new SlidingAverageComputation(pipeline, null, LegacyConfiguration(true, true, TenSeconds, writer), "Indices"));

            AssertMatchesReference("PhysicalVerbalPhases", recorder);
        }

        [TestMethod]
        public void Builder_PhysicalVerbalWithPhases_MatchesReference()
        {
            StreamRecorder recorder = RunSingle(true, true, (pipeline, writer) =>
                Declare(pipeline, true, true, TenSeconds).WithCsvExport(writer).Build());

            AssertMatchesReference("PhysicalVerbalPhases", recorder);
        }

        [TestMethod]
        public void Configuration_PhysicalVerbalWithPhases_MatchesReference()
        {
            // The declaration goes through its JSON form and back before being built.
            string json;
            using (Pipeline scratch = Pipeline.Create())
            {
                json = Declare(scratch, true, true, TenSeconds).ToConfiguration().ToJson();
            }

            StreamRecorder recorder = RunSingle(true, true, (pipeline, writer) =>
                CollaborationIndicesBuilder.FromConfiguration(pipeline, CollaborationIndicesConfiguration.FromJson(json)).WithCsvExport(writer).Build());

            AssertMatchesReference("PhysicalVerbalPhases", recorder);
        }

        [TestMethod]
        public void LegacySet_TwoWindows_MatchesReference()
        {
            StreamRecorder recorder = RunSet((pipeline, writers) =>
                new SlidingAverageComputationSet(pipeline, null, LegacyConfiguration(true, false, TimeSpan.FromSeconds(30), null), writers));

            AssertMatchesReference("PhysicalVerbalSet", recorder);
        }

        [TestMethod]
        public void BuilderSet_TwoWindows_MatchesReference()
        {
            StreamRecorder recorder = RunSet((pipeline, writers) =>
                Declare(pipeline, true, false, TimeSpan.FromSeconds(30)).WithName("SlidingAverage").BuildSet(writers));

            AssertMatchesReference("PhysicalVerbalSet", recorder);
        }

        [TestMethod]
        public void LegacySet_ConnectClocks_DrivesEveryInstance()
        {
            // The calls an existing pipeline makes: the set built from a configuration by
            // families, then the two shared clocks given as TimeSpan streams.
            var ticks = new Dictionary<int, int>();
            var attention = new Dictionary<int, int>();

            IndicesHarness.Run(1, pipeline =>
            {
                Timeline timeline = new Timeline()
                    .Ticks("Clock", 0.02, 30, 1.0)
                    .Ticks("Fast", 0.005, 30, 0.1)
                    .Play(pipeline);

                var set = new SlidingAverageComputationSet(
                    pipeline,
                    null,
                    LegacyConfiguration(true, false, TimeSpan.FromSeconds(30), null),
                    new Dictionary<TimeSpan, TextWriter> { { TimeSpan.FromSeconds(10), null }, { TimeSpan.FromSeconds(20), null } });

                set.ConnectClocks(
                    timeline.TicksOf("Clock").Select(_ => IndicesHarness.Second),
                    timeline.TicksOf("Fast").Select(_ => TimeSpan.FromMilliseconds(100)));

                set.ForEach((window, indices) =>
                {
                    int key = (int)window.TotalSeconds;
                    ticks[key] = 0;
                    attention[key] = 0;
                    indices.Gate.Out.Do(_ => ticks[key]++);
                    indices.AttentionLevel.Out.Do(_ => attention[key]++);
                });
            });

            Assert.AreEqual(20, ticks[10], "Thirty ticks, the first ten spent filling the 10 s window.");
            Assert.AreEqual(10, ticks[20], "Thirty ticks, the first twenty spent filling the 20 s window.");
            Assert.AreEqual(300, attention[10], "The attention accumulator follows its own clock, whatever the gate.");
            Assert.AreEqual(300, attention[20]);
        }

        [TestMethod]
        public void BuilderSet_NamesItsInstancesAfterTheWindow()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                SlidingAverageComputationSet set = Declare(pipeline, true, false, TenSeconds)
                    .WithName("SlidingAverage")
                    .BuildSet(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));

                CollectionAssert.AreEquivalent(new[] { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20) }, set.Windows.ToList());
                Assert.AreEqual("SlidingAverage_10s", set.OfSeconds(10).Name);
                Assert.AreEqual("SlidingAverage_20s", set.OfSeconds(20).Name);
                Assert.AreEqual(TimeSpan.FromSeconds(20), set.OfSeconds(20).WindowDuration);
                Assert.IsNull(set.OfSeconds(10).Export, "No writer was given, so no export.");
            }
        }

        [TestMethod]
        public void Comparison_DetectsAChangedValueAndAMissingMessage()
        {
            // Guards the tests above: a comparison that never fails would prove nothing.
            StreamRecorder reference = StreamRecorder.Load(IndicesHarness.GoldenPath("PhysicalOnly"));

            var changed = new StreamRecorder();
            var truncated = new StreamRecorder();
            foreach (string stream in reference.Keys)
            {
                IReadOnlyList<StreamRecorder.Sample> samples = reference.Get(stream);
                for (int i = 0; i < samples.Count; i++)
                {
                    bool target = stream == "Movement.Group" && i == 7;
                    double value = target ? double.Parse(samples[i].Payload, System.Globalization.CultureInfo.InvariantCulture) : 0;
                    changed.Add(stream, samples[i].Time, target ? StreamRecorder.Format(value + 1e-6) : samples[i].Payload);

                    if (!(stream == "Synchrony.Pair" && i == samples.Count - 1))
                    {
                        truncated.Add(stream, samples[i].Time, samples[i].Payload);
                    }
                }
            }

            List<string> streams = IndicesHarness.IndicatorStreams(reference).ToList();

            Assert.AreEqual(0, reference.DifferencesWith(reference, streams).Count);

            List<string> valueDifferences = changed.DifferencesWith(reference, streams);
            Assert.AreEqual(1, valueDifferences.Count);
            StringAssert.StartsWith(valueDifferences[0], "Movement.Group[7]");

            List<string> countDifferences = truncated.DifferencesWith(reference, streams);
            Assert.AreEqual(1, countDifferences.Count);
            StringAssert.StartsWith(countDifferences[0], "Synchrony.Pair:");
        }

        [TestMethod]
        public void AnotherScenario_DoesNotMatchTheReference()
        {
            // Same guard, end to end: the indices of a session with phases are not those of a session without.
            StreamRecorder recorder = RunSingle(false, true, (pipeline, writer) =>
                Declare(pipeline, false, true, TenSeconds).WithCsvExport(writer).Build());

            StreamRecorder reference = StreamRecorder.Load(IndicesHarness.GoldenPath("PhysicalOnly"));
            List<string> differences = recorder.DifferencesWith(reference, IndicesHarness.IndicatorStreams(reference));

            Assert.IsTrue(differences.Count > 5, $"Only {differences.Count} differences found.");
        }

        /// <summary>The configuration by families of indices used to record the reference.</summary>
        internal static SlidingAverageConfiguration LegacyConfiguration(bool verbal, bool requirePhase, TimeSpan window, TextWriter writer)
            => new SlidingAverageConfiguration
            {
                ParticipantIds = SyntheticData.Participants.ToList(),
                WindowDuration = window,
                UseTaskIndices = false,
                UseSpatialIndices = false,
                UseVerbalIndices = verbal,
                UseVisualIndices = false,
                UsePhysicalIndices = true,
                ComputeCollaborationScores = true,
                GenerateGraph = false,
                UseInternalClock = false,
                RequirePhase = requirePhase,
                IndicesWriter = writer,
            };

        /// <summary>The same indices, declared with the builder.</summary>
        internal static CollaborationIndicesBuilder Declare(Pipeline pipeline, bool verbal, bool requirePhase, TimeSpan window)
        {
            CollaborationIndicesBuilder builder = new CollaborationIndicesBuilder(pipeline)
                .WithName("Indices")
                .WithParticipants(SyntheticData.Participants)
                .WithWindow(window)
                .WithPhaseGate(requirePhase)
                .AddIndicator(Indicators.AttentionLevel)
                .AddIndicator(Indicators.Movement, options => options.AdditionalWindows = new List<TimeSpan> { TimeSpan.FromSeconds(5) })
                .AddIndicator(Indicators.Synchrony);

            if (verbal)
            {
                builder
                    .AddIndicator(Indicators.VerbalParticipation)
                    .AddIndicator(Indicators.SpeechEquality)
                    .AddIndicator(Indicators.TalkingMost);
            }

            return builder.WithCollaborationScore();
        }

        private static StreamRecorder RunSingle(bool verbal, bool phases, Func<Pipeline, StringWriter, SlidingAverageComputation> build)
        {
            var recorder = new StreamRecorder();
            var writer = new StringWriter();

            IndicesHarness.Run(1, pipeline =>
            {
                SessionPlayer player = phases
                    ? new SessionPlayer(pipeline, IndicesHarness.Second, new[] { 3.0, 35.0 }, new[] { 30.0 })
                    : new SessionPlayer(pipeline, IndicesHarness.Second);

                SlidingAverageComputation indices = build(pipeline, writer);
                Assert.AreEqual(verbal, indices.Contains(IndexNames.VerbalParticipation));

                IndicesHarness.Connect(player, indices);
                IndicesHarness.Record(recorder, indices);
            });

            IndicesHarness.RecordCsv(recorder, writer);
            return recorder;
        }

        private static StreamRecorder RunSet(Func<Pipeline, Dictionary<TimeSpan, TextWriter>, SlidingAverageComputationSet> build)
        {
            var recorder = new StreamRecorder();
            var writers = new Dictionary<TimeSpan, TextWriter>
            {
                { TimeSpan.FromSeconds(10), new StringWriter() },
                { TimeSpan.FromSeconds(20), new StringWriter() },
            };

            IndicesHarness.Run(1, pipeline =>
            {
                var player = new SessionPlayer(pipeline, IndicesHarness.Second);
                SlidingAverageComputationSet set = build(pipeline, writers);

                set.ForEach((window, indices) =>
                {
                    Assert.AreEqual($"SlidingAverage_{(int)window.TotalSeconds}s", indices.Name);
                    IndicesHarness.Connect(player, indices);
                    IndicesHarness.Record(recorder, indices, $"W{(int)window.TotalSeconds}.");
                });
            });

            foreach (var writer in writers)
            {
                IndicesHarness.RecordCsv(recorder, (StringWriter)writer.Value, $"W{(int)writer.Key.TotalSeconds}.");
            }

            return recorder;
        }

        private static void AssertMatchesReference(string scenario, StreamRecorder actual)
        {
            StreamRecorder reference = StreamRecorder.Load(IndicesHarness.GoldenPath(scenario));
            List<string> streams = IndicesHarness.IndicatorStreams(reference).ToList();

            Assert.IsTrue(streams.Count >= 15, $"The reference of {scenario} should hold the streams of several indicators, found {streams.Count}.");

            List<string> differences = actual.DifferencesWith(reference, streams);
            Assert.AreEqual(0, differences.Count, $"{differences.Count} stream(s) differ from the reference:{Environment.NewLine}{string.Join(Environment.NewLine, differences)}");
        }
    }
}
