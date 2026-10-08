// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.CollaborationIndices;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// Are the turn takings detected the ones that happened? The conversations here are
    /// written or simulated, so what happened is known: who held the floor, who took it and
    /// how. The speech intervals reach the detector the way a recogniser delivers them, each
    /// one when it ends, and what the detector publishes is compared with what happened.
    ///
    /// Vocabulary, after Gravano and Hirschberg (2011):
    ///  - turn taking without overlap: the floor passes to somebody who starts after the
    ///    previous speaker stopped (smooth switch);
    ///  - turn taking with overlap: the floor passes to somebody who started before the
    ///    previous speaker stopped, and goes on;
    ///  - overlap: somebody speaks over the speaker, who keeps the floor (backchannel,
    ///    failed interruption).
    /// </summary>
    [TestClass]
    public class TurnTakingScenarioTests
    {
        private const uint A = 0;
        private const uint B = 1;
        private const uint C = 2;
        private const string Without = IndexCategories.TurnTakingWithoutOverlap;
        private const string With = IndexCategories.TurnTakingWithOverlap;
        private const string Over = IndexCategories.Overlap;

        private static readonly List<uint> Trio = new List<uint> { A, B, C };

        // ------------------------------------------------------------------
        // 1. Every elementary configuration, written by hand
        // ------------------------------------------------------------------

        [TestMethod]
        public void ElementaryConfigurations_AreDetectedAsWhatTheyAre()
        {
            var failures = new List<string>();

            void Check(string name, (uint Speaker, double Start, double End)[] speech, params (string Category, uint Who, uint From, double At)[] expected)
            {
                List<string> detected = Describe(Detect(speech.Select(s => Speech(s.Speaker, s.Start, s.End)).ToList(), new TurnTakingDetectorConfiguration { ParticipantIds = Trio }));
                List<string> wanted = expected.Select(e => $"{e.Category} {e.Who}<{e.From} @{e.At:0.0}").OrderBy(s => s).ToList();
                if (!detected.SequenceEqual(wanted))
                {
                    failures.Add($"{name}: expected [{string.Join(", ", wanted)}], detected [{string.Join(", ", detected)}]");
                }
            }

            Check("first utterance of a session", new[] { (A, 0.0, 2.0) });

            Check("same speaker in several segments", new[] { (A, 0.0, 2.0), (A, 2.5, 4.0), (A, 5.0, 7.0) });

            Check("smooth switch", new[] { (A, 0.0, 2.0), (B, 3.0, 5.0) }, (Without, B, A, 3.0));

            Check("switch without any gap", new[] { (A, 0.0, 2.0), (B, 2.0, 4.0) }, (Without, B, A, 2.0));

            Check("switch after a long silence", new[] { (A, 0.0, 2.0), (B, 40.0, 42.0) }, (Without, B, A, 40.0));

            Check("overlapping switch", new[] { (A, 0.0, 3.0), (B, 2.5, 5.0) }, (With, B, A, 2.5));

            Check("backchannel", new[] { (A, 0.0, 5.0), (B, 2.0, 2.6) }, (Over, B, A, 2.0));

            Check(
                "backchannel, then the speaker goes on in a new segment",
                new[] { (A, 0.0, 5.0), (B, 2.0, 2.6), (A, 5.5, 8.0) },
                (Over, B, A, 2.0));

            Check(
                "backchannel after an earlier segment of the same speaker",
                new[] { (A, 0.0, 2.0), (A, 2.5, 7.0), (B, 4.0, 4.6) },
                (Over, B, A, 4.0));

            Check(
                "failed interruption, then smooth switch to the same participant",
                new[] { (A, 0.0, 5.0), (B, 3.0, 3.5), (B, 6.0, 8.0) },
                (Over, B, A, 3.0),
                (Without, B, A, 6.0));

            Check(
                "three smooth switches round the group",
                new[] { (A, 0.0, 2.0), (B, 3.0, 4.0), (C, 5.0, 6.0), (A, 7.0, 8.0) },
                (Without, B, A, 3.0),
                (Without, C, B, 5.0),
                (Without, A, C, 7.0));

            Check(
                "backchannel of one listener, then the other takes the turn",
                new[] { (A, 0.0, 6.0), (B, 2.0, 2.5), (C, 7.0, 9.0) },
                (Over, B, A, 2.0),
                (Without, C, A, 7.0));

            Check(
                "overlapping switch, then the former speaker backchannels",
                new[] { (A, 0.0, 3.0), (B, 2.5, 8.0), (A, 5.0, 5.4) },
                (With, B, A, 2.5),
                (Over, A, B, 5.0));

            Check(
                "two overlapping switches in a row",
                new[] { (A, 0.0, 3.0), (B, 2.5, 5.0), (A, 4.6, 7.0) },
                (With, B, A, 2.5),
                (With, A, B, 4.6));

            Check(
                "a short answer between two segments of a speaker is a turn, and the turn comes back",
                new[] { (A, 0.0, 2.0), (B, 2.3, 2.8), (A, 3.0, 5.0) },
                (Without, B, A, 2.3),
                (Without, A, B, 3.0));

            Assert.AreEqual(0, failures.Count, "\n" + string.Join("\n", failures));
        }

        [TestMethod]
        public void KnownLimits_AreWhatTheDocumentationSays()
        {
            // Two listeners backchannel at the same time: both speak over A, and neither takes
            // the turn from the other, although C outlasts B.
            List<string> simultaneous = Describe(Detect(
                new List<InteractionInterval> { Speech(A, 0.0, 6.0), Speech(B, 2.0, 2.5), Speech(C, 2.2, 2.8) },
                new TurnTakingDetectorConfiguration { ParticipantIds = Trio }));
            CollectionAssert.AreEquivalent(new[] { "Overlap 1<0 @2.0", "Overlap 2<0 @2.2" }, simultaneous, "Two backchannels at once are two overlaps.");

            // Three participants speak at once for a moment: C comes in while B, who had just
            // taken the floor from A, and A are both still speaking. The floor is taken from B.
            List<string> triple = Describe(Detect(
                new List<InteractionInterval> { Speech(A, 0.0, 5.1), Speech(B, 4.2, 5.7), Speech(C, 5.0, 8.0) },
                new TurnTakingDetectorConfiguration { ParticipantIds = Trio }));
            CollectionAssert.AreEquivalent(new[] { "TurnTakingWithOverlap 1<0 @4.2", "TurnTakingWithOverlap 2<1 @5.0" }, triple, "The floor is taken from its last holder only.");

            // A backchannel over an interval that ends later than the confirmation delay: the
            // backchannel is published as a turn taking before the long interval is known.
            var speech = new List<InteractionInterval> { Speech(C, 0.0, 2.0), Speech(A, 3.0, 23.0), Speech(B, 5.0, 5.5) };
            List<string> late = Describe(Detect(speech, new TurnTakingDetectorConfiguration { ParticipantIds = Trio, ConfirmationDelay = TimeSpan.FromSeconds(5) }));
            CollectionAssert.AreEquivalent(
                new[] { "TurnTakingWithoutOverlap 0<2 @3.0", "TurnTakingWithoutOverlap 1<2 @5.0", "Overlap 1<0 @5.0" },
                late,
                "Limit: with a delay shorter than the long interval, the backchannel is also counted as a turn taking.");

            List<string> patient = Describe(Detect(speech, new TurnTakingDetectorConfiguration { ParticipantIds = Trio, ConfirmationDelay = TimeSpan.FromSeconds(30) }));
            CollectionAssert.AreEquivalent(new[] { "TurnTakingWithoutOverlap 0<2 @3.0", "Overlap 1<0 @5.0" }, patient, "A delay longer than the interval removes it.");

            List<string> limited = Describe(Detect(
                new List<InteractionInterval> { Speech(A, 0.0, 2.0), Speech(B, 40.0, 42.0) },
                new TurnTakingDetectorConfiguration { ParticipantIds = Trio, MaximumSilence = TimeSpan.FromSeconds(10) }));
            Assert.AreEqual(0, limited.Count, "With a maximum silence, speaking long after somebody is not taking the turn from them.");
        }

        // ------------------------------------------------------------------
        // 2. Simulated conversations
        // ------------------------------------------------------------------

        [TestMethod]
        public void SimulatedConversations_AreRecoveredFromTheSpeechIntervals()
        {
            foreach (int participants in new[] { 2, 3, 4 })
            {
                var totals = new Dictionary<string, Score> { { Without, new Score() }, { With, new Score() }, { Over, new Score() } };
                for (int seed = 1; seed <= 10; seed++)
                {
                    Conversation conversation = Conversation.Simulate(seed, participants, turns: 120, longestSegment: 4.5);
                    var configuration = new TurnTakingDetectorConfiguration { ParticipantIds = conversation.Participants };
                    List<InteractionEvent> detected = Detect(conversation.Speech, configuration);
                    Accumulate(totals, conversation.Truth, Describe(detected));

                    // What was said around an event that did not happen, to read the configuration.
                    foreach (InteractionEvent invented in detected.Where(d => !conversation.Truth.Contains(Describe(new[] { d })[0])).Take(2))
                    {
                        var around = conversation.Speech
                            .Where(s => (s.EndTime - invented.OriginatingTime).TotalSeconds > -6 && (s.StartTime - invented.OriginatingTime).TotalSeconds < 6)
                            .OrderBy(s => s.StartTime)
                            .Select(s => $"{s.ParticipantId}[{(s.StartTime - SyntheticData.Start).TotalSeconds:0.0},{(s.EndTime - SyntheticData.Start).TotalSeconds:0.0}]");
                        Console.WriteLine($"   seed {seed}: {Describe(new[] { invented })[0]} among {string.Join(" ", around)}");
                    }
                }

                Console.WriteLine($"{participants} participants: {Summary(totals)}");
                foreach (var entry in totals)
                {
                    Assert.AreEqual(0, entry.Value.Missed, $"{participants} participants, {entry.Key}: every event that happened is detected.");
                    Assert.AreEqual(0, entry.Value.Invented, $"{participants} participants, {entry.Key}: nothing else is detected.");
                }
            }
        }

        [TestMethod]
        public void ConfirmationDelay_RemovesTheFalseTurnTakingsOfTheBackchannels()
        {
            // Segments of up to 12 s: a backchannel can be said more than 5 s before the end
            // of the segment it is said over.
            var invented = new Dictionary<double, int>();
            int backchannels = 0;
            foreach (double delay in new[] { 0.0, 2.0, 5.0, 15.0 })
            {
                var totals = new Dictionary<string, Score> { { Without, new Score() }, { With, new Score() }, { Over, new Score() } };
                for (int seed = 1; seed <= 10; seed++)
                {
                    Conversation conversation = Conversation.Simulate(seed, 3, turns: 120, longestSegment: 12.0);
                    var configuration = new TurnTakingDetectorConfiguration { ParticipantIds = conversation.Participants, ConfirmationDelay = TimeSpan.FromSeconds(delay) };
                    Accumulate(totals, conversation.Truth, Describe(Detect(conversation.Speech, configuration)));
                }

                Console.WriteLine($"confirmation delay {delay,4:0.0} s: {Summary(totals)}");
                invented[delay] = totals[Without].Invented;
                backchannels = totals[Over].Found + totals[Over].Missed;

                Assert.AreEqual(0, totals.Values.Sum(s => s.Missed), "Whatever the delay, nothing that happened is missed.");
                Assert.AreEqual(0, totals[With].Invented + totals[Over].Invented, "In these conversations the delay only matters for the turn takings without overlap.");
            }

            Assert.IsTrue(invented[0.0] > backchannels / 2, "Without delay, most backchannels are also counted as a turn taking.");
            Assert.IsTrue(invented[2.0] < invented[0.0] && invented[5.0] < invented[2.0], "The longer the delay, the fewer.");
            Assert.AreEqual(0, invented[15.0], "A delay longer than the longest segment leaves none.");
        }

        // ------------------------------------------------------------------
        // 3. From the speech intervals to the index
        // ------------------------------------------------------------------

        [TestMethod]
        public void DetectedTurnTakings_ReachTheIndexOfTheBuilder()
        {
            Conversation conversation = Conversation.Simulate(seed: 7, participants: 3, turns: 40, longestSegment: 4.5);
            var snapshots = new List<IndexSnapshot>();
            var published = new List<(InteractionEvent Event, DateTime PublishedAt)>();
            TimeSpan window = TimeSpan.FromSeconds(20);

            IndicesHarness.Run(1, pipeline =>
            {
                var (speech, ticks) = Play(pipeline, conversation.Speech, tickPeriod: 1.0);

                var detector = new TurnTakingDetector(pipeline, new TurnTakingDetectorConfiguration { ParticipantIds = conversation.Participants });
                speech.PipeTo(detector.SpeechIn);
                ticks.PipeTo(detector.TickIn);

                // No reference value: the index is the count itself.
                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(conversation.Participants)
                    .WithWindow(window)
                    .WithCalibration(new IndexCalibration())
                    .WithClock(ticks)
                    .AddIndicator(Indicators.TurnTaking)
                    .Build();

                detector.Out.PipeTo(indices.Get(Indicators.TurnTaking).Component.EventIn);
                detector.Out.Do((e, envelope) => published.Add((e.Clone(), envelope.OriginatingTime)));
                indices.SnapshotOut.Do(snapshot => snapshots.Add(snapshot.DeepClone()));
            });

            CollectionAssert.AreEqual(conversation.Truth, Describe(published.Select(p => p.Event)), "The detector finds what happened.");
            Assert.IsTrue(snapshots.Count > 60, "One snapshot per second once the window is filled.");

            // An index counts the events that happened in its window and have been published
            // by its tick: a turn taking is published when the interval that settles it arrives.
            int compared = 0;
            TimeSpan margin = TimeSpan.FromMilliseconds(50);
            foreach (IndexSnapshot snapshot in snapshots)
            {
                DateTime tick = snapshot.OriginatingTime;
                if (published.Any(p => (p.PublishedAt - tick).Duration() < margin || (p.Event.OriginatingTime - (tick - window)).Duration() < margin))
                {
                    // Published or leaving the window at the very instant of the tick: either count is right.
                    continue;
                }

                compared++;
                foreach (string category in new[] { Without, With, Over })
                {
                    int expected = published.Count(p => p.Event.Category == category && p.PublishedAt < tick && p.Event.OriginatingTime > tick - window && p.Event.OriginatingTime <= tick);
                    Assert.AreEqual(expected, snapshot.Group[category], 1e-9, $"{category} at {(tick - SyntheticData.Start).TotalSeconds:0.0} s");
                }
            }

            Assert.IsTrue(compared > 30, $"Enough ticks compared ({compared}).");
            Assert.IsTrue(snapshots.Any(s => s.Group[Without] >= 2 && s.Group[With] >= 1 && s.Group[Over] >= 1), "The three kinds are counted.");
        }

        // ------------------------------------------------------------------
        // Support
        // ------------------------------------------------------------------

        private static InteractionInterval Speech(uint speaker, double start, double end)
            => new InteractionInterval(SyntheticData.At(start), SyntheticData.At(end), IndexCategories.Speaking, speaker);

        private static List<string> Describe(IEnumerable<InteractionEvent> events)
            => events.Select(e => $"{e.Category} {e.ParticipantId}<{e.TargetId} @{(e.OriginatingTime - SyntheticData.Start).TotalSeconds:0.0}").OrderBy(s => s).ToList();

        /// <summary>
        /// Plays speech intervals as a recogniser delivers them, each one at its end, together
        /// with a clock, from a single source so that the order is the order of the timestamps.
        /// </summary>
        private static (IProducer<InteractionInterval> Speech, IProducer<bool> Ticks) Play(Pipeline pipeline, List<InteractionInterval> speech, double tickPeriod)
        {
            var messages = speech.Select(s => (Interval: s, Time: s.EndTime)).ToList();
            DateTime end = speech.Max(s => s.EndTime) + TimeSpan.FromSeconds(40);
            for (DateTime tick = SyntheticData.Start; tick <= end; tick += TimeSpan.FromSeconds(tickPeriod))
            {
                messages.Add((null, tick));
            }

            var ordered = new List<(InteractionInterval, DateTime)>();
            DateTime last = DateTime.MinValue;
            foreach (var message in messages.OrderBy(m => m.Time).ThenBy(m => m.Interval == null ? 0 : 1))
            {
                DateTime stamped = message.Time > last ? message.Time : last.AddTicks(1);
                ordered.Add((message.Interval ?? new InteractionInterval { Category = "Tick" }, stamped));
                last = stamped;
            }

            IProducer<InteractionInterval> source = Generators.Sequence(pipeline, ordered);
            return (source.Where(m => m.Category != "Tick").Select(m => m.Clone()), source.Where(m => m.Category == "Tick").Select(_ => true));
        }

        private static List<InteractionEvent> Detect(List<InteractionInterval> speech, TurnTakingDetectorConfiguration configuration)
        {
            var detected = new List<InteractionEvent>();
            IndicesHarness.Run(1, pipeline =>
            {
                var (intervals, ticks) = Play(pipeline, speech, tickPeriod: 0.5);
                var detector = new TurnTakingDetector(pipeline, configuration);
                intervals.PipeTo(detector.SpeechIn);
                ticks.PipeTo(detector.TickIn);
                detector.Out.Do(e => detected.Add(e.Clone()));
            });

            return detected;
        }

        private static void Accumulate(Dictionary<string, Score> totals, List<string> truth, List<string> detected)
        {
            foreach (var entry in totals)
            {
                var wanted = truth.Where(t => t.StartsWith(entry.Key + " ")).ToList();
                var found = detected.Where(d => d.StartsWith(entry.Key + " ")).ToList();
                foreach (string item in wanted.ToList())
                {
                    if (found.Remove(item))
                    {
                        wanted.Remove(item);
                        entry.Value.Found++;
                    }
                }

                entry.Value.Missed += wanted.Count;
                if (wanted.Count + found.Count > 0 && entry.Value.Examples.Count < 6)
                {
                    entry.Value.Examples.Add($"missed [{string.Join(", ", wanted.Take(3))}] invented [{string.Join(", ", found.Take(3))}]");
                }
                entry.Value.Invented += found.Count;
            }
        }

        private static string Summary(Dictionary<string, Score> totals)
            => string.Join("; ", totals.Select(t => $"{t.Key}: {t.Value.Found} found, {t.Value.Missed} missed, {t.Value.Invented} invented"))
               + string.Concat(totals.SelectMany(t => t.Value.Examples.Take(3)).Select(e => Environment.NewLine + "      " + e));

        private sealed class Score
        {
            public int Found;
            public int Missed;
            public int Invented;
            public List<string> Examples = new List<string>();
        }

        /// <summary>
        /// A conversation produced by a model of the floor, so that what happened is known by
        /// construction and not by the rules of the detector.
        ///
        /// One participant holds the floor and speaks in one to three segments separated by
        /// short pauses. During a segment a listener may say a short backchannel. At the end
        /// of the turn another participant takes the floor, either after a silence or by
        /// starting before the end of the last segment.
        /// </summary>
        private sealed class Conversation
        {
            public List<uint> Participants { get; private set; }

            public List<InteractionInterval> Speech { get; } = new List<InteractionInterval>();

            public List<(string Category, uint Who, uint From, double At)> Events { get; } = new List<(string, uint, uint, double)>();

            public List<string> Truth => this.Events.Select(e => $"{e.Category} {e.Who}<{e.From} @{e.At:0.0}").OrderBy(s => s).ToList();

            public static Conversation Simulate(int seed, int participants, int turns, double longestSegment)
            {
                var random = new Random(seed);
                double Between(double low, double high) => Math.Round(low + (random.NextDouble() * (high - low)), 1);
                uint Other(uint than)
                {
                    uint pick;
                    do
                    {
                        pick = (uint)random.Next(participants);
                    }
                    while (pick == than);
                    return pick;
                }

                var conversation = new Conversation { Participants = Enumerable.Range(0, participants).Select(i => (uint)i).ToList() };
                uint holder = (uint)random.Next(participants);
                double start = 1.0;
                double firstSegmentAtLeast = 1.0;

                for (int turn = 0; turn < turns; turn++)
                {
                    int segments = 1 + random.Next(3);
                    double segmentStart = start;
                    double segmentEnd = start;
                    for (int segment = 0; segment < segments; segment++)
                    {
                        double shortest = segment == 0 ? firstSegmentAtLeast : 1.0;
                        segmentEnd = segmentStart + Between(shortest, Math.Max(shortest + 0.5, longestSegment));
                        conversation.Add(holder, segmentStart, segmentEnd);

                        // A backchannel well inside the segment: after the previous speaker has stopped when the
                        // turn began with an overlap, and away from the end where the next speaker may come in.
                        double earliest = segmentStart + (segment == 0 ? 1.2 : 0.4);
                        if (segmentEnd - 2.2 >= earliest && random.NextDouble() < 0.4)
                        {
                            uint listener = Other(holder);
                            double from = Between(earliest, segmentEnd - 2.2);
                            double to = from + Between(0.3, 0.7);
                            conversation.Add(listener, from, to);
                            conversation.Events.Add((Over, listener, holder, from));
                        }

                        if (segment < segments - 1)
                        {
                            segmentStart = segmentEnd + Between(0.2, 0.7);
                        }
                    }

                    if (turn == turns - 1)
                    {
                        // The conversation ends with this turn: nobody takes the floor after it.
                        break;
                    }

                    uint next = Other(holder);
                    if (random.NextDouble() < 0.7)
                    {
                        // Smooth switch.
                        start = segmentEnd + Between(0.2, 2.0);
                        firstSegmentAtLeast = 1.0;
                        conversation.Events.Add((Without, next, holder, start));
                    }
                    else
                    {
                        // Overlapping switch: the next speaker starts before the end and goes on after it.
                        double overlap = Between(0.2, Math.Min(0.9, segmentEnd - segmentStart - 0.2));
                        start = segmentEnd - overlap;
                        firstSegmentAtLeast = overlap + 0.5;
                        conversation.Events.Add((With, next, holder, start));
                    }

                    holder = next;
                }

                return conversation;
            }

            private void Add(uint speaker, double start, double end)
                => this.Speech.Add(new InteractionInterval(SyntheticData.At(start), SyntheticData.At(end), IndexCategories.Speaking, speaker));
        }
    }
}
