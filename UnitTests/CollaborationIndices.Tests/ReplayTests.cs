// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.CollaborationIndices;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// Replay of recorded data: the windows must be cut on the timestamps of the messages,
    /// never on the clock of the machine.
    /// </summary>
    [TestClass]
    public class ReplayTests
    {
        private const double Window = 10.0;

        [TestMethod]
        public void Replay_FasterThanRealTime_CutsItsWindowsOnTheMessageTimestamps()
        {
            var recorder = new StreamRecorder();
            var stopwatch = Stopwatch.StartNew();

            // A minute of data, dated January 2026, replayed as fast as the machine allows.
            IndicesHarness.Run(1, pipeline =>
            {
                var player = new SessionPlayer(pipeline, IndicesHarness.Second);
                SlidingAverageComputation indices = Declare(pipeline).Build();
                IndicesHarness.Connect(player, indices);
                IndicesHarness.Record(recorder, indices);
            });

            stopwatch.Stop();
            Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(30), $"The minute of data was expected to replay in a few seconds, it took {stopwatch.Elapsed}.");

            // The whole minute was computed, although a 10 s window never elapsed on the wall clock.
            AssertVerbalParticipationFollowsTheData(recorder, 50);

            foreach (string stream in recorder.Keys)
            {
                foreach (StreamRecorder.Sample sample in recorder.Get(stream))
                {
                    Assert.IsTrue(
                        sample.Time >= SyntheticData.Start && sample.Time <= SyntheticData.Start + SyntheticData.Duration,
                        $"{stream} is stamped {sample.Time:o}, outside the session: an index must carry the time of the data it describes.");
                }
            }
        }

        [TestMethod]
        public void Replay_OfTheSameDataAtAnotherDate_GivesTheSameValues()
        {
            // 1000 days later: a whole number of days, so the 50 ms grid of the synchrony does not move.
            TimeSpan shift = TimeSpan.FromDays(1000);

            StreamRecorder original = RunDirect(TimeSpan.Zero);
            StreamRecorder shifted = RunDirect(shift);

            var realigned = new StreamRecorder();
            foreach (string stream in shifted.Keys)
            {
                foreach (StreamRecorder.Sample sample in shifted.Get(stream))
                {
                    realigned.Add(stream, sample.Time - shift, sample.Payload);
                }
            }

            List<string> differences = realigned.DifferencesWith(original, original.Keys.ToList(), 1e-12);
            Assert.IsTrue(original.Keys.Count() > 15);
            Assert.AreEqual(0, differences.Count, string.Join(Environment.NewLine, differences));
        }

        [TestMethod]
        public void Replay_FromAStore_GivesTheSameIndicesAsTheLiveStreams()
        {
            string folder = Path.Combine(Path.GetTempPath(), "CollaborationIndicesTests_" + Guid.NewGuid().ToString("N"));
            try
            {
                // Record the session into a \psi store...
                IndicesHarness.Run(1, pipeline =>
                {
                    var player = new SessionPlayer(pipeline, IndicesHarness.Second);
                    PsiExporter store = PsiStore.Create(pipeline, "Session", folder);
                    player.Source.Write("Messages", store);
                });

                // ...then compute the indices from the store, in another pipeline.
                var replayed = new StreamRecorder();
                IndicesHarness.Run(1, pipeline =>
                {
                    PsiImporter store = PsiStore.Open(pipeline, "Session", folder);
                    var player = new SessionPlayer(store.OpenStream<SessionPlayer.SessionMessage>("Messages"));

                    SlidingAverageComputation indices = Declare(pipeline).Build();
                    IndicesHarness.Connect(player, indices);
                    IndicesHarness.Record(replayed, indices);
                });

                AssertVerbalParticipationFollowsTheData(replayed, 50);

                StreamRecorder direct = RunDirect(TimeSpan.Zero);
                List<string> differences = replayed.DifferencesWith(direct, direct.Keys.ToList(), 1e-12);
                Assert.AreEqual(0, differences.Count, string.Join(Environment.NewLine, differences));
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        [TestMethod]
        public void DataClock_TicksOnTheTimelineOfTheData()
        {
            var recorder = new StreamRecorder();

            IndicesHarness.Run(1, pipeline =>
            {
                var player = new SessionPlayer(pipeline, IndicesHarness.Second);

                // No tick stream at all: the clock is derived from the head positions of participant 0.
                SlidingAverageComputation indices = Declare(pipeline)
                    .WithDataClock(player.Positions(0, BodyPartNames.Head))
                    .Build();

                IndicesHarness.Connect(player, indices, connectClock: false);
                IndicesHarness.Record(recorder, indices);
            });

            IReadOnlyList<StreamRecorder.Sample> ticks = recorder.Get("Gate.Tick");
            Assert.IsTrue(ticks.Count >= 45, $"About one tick per second of data after the warm-up, got {ticks.Count}.");

            var sampleTimes = new HashSet<DateTime>(SyntheticData.Positions(0, BodyPartNames.Head).Select(sample => sample.Item2));
            for (int i = 0; i < ticks.Count; i++)
            {
                Assert.IsTrue(sampleTimes.Contains(ticks[i].Time), "Each tick carries the timestamp of a message of the source.");
                if (i > 0)
                {
                    Assert.IsTrue(ticks[i].Time - ticks[i - 1].Time >= IndicesHarness.Second);
                }
            }

            AssertVerbalParticipationFollowsTheData(recorder, ticks.Count);
        }

        private static CollaborationIndicesBuilder Declare(Pipeline pipeline)
            => new CollaborationIndicesBuilder(pipeline)
                .WithParticipants(SyntheticData.Participants)
                .WithWindow(TimeSpan.FromSeconds(Window))
                .AddIndicator(Indicators.Movement)
                .AddIndicator(Indicators.Synchrony)
                .AddIndicator(Indicators.VerbalParticipation)
                .AddIndicator(Indicators.SpeechEquality)
                .WithCollaborationScore();

        private static StreamRecorder RunDirect(TimeSpan shift)
        {
            var recorder = new StreamRecorder();
            IndicesHarness.Run(1, pipeline =>
            {
                var player = new SessionPlayer(pipeline, IndicesHarness.Second, shift: shift);
                SlidingAverageComputation indices = Declare(pipeline).Build();
                IndicesHarness.Connect(player, indices);
                IndicesHarness.Record(recorder, indices);
            });

            return recorder;
        }

        /// <summary>
        /// Checks every published speaking time against the speech intervals of the window
        /// that ends at the timestamp of the value.
        /// </summary>
        private static void AssertVerbalParticipationFollowsTheData(StreamRecorder recorder, int expectedCount)
        {
            IReadOnlyList<StreamRecorder.Sample> speakingTimes = recorder.Get("Verbal.SpeakingTimes");
            Assert.AreEqual(expectedCount, speakingTimes.Count);

            foreach (StreamRecorder.Sample sample in speakingTimes)
            {
                double now = (sample.Time - SyntheticData.Start).TotalSeconds;
                Dictionary<string, double> seconds = StreamRecorder.ParseDictionary(sample.Payload);

                foreach (uint id in SyntheticData.Participants)
                {
                    Assert.AreEqual(
                        SyntheticData.ExpectedSpeakingSeconds(id, now - Window, now),
                        seconds[id.ToString()],
                        1e-6,
                        $"Speaking time of participant {id} over the {Window} s ending at {now:0.000} s of data");
                }
            }
        }
    }
}
