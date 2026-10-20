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
    /// Silence, cross-talk and mutual gaze, on scenarios whose expected values are worked out
    /// by hand.
    /// </summary>
    [TestClass]
    public class SilenceCrossTalkMutualGazeTests
    {
        private const uint A = 0;
        private const uint B = 1;
        private const uint C = 2;
        private static readonly List<uint> Trio = new List<uint> { A, B, C };
        private static readonly TimeSpan Window = TimeSpan.FromSeconds(10);

        // A speaks from 12 to 20 and again from 19 to 21 (two intervals of one speech);
        // B from 14 to 16 and from 18 to 22; C from 15 to 19.
        private static readonly (uint Speaker, double Start, double End)[] Conversation =
        {
            (A, 12, 20), (A, 19, 21), (B, 14, 16), (B, 18, 22), (C, 15, 19),
        };

        [TestMethod]
        public void Measure_CountsEachInstantOnce()
        {
            DateTime start = SyntheticData.At(0);
            DateTime end = SyntheticData.At(20);
            var speech = new[]
            {
                Intervals((2, 10)),             // A
                Intervals((4, 6), (8, 12)),     // B
                Intervals((5, 9)),              // C
            };

            // Speakers: 0 until 2, 1 until 4, 2 until 5, 3 until 6, 2 until 8, 3 until 9, 2 until 10, 1 until 12, then 0.
            Assert.AreEqual(10.0, SimultaneousSpeechComponent.Measure(speech, start, end, 0, 0), 1e-9, "Nobody speaks before 2 and after 12.");
            Assert.AreEqual(6.0, SimultaneousSpeechComponent.Measure(speech, start, end, 2, int.MaxValue), 1e-9, "At least two speak from 4 to 10, three of them or not.");
            Assert.AreEqual(4.0, SimultaneousSpeechComponent.Measure(speech, start, end, 1, 1), 1e-9, "The three measures share the window.");
            Assert.AreEqual(2.0, SimultaneousSpeechComponent.Measure(speech, start, end, 3, 3), 1e-9);

            // Two participants: both speak from 4 to 6 and from 8 to 10.
            Assert.AreEqual(4.0, SimultaneousSpeechComponent.Measure(new[] { speech[0], speech[1] }, start, end, 2, 2), 1e-9);
            Assert.AreEqual(20.0, SimultaneousSpeechComponent.Measure(new List<KeyValuePair<DateTime, DateTime>>[0], start, end, 0, 0), 1e-9, "Without speech the window is silent.");
        }

        [TestMethod]
        public void SilenceAndCrossTalk_AreTimesOfTheSlidingWindow()
        {
            var silence = new Dictionary<double, double>();
            var crossTalk = new Dictionary<double, double>();
            var crossTalkRatio = new Dictionary<double, double>();
            var crossTalkPairs = new Dictionary<double, Dictionary<ParticipantPair, double>>();
            var silenceOutputs = new List<string>();

            IndicesHarness.Run(1, pipeline =>
            {
                Scenario scenario = SpeechScenario().Play(pipeline);

                // Turn taking is declared too: it has a silence output of its own, which the
                // Silence indicator replaces.
                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(Trio)
                    .WithWindow(Window)
                    .WithClock(scenario.Ticks)
                    .AddIndicator(Indicators.TurnTaking)
                    .AddIndicator(Indicators.Silence)
                    .AddIndicator(Indicators.CrossTalk)
                    .AddIndicator(Indicators.CrossTalk, options =>
                    {
                        options.Name = "CrossTalkRatio";
                        options.AsRatioOfWindow = true;
                    })
                    .Build();

                foreach (uint id in Trio)
                {
                    IProducer<InteractionInterval> speech = scenario.Speech(id);
                    speech.PipeTo(indices.Get(Indicators.Silence).Component.GetIntervalInput(id));
                    speech.PipeTo(indices.Get(Indicators.CrossTalk).Component.GetIntervalInput(id));
                    speech.PipeTo(indices.Get<CrossTalkIndicator>("CrossTalkRatio").Component.GetIntervalInput(id));
                }

                indices.Outputs.Group(IndexNames.Silence).Do((value, envelope) => silence[Seconds(envelope.OriginatingTime)] = value);
                indices.Outputs.Group(IndexNames.CrossTalk).Do((value, envelope) => crossTalk[Seconds(envelope.OriginatingTime)] = value);
                indices.Outputs.Group("CrossTalkRatio").Do((value, envelope) => crossTalkRatio[Seconds(envelope.OriginatingTime)] = value);
                indices.Outputs.Pair(IndexNames.CrossTalk).Do((value, envelope) => crossTalkPairs[Seconds(envelope.OriginatingTime)] = new Dictionary<ParticipantPair, double>(value));
                silenceOutputs.AddRange(indices.Outputs.Groups.Where(output => output.Name == IndexNames.Silence).Select(output => output.IndicatorName));
            });

            CollectionAssert.AreEqual(new[] { IndexNames.Silence }, silenceOutputs, "One silence output, the one of the Silence indicator.");

            // One window of warm-up, then one value per tick.
            Assert.AreEqual(10.5, silence.Keys.Min());
            Assert.AreEqual(39.5, silence.Keys.Max());
            Assert.AreEqual(10.0, silence[10.5], 1e-9, "Nobody has spoken yet.");

            // At 20.5 the window is [10.5, 20.5]. Received: A 12-20, B 14-16, C 15-19; the
            // speeches that end at 21 and 22 are not known yet.
            // Speakers: 0 until 12, 1 until 14, 2 until 15, 3 until 16, 2 until 19, 1 until 20, 0 until 20.5.
            Assert.AreEqual(2.0, silence[20.5], 1e-9);
            Assert.AreEqual(5.0, crossTalk[20.5], 1e-9);
            Assert.AreEqual(2.0, crossTalkPairs[20.5][new ParticipantPair(A, B)], 1e-9);
            Assert.AreEqual(4.0, crossTalkPairs[20.5][new ParticipantPair(A, C)], 1e-9);
            Assert.AreEqual(1.0, crossTalkPairs[20.5][new ParticipantPair(B, C)], 1e-9);

            // At 25.5 the window is [15.5, 25.5] and everything is known. The two intervals of
            // A are one speech, until 21.
            // Speakers: 3 until 16, 2 until 18, 3 until 19, 2 until 21, 1 until 22, 0 until 25.5.
            Assert.AreEqual(3.5, silence[25.5], 1e-9);
            Assert.AreEqual(5.5, crossTalk[25.5], 1e-9);
            Assert.AreEqual(0.55, crossTalkRatio[25.5], 1e-9, "The same time as a share of the window.");
            Assert.AreEqual(3.5, crossTalkPairs[25.5][new ParticipantPair(A, B)], 1e-9);
            Assert.AreEqual(3.5, crossTalkPairs[25.5][new ParticipantPair(A, C)], 1e-9);
            Assert.AreEqual(1.5, crossTalkPairs[25.5][new ParticipantPair(B, C)], 1e-9);

            // Once the conversation has left the window.
            Assert.AreEqual(10.0, silence[35.5], 1e-9);
            Assert.AreEqual(0.0, crossTalk[35.5], 1e-9);

            // Whatever the tick, nobody speaks or somebody does, and several cannot speak longer than somebody.
            foreach (double tick in silence.Keys)
            {
                Assert.IsTrue(silence[tick] >= 0 && silence[tick] <= 10.0 + 1e-9, $"Silence at {tick}");
                Assert.IsTrue(crossTalk[tick] >= 0 && crossTalk[tick] <= 10.0 - silence[tick] + 1e-9, $"Cross-talk at {tick}");
            }
        }

        [TestMethod]
        public void MutualGaze_IsTheTimeTwoLooksAtEachOtherOverlap()
        {
            InteractionInterval aAtB = Look(A, B, 1.0, 4.0);

            InteractionInterval mutual = MutualGazeDetector.MutualGaze(aAtB, Look(B, A, 2.0, 3.0), TimeSpan.Zero);
            Assert.IsNotNull(mutual);
            Assert.AreEqual(2.0, Seconds(mutual.StartTime));
            Assert.AreEqual(3.0, Seconds(mutual.EndTime));
            Assert.AreEqual(A, mutual.ParticipantId);
            Assert.AreEqual(B, mutual.TargetId);

            mutual = MutualGazeDetector.MutualGaze(Look(B, A, 3.5, 6.0), aAtB, TimeSpan.Zero);
            Assert.AreEqual(3.5, Seconds(mutual.StartTime), "From the start of the later look.");
            Assert.AreEqual(4.0, Seconds(mutual.EndTime), "To the end of the earlier one.");
            Assert.AreEqual(A, mutual.ParticipantId, "The pair, whoever looked last.");

            Assert.IsNull(MutualGazeDetector.MutualGaze(aAtB, Look(B, A, 4.0, 5.0), TimeSpan.Zero), "One look ends when the other starts.");
            Assert.IsNull(MutualGazeDetector.MutualGaze(aAtB, Look(B, C, 2.0, 3.0), TimeSpan.Zero), "B looks at somebody else.");
            Assert.IsNull(MutualGazeDetector.MutualGaze(aAtB, Look(C, B, 2.0, 3.0), TimeSpan.Zero), "Two participants look at the same third one.");
            Assert.IsNull(MutualGazeDetector.MutualGaze(aAtB, Look(A, B, 2.0, 3.0), TimeSpan.Zero), "Two looks of the same participant.");
            Assert.IsNull(MutualGazeDetector.MutualGaze(aAtB, Look(B, A, 2.0, 3.0), TimeSpan.FromSeconds(1.5)), "Shorter than the minimum.");
        }

        [TestMethod]
        public void MutualGazes_AreDetectedAndCountedOverTheWindow()
        {
            var detected = new List<InteractionEvent>();
            var longEnough = new List<InteractionEvent>();
            var group = new Dictionary<double, double>();
            var pairs = new Dictionary<double, Dictionary<ParticipantPair, double>>();

            IndicesHarness.Run(1, pipeline =>
            {
                // A look is published when it ends.
                Scenario scenario = new Scenario()
                    .Look(A, B, 1.0, 4.0)
                    .Look(B, A, 2.0, 3.0)       // inside the look of A: mutual from 2 to 3
                    .Look(B, A, 3.5, 6.0)       // meets the same look of A: mutual from 3.5 to 4
                    .Look(A, C, 5.0, 5.5)
                    .Look(C, A, 5.2, 5.4)       // mutual from 5.2 to 5.4
                    .Look(B, C, 7.0, 8.0)
                    .Look(C, B, 8.0, 9.0)       // starts when the other ends: no mutual gaze
                    .Look(A, B, 9.0, 9.5)       // nobody looks back
                    .WithTicks(0.5, 30)
                    .Play(pipeline);

                var detector = new MutualGazeDetector(pipeline, new MutualGazeDetectorConfiguration { ParticipantIds = Trio });
                var strict = new MutualGazeDetector(pipeline, new MutualGazeDetectorConfiguration { ParticipantIds = Trio, MinimumDuration = TimeSpan.FromSeconds(0.3) }, "Strict");
                scenario.Looks.PipeTo(detector.PeerGazeIn);
                scenario.Looks.PipeTo(strict.PeerGazeIn);
                detector.Out.Do(mutual => detected.Add(mutual.Clone()));
                strict.Out.Do(mutual => longEnough.Add(mutual.Clone()));

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(Trio)
                    .WithWindow(Window)
                    .WithClock(scenario.Ticks)
                    .AddIndicator(Indicators.MutualGaze)
                    .Build();

                detector.Out.PipeTo(indices.Get(Indicators.MutualGaze).Component.EventIn);
                indices.Outputs.Group(IndexNames.MutualGaze).Do((value, envelope) => group[Seconds(envelope.OriginatingTime)] = value);
                indices.Outputs.Pair(IndexNames.MutualGaze).Do((value, envelope) => pairs[Seconds(envelope.OriginatingTime)] = new Dictionary<ParticipantPair, double>(value));
            });

            // Each mutual gaze once, with its two participants and the time it ended.
            CollectionAssert.AreEqual(
                new[] { "0-1@3", "0-1@4", "0-2@5.4" },
                detected.OrderBy(mutual => mutual.OriginatingTime).Select(Describe).ToArray());
            Assert.IsTrue(detected.All(mutual => mutual.Category == IndexCategories.MutualGaze));
            CollectionAssert.AreEqual(
                new[] { "0-1@3", "0-1@4" },
                longEnough.OrderBy(mutual => mutual.OriginatingTime).Select(Describe).ToArray(),
                "The mutual gaze of 0.2 s is shorter than the minimum of 0.3 s.");

            // At 10.5 the window is [0.5, 10.5]: the three of them.
            Assert.AreEqual(3.0, group[10.5], 1e-9);
            Assert.AreEqual(2.0, pairs[10.5][new ParticipantPair(A, B)], 1e-9);
            Assert.AreEqual(1.0, pairs[10.5][new ParticipantPair(A, C)], 1e-9);
            Assert.AreEqual(0.0, pairs[10.5][new ParticipantPair(B, C)], 1e-9);

            // At 13.5 the window is [3.5, 13.5]: the one that ended at 3 has left it.
            Assert.AreEqual(2.0, group[13.5], 1e-9);
            Assert.AreEqual(1.0, pairs[13.5][new ParticipantPair(A, B)], 1e-9);

            // At 14.5 the window is [4.5, 14.5]; at 16.5 nothing is left.
            Assert.AreEqual(1.0, group[14.5], 1e-9);
            Assert.AreEqual(1.0, pairs[14.5][new ParticipantPair(A, C)], 1e-9);
            Assert.AreEqual(0.0, group[16.5], 1e-9);
        }

        [TestMethod]
        public void Template_DeclaresTheThreeIndicators()
        {
            CollaborationSessionConfiguration template = CollaborationSessionConfiguration.Template();
            Assert.IsTrue(template.Has(IndexNames.Silence) && template.Has(IndexNames.CrossTalk) && template.Has(IndexNames.MutualGaze));

            using (Pipeline pipeline = Pipeline.Create())
            {
                SlidingAverageComputationSet indices = template.CreateBuilder(pipeline).BuildSet(template.Windows.ToArray());
                indices.ForEach(instance =>
                {
                    Assert.AreEqual(1, instance.Outputs.Groups.Count(output => output.Name == IndexNames.Silence));
                    Assert.IsNotNull(instance.Get(Indicators.Silence).Component);
                    Assert.IsNotNull(instance.Get(Indicators.CrossTalk).Component);
                    Assert.IsNotNull(instance.Get(Indicators.MutualGaze).Component);
                });
            }
        }

        private static Scenario SpeechScenario()
        {
            var scenario = new Scenario();
            foreach ((uint speaker, double start, double end) in Conversation)
            {
                scenario.Speech(speaker, start, end);
            }

            return scenario.WithTicks(0.5, 40);
        }

        private static List<KeyValuePair<DateTime, DateTime>> Intervals(params (double Start, double End)[] seconds)
            => seconds.Select(s => new KeyValuePair<DateTime, DateTime>(SyntheticData.At(s.Start), SyntheticData.At(s.End))).ToList();

        private static InteractionInterval Look(uint gazer, uint gazed, double start, double end)
            => new InteractionInterval(SyntheticData.At(start), SyntheticData.At(end), IndexCategories.GazeOnPeer, gazer, gazed);

        private static double Seconds(DateTime time) => Math.Round((time - SyntheticData.Start).TotalSeconds, 3);

        private static string Describe(InteractionEvent mutual)
            => $"{mutual.ParticipantId}-{mutual.TargetId}@{Seconds(mutual.OriginatingTime).ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        /// <summary>
        /// Intervals and ticks played from a single source, so that they reach the components
        /// in the order of their timestamps. An interval is published when it ends.
        /// </summary>
        private sealed class Scenario
        {
            private readonly List<(Message, DateTime)> messages = new List<(Message, DateTime)>();
            private IProducer<Message> source;

            public IProducer<bool> Ticks => this.source.Where(message => message.Interval == null).Select(_ => true);

            public IProducer<InteractionInterval> Looks => this.source.Where(message => message.Interval != null && message.Interval.Category == IndexCategories.GazeOnPeer).Select(message => message.Interval);

            public Scenario Speech(uint speaker, double start, double end)
                => this.Add(new InteractionInterval(SyntheticData.At(start), SyntheticData.At(end), IndexCategories.Speaking, speaker), end);

            public Scenario Look(uint gazer, uint gazed, double start, double end)
                => this.Add(SilenceCrossTalkMutualGazeTests.Look(gazer, gazed, start, end), end);

            public Scenario WithTicks(double first, double last)
            {
                for (double tick = first; tick <= last; tick += 1.0)
                {
                    this.messages.Add((new Message(), SyntheticData.At(tick)));
                }

                return this;
            }

            public Scenario Play(Pipeline pipeline)
            {
                // Two messages cannot share a timestamp on one stream: nudge the later ones by a tick each.
                var ordered = new List<(Message, DateTime)>();
                DateTime last = DateTime.MinValue;
                foreach ((Message message, DateTime time) in this.messages.OrderBy(m => m.Item2))
                {
                    DateTime stamped = time > last ? time : last.AddTicks(1);
                    ordered.Add((message, stamped));
                    last = stamped;
                }

                this.source = Generators.Sequence(pipeline, ordered);
                return this;
            }

            public IProducer<InteractionInterval> Speech(uint speaker)
                => this.source.Where(message => message.Interval != null && message.Interval.Category == IndexCategories.Speaking && message.Interval.ParticipantId == speaker).Select(message => message.Interval);

            private Scenario Add(InteractionInterval interval, double publishedAt)
            {
                this.messages.Add((new Message { Interval = interval }, SyntheticData.At(publishedAt)));
                return this;
            }

            private sealed class Message
            {
                public InteractionInterval Interval { get; set; }
            }
        }
    }
}
