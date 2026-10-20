// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Psi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.CollaborationIndices;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// The score, the graph and the export must be built from indices of the same tick.
    ///
    /// Before the snapshot assembler they were not: each index reached them on its own
    /// receiver, and the score was computed when the first one of a tick arrived, with the
    /// previous value of all the others. The first test documents that on the reference
    /// recording; the others check that it no longer happens, including with several threads,
    /// where the arrival order changes from one run to the next.
    /// </summary>
    [TestClass]
    public class FusionAlignmentTests
    {
        private static readonly Dictionary<string, string> GroupStreamOfIndex = new Dictionary<string, string>
        {
            { IndexNames.Movement, "Movement.Group" },
            { IndexNames.Synchrony, "Synchrony.Group" },
            { IndexNames.VerbalParticipation, "Verbal.Group" },
        };

        [TestMethod]
        public void ReferenceRecording_ShowsTheScoreMixingTwoTicks()
        {
            StreamRecorder reference = StreamRecorder.Load(IndicesHarness.GoldenPath("PhysicalVerbalPhases"));

            int stale = CountStaleScoreInputs(reference);

            Assert.IsTrue(stale > 20, $"The legacy score was expected to use indices of the previous tick; {stale} stale values found.");
        }

        [TestMethod]
        public void Score_UsesTheIndicesOfItsOwnTick()
        {
            StreamRecorder recorder = RunSession(1, out _);

            Assert.AreEqual(recorder.Get("Gate.Tick").Count, recorder.Get("Score.Indices").Count, "One score per tick.");
            Assert.AreEqual(0, CountStaleScoreInputs(recorder));
        }

        [TestMethod]
        public void Score_UsesTheIndicesOfItsOwnTick_WithSeveralThreads()
        {
            for (int run = 0; run < 5; run++)
            {
                StreamRecorder recorder = RunSession(0, out _);

                Assert.AreEqual(recorder.Get("Gate.Tick").Count, recorder.Get("Score.Indices").Count, $"Run {run}: one score per tick.");
                Assert.AreEqual(0, CountStaleScoreInputs(recorder), $"Run {run}");
            }
        }

        [TestMethod]
        public void Score_IsTheMeanOfItsDimensions()
        {
            StreamRecorder recorder = RunSession(1, out _);

            IReadOnlyList<StreamRecorder.Sample> indices = recorder.Get("Score.Indices");
            IReadOnlyList<StreamRecorder.Sample> dimensions = recorder.Get("Score.Dimensions");
            IReadOnlyList<StreamRecorder.Sample> global = recorder.Get("Score.Global");

            Assert.IsTrue(indices.Count > 20);
            for (int i = 0; i < indices.Count; i++)
            {
                Dictionary<string, double> index = StreamRecorder.ParseDictionary(indices[i].Payload);
                Dictionary<string, double> dimension = StreamRecorder.ParseDictionary(dimensions[i].Payload);

                // Default model: each dimension is the mean of log(1 + index) over the indices it has.
                Assert.AreEqual(Math.Log(1 + index[IndexNames.Movement]), dimension["Engagement"], 1e-12);
                Assert.AreEqual(Math.Log(1 + index[IndexNames.VerbalParticipation]), dimension["CommunicationProcessManagement"], 1e-12);
                if (index.ContainsKey(IndexNames.Synchrony))
                {
                    Assert.AreEqual(Math.Log(1 + index[IndexNames.Synchrony]), dimension["SpatialBehaviour"], 1e-12);
                }

                // The equality only counts while the group speaks enough for it to mean something.
                if (dimension.ContainsKey("Dominance"))
                {
                    Assert.AreEqual(Math.Log(1 + index[IndexNames.SpeechEquality]), dimension["Dominance"], 1e-12);
                }

                Assert.AreEqual(dimension.Values.Average(), double.Parse(global[i].Payload, CultureInfo.InvariantCulture), 1e-12);
            }
        }

        [TestMethod]
        public void Export_RowHoldsTheValuesOfItsTick_WithSeveralThreads()
        {
            StreamRecorder recorder = RunSession(0, out StringWriter csv);

            List<string> rows = IndicesHarness.Rows(csv);
            string[] header = rows[0].Split(';');
            IReadOnlyList<StreamRecorder.Sample> ticks = recorder.Get("Gate.Tick");

            Assert.AreEqual("Timestamp", header[0]);
            Assert.AreEqual(ticks.Count, rows.Count - 1, "One row per tick, plus the header.");

            for (int i = 0; i < ticks.Count; i++)
            {
                DateTime tick = ticks[i].Time;
                string[] cells = rows[i + 1].Split(';');
                Dictionary<string, string> row = header.Zip(cells, (column, cell) => new { column, cell }).ToDictionary(x => x.column, x => x.cell);

                Assert.AreEqual(UnixMilliseconds(tick), row["Timestamp"]);

                Dictionary<string, double> movement = StreamRecorder.ParseDictionary(IndicesHarness.LatestAt(recorder, "Movement.Individual", tick));
                Dictionary<string, double> verbal = StreamRecorder.ParseDictionary(IndicesHarness.LatestAt(recorder, "Verbal.Individual", tick));
                foreach (uint id in SyntheticData.Participants)
                {
                    Assert.AreEqual(Cell(movement[id.ToString()]), row[$"Movement_{id}"], $"Movement of {id} at row {i}");
                    Assert.AreEqual(Cell(verbal[id.ToString()]), row[$"VerbalParticipation_{id}"], $"Verbal participation of {id} at row {i}");
                }

                string synchrony = IndicesHarness.LatestAt(recorder, "Synchrony.Pair", tick);
                Assert.AreEqual(synchrony == null ? "NA" : Cell(StreamRecorder.ParseDictionary(synchrony)["0-1"]), row["Synchrony_0-1"], $"Synchrony at row {i}");

                string score = IndicesHarness.LatestAt(recorder, "Score.Global", tick);
                Assert.AreEqual(Cell(double.Parse(score, CultureInfo.InvariantCulture)), row["CollaborationScore"], $"Score at row {i}");
                Assert.AreEqual(Cell(double.Parse(IndicesHarness.LatestAt(recorder, "SpeechEquality.Group", tick), CultureInfo.InvariantCulture)), row["SpeechEquality"]);
            }
        }

        [TestMethod]
        public void Snapshot_CarriesEveryIndexOfTheTick()
        {
            var snapshots = new List<IndexSnapshot>();
            StreamRecorder recorder = RunSession(0, out _, indices => indices.SnapshotOut.Do(snapshot => snapshots.Add(snapshot.Clone())));

            Assert.AreEqual(recorder.Get("Gate.Tick").Count, snapshots.Count);

            foreach (IndexSnapshot snapshot in snapshots)
            {
                DateTime tick = snapshot.OriginatingTime;

                Assert.AreEqual(
                    IndicesHarness.LatestAt(recorder, "Movement.Group", tick),
                    StreamRecorder.Format(snapshot.Group[IndexNames.Movement]));
                Assert.AreEqual(
                    IndicesHarness.LatestAt(recorder, "Movement.Individual", tick),
                    StreamRecorder.Format(snapshot.Individual[IndexNames.Movement]));
                Assert.AreEqual(
                    IndicesHarness.LatestAt(recorder, "Verbal.Group", tick),
                    StreamRecorder.Format(snapshot.Group[IndexNames.VerbalParticipation]));
                Assert.AreEqual(
                    IndicesHarness.LatestAt(recorder, "Score.Global", tick),
                    StreamRecorder.Format(snapshot.Group[IndexNames.CollaborationScore]));

                string synchrony = IndicesHarness.LatestAt(recorder, "Synchrony.Pair", tick);
                if (synchrony != null)
                {
                    Assert.AreEqual(synchrony, StreamRecorder.Format(snapshot.Pair[IndexNames.Synchrony]));
                }
            }
        }

        [TestMethod]
        public void Assembler_WaitsForTheValuesThatWereAnnounced()
        {
            var snapshots = new List<IndexSnapshot>();

            IndicesHarness.Run(1, pipeline =>
            {
                Timeline timeline = new Timeline()
                    .Ticks("Clock", 1.0, 4.5, 1.0)
                    .Play(pipeline);

                var assembler = new IndexSnapshotAssembler(pipeline);
                IProducer<bool> clock = timeline.TicksOf("Clock");
                clock.PipeTo(assembler.TickIn);

                // The indicator publishes on every tick.
                int pulse = assembler.AddPulse(clock.Select(_ => true));

                // Its value takes a long detour, while the tick and the marker go straight in:
                // they reach the assembler well before the value they announce.
                IProducer<double> values = clock.Select((_, envelope) => (envelope.OriginatingTime - SyntheticData.Start).TotalSeconds);
                for (int hop = 0; hop < 20; hop++)
                {
                    values = values.Select(value => value);
                }

                assembler.AddGroup("Index", values, pulse);
                assembler.Out.Do(snapshot => snapshots.Add(snapshot.Clone()));
            });

            Assert.AreEqual(4, snapshots.Count);
            foreach (IndexSnapshot snapshot in snapshots)
            {
                double tick = (snapshot.OriginatingTime - SyntheticData.Start).TotalSeconds;
                Assert.AreEqual(tick, snapshot.Group["Index"], 1e-9, "The snapshot of a tick holds the value of that tick, not of the previous one.");
            }
        }

        [TestMethod]
        public void Assembler_DoesNotWaitForAnIndicatorThatSkippedTheTick()
        {
            var snapshots = new List<IndexSnapshot>();

            IndicesHarness.Run(1, pipeline =>
            {
                Timeline timeline = new Timeline()
                    .Ticks("Clock", 1.0, 5.5, 1.0)
                    .Play(pipeline);

                var assembler = new IndexSnapshotAssembler(pipeline);
                IProducer<bool> clock = timeline.TicksOf("Clock");
                clock.PipeTo(assembler.TickIn);

                // The indicator publishes on odd seconds only, and says so on the even ones.
                IProducer<bool> published = clock.Select((_, envelope) => ((int)Math.Round((envelope.OriginatingTime - SyntheticData.Start).TotalSeconds)) % 2 == 1);
                int pulse = assembler.AddPulse(published);

                IProducer<double> values = published
                    .Where(isPublished => isPublished)
                    .Select((_, envelope) => (envelope.OriginatingTime - SyntheticData.Start).TotalSeconds);
                assembler.AddGroup("Index", values, pulse);

                assembler.Out.Do(snapshot => snapshots.Add(snapshot.Clone()));
            });

            // Five ticks, five snapshots: the skipped ticks keep the last value instead of stalling.
            CollectionAssert.AreEqual(
                new[] { 1.0, 1.0, 3.0, 3.0, 5.0 },
                snapshots.Select(snapshot => snapshot.Group["Index"]).ToList());
        }

        [TestMethod]
        public void Assembler_ReadsTheLatestValueOfAnInputWithoutPulse()
        {
            var snapshots = new List<IndexSnapshot>();

            IndicesHarness.Run(1, pipeline =>
            {
                Timeline timeline = new Timeline()
                    .Ticks("Clock", 1.0, 3.5, 1.0)
                    .Ticks("Fast", 0.1, 3.5, 0.25)
                    .Play(pipeline);

                var assembler = new IndexSnapshotAssembler(pipeline);
                timeline.TicksOf("Clock").PipeTo(assembler.TickIn);
                assembler.AddGroup("Fast", timeline.TicksOf("Fast").Select((_, envelope) => (envelope.OriginatingTime - SyntheticData.Start).TotalSeconds));
                assembler.Out.Do(snapshot => snapshots.Add(snapshot.Clone()));
            });

            // The fast stream ticks at 0.1, 0.35, 0.6, 0.85, 1.1, ...: the last one before each second.
            CollectionAssert.AreEqual(new[] { 0.85, 1.85, 2.85 }, snapshots.Select(snapshot => Math.Round(snapshot.Group["Fast"], 2)).ToList());
        }

        [TestMethod]
        public void Assembler_GoesOnWhenAnAnnouncedValueNeverComes()
        {
            var snapshots = new List<IndexSnapshot>();
            IndexSnapshotAssembler assembler = null;

            IndicesHarness.Run(1, pipeline =>
            {
                Timeline timeline = new Timeline()
                    .Ticks("Clock", 1.0, 10.5, 1.0)
                    .Play(pipeline);

                assembler = new IndexSnapshotAssembler(pipeline, new IndexSnapshotAssemblerConfiguration { MaximumPendingTicks = 3, LogWarnings = false });
                IProducer<bool> clock = timeline.TicksOf("Clock");
                clock.PipeTo(assembler.TickIn);

                // A faulty indicator: it announces a value on every tick and never posts any.
                int pulse = assembler.AddPulse(clock.Select(_ => true));
                assembler.AddGroup("Missing", clock.Where(_ => false).Select(_ => 0.0), pulse);
                assembler.Out.Do(snapshot => snapshots.Add(snapshot.Clone()));
            });

            Assert.IsTrue(snapshots.Count >= 7, $"The pipeline must not stay silent: {snapshots.Count} snapshots.");
            Assert.IsTrue(assembler.ForcedSnapshotCount > 0);
            Assert.IsFalse(snapshots[0].Group.ContainsKey("Missing"));
        }

        private static StreamRecorder RunSession(int threads, out StringWriter csv, Action<SlidingAverageComputation> also = null)
        {
            var recorder = new StreamRecorder();
            var writer = new StringWriter();

            IndicesHarness.Run(threads, pipeline =>
            {
                var player = new SessionPlayer(pipeline, IndicesHarness.Second, new[] { 3.0, 35.0 }, new[] { 30.0 });
                SlidingAverageComputation indices = EquivalenceTests.Declare(pipeline, true, true, TimeSpan.FromSeconds(10)).WithCsvExport(writer).Build();

                IndicesHarness.Connect(player, indices);
                IndicesHarness.Record(recorder, indices);
                also?.Invoke(indices);
            });

            csv = writer;
            return recorder;
        }

        /// <summary>
        /// Number of score inputs that are not the value published by the indicator for the
        /// tick of the score (or its last value when the indicator skipped that tick).
        /// </summary>
        private static int CountStaleScoreInputs(StreamRecorder recorder)
        {
            int stale = 0;
            foreach (StreamRecorder.Sample score in recorder.Get("Score.Indices"))
            {
                foreach (var input in StreamRecorder.ParseDictionary(score.Payload))
                {
                    double? expected;
                    if (input.Key == IndexNames.SpeechEquality)
                    {
                        // The score receives the equality, 1 - Gini, and 0 while the index is undefined.
                        string gini = IndicesHarness.LatestAt(recorder, "SpeechEquality.Group", score.Time);
                        expected = gini == null ? (double?)null : (gini == "-1" ? 0.0 : 1.0 - double.Parse(gini, CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        string group = IndicesHarness.LatestAt(recorder, GroupStreamOfIndex[input.Key], score.Time);
                        expected = group == null ? (double?)null : double.Parse(group, CultureInfo.InvariantCulture);
                    }

                    if (expected != input.Value)
                    {
                        stale++;
                    }
                }
            }

            return stale;
        }

        private static string Cell(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

        private static string UnixMilliseconds(DateTime time)
            => time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
