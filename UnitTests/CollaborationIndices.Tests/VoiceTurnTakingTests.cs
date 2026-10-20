// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.CollaborationIndices;
using SAAC.Groups;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// The turn takings found on the voice activity as it unfolds. The conversations are
    /// written, so what happened is known; the voice activity of each participant is a
    /// sample every 20 ms, and what the detector publishes, and when, is compared with it.
    /// An intervention is a second of voice at least; a pause under 0.3 s does not end it.
    /// </summary>
    [TestClass]
    public class VoiceTurnTakingTests
    {
        private const uint A = 0;
        private const uint B = 1;
        private const uint C = 2;
        private const string Without = IndexCategories.TurnTakingWithoutOverlap;
        private const string With = IndexCategories.TurnTakingWithOverlap;
        private const string Over = IndexCategories.Overlap;

        [TestMethod]
        public void ATurnTakenInASilence_IsKnownASecondAfterItStarted()
        {
            // A speaks, then B after a second of silence, then A again after four seconds.
            var found = Detect(Voices(16, (A, 1, 4), (B, 5, 8), (A, 12, 15))).Found;

            CollectionAssert.AreEqual(new[] { $"{Without} 1<-0 @5.00", $"{Without} 0<-1 @12.00" }, found.Select(Describe).ToList(), "The first speaker of the session takes the turn from nobody.");
            Assert.AreEqual(6.0, found[0].Published, 0.05, "Known once the voice has lasted a second.");
            Assert.AreEqual(13.0, found[1].Published, 0.05, "The length of the silence before does not matter.");
        }

        [TestMethod]
        public void TheSameParticipantResuming_TakesNoTurn()
        {
            // A speaks, stops for two seconds and goes on; inside its speech, a pause of 0.2 s.
            var found = Detect(Voices(12, (A, 1, 2), (A, 2.2, 3), (A, 5, 7), (B, 8, 10))).Found;

            CollectionAssert.AreEqual(new[] { $"{Without} 1<-0 @8.00" }, found.Select(Describe).ToList(), "Only B takes a turn.");
        }

        [TestMethod]
        public void SpeakingOverSomebodyWhoStops_TakesTheTurnWithOverlap()
        {
            // B starts while A speaks, and A stops a second later.
            var found = Detect(Voices(10, (A, 1, 5), (B, 4, 8))).Found;

            CollectionAssert.AreEqual(new[] { $"{With} 1<-0 @4.00" }, found.Select(Describe).ToList());
            Assert.AreEqual(5.3, found[0].Published, 0.1, "Known when A has stopped for longer than a pause.");
        }

        [TestMethod]
        public void SpeakingOverSomebodyWhoGoesOn_IsAnOverlap()
        {
            // B says something over A, who keeps the floor.
            var found = Detect(Voices(12, (A, 1, 10), (B, 3, 4.5))).Found;

            CollectionAssert.AreEqual(new[] { $"{Over} 1<-0 @3.00" }, found.Select(Describe).ToList(), "B takes no turn, and A takes none when it goes on.");
            Assert.AreEqual(4.8, found[0].Published, 0.1, "Known when B has stopped.");
        }

        [TestMethod]
        public void VoiceTooShortToBeAnIntervention_IsIgnored()
        {
            // B makes a sound of 0.6 s in the silence, and another one over C: neither takes nor holds the floor.
            var found = Detect(Voices(12, (A, 1, 4), (B, 5, 5.6), (C, 6.5, 10), (B, 8, 8.6))).Found;

            CollectionAssert.AreEqual(new[] { $"{Without} 2<-0 @6.50" }, found.Select(Describe).ToList(), "C takes the turn from A, the last to have spoken for good.");

            // The length that makes an intervention is a setting.
            found = Detect(Voices(12, (A, 1, 4), (B, 5, 5.6), (C, 6.5, 10)), configuration => configuration.MinimumSpeechDuration = TimeSpan.FromSeconds(0.5)).Found;
            CollectionAssert.AreEqual(new[] { $"{Without} 1<-0 @5.00", $"{Without} 2<-1 @6.50" }, found.Select(Describe).ToList());
            Assert.AreEqual(5.5, found[0].Published, 0.05);
        }

        [TestMethod]
        public void WithSeveralParticipantsSpeaking_TheTurnIsTakenFromTheLastToHaveStarted()
        {
            // A speaks, B joins and outlasts it, C joins while both speak and outlasts them.
            var found = Detect(Voices(12, (A, 1, 5.5), (B, 3, 6), (C, 5, 9))).Found;

            CollectionAssert.AreEquivalent(new[] { $"{With} 1<-0 @3.00", $"{With} 2<-1 @5.00" }, found.Select(Describe).ToList());
        }

        [TestMethod]
        public void AMaximumSilenceCanBeSet()
        {
            Timeline Conversation() => Voices(16, (A, 1, 3), (B, 10, 12));

            Assert.AreEqual(1, Detect(Conversation()).Found.Count, "No limit by default.");
            Assert.AreEqual(0, Detect(Conversation(), configuration => configuration.MaximumSilence = TimeSpan.FromSeconds(3)).Found.Count);
        }

        [TestMethod]
        public void AnInterventionTheTranscriptionDoesNotConfirm_IsWithdrawn()
        {
            // A and C speak and are transcribed; in between, the microphone of B hears two seconds of something that is not speech.
            Timeline Conversation() => Voices(18, (A, 1, 4), (B, 5, 7), (C, 14, 16))
                .Interval("Speech", 1, 4, IndexCategories.Speaking, A)
                .Interval("Speech", 14, 16, IndexCategories.Speaking, C);

            var validated = Detect(Conversation(), transcription: true);
            CollectionAssert.AreEqual(new[] { $"{Without} 1<-0 @5.00", $"{Without} 2<-0 @14.00" }, validated.Found.Select(Describe).ToList(), "Published at once; then B no longer counts as whose turn it was.");
            CollectionAssert.AreEqual(new[] { $"{Without} 1<-0 @5.00" }, validated.Cancelled.Select(cancelled => Describe((cancelled.Event, cancelled.Published))).ToList());
            Assert.AreEqual(12.0, validated.Cancelled[0].Published, 0.1, "Withdrawn five seconds after the end of the intervention.");

            // Without the validation the voice activity alone decides.
            var alone = Detect(Conversation());
            CollectionAssert.AreEqual(new[] { $"{Without} 1<-0 @5.00", $"{Without} 2<-1 @14.00" }, alone.Found.Select(Describe).ToList());
            Assert.AreEqual(0, alone.Cancelled.Count);
        }

        [TestMethod]
        public void AParticipantWhoseVoiceActivityStops_DoesNotHoldTheOthersBack()
        {
            // The stream of C ends after two seconds: the turn of B is still found.
            var found = new List<(InteractionEvent Event, double Published)>();
            IndicesHarness.Run(1, pipeline =>
            {
                Timeline timeline = Voices(12, (A, 1, 4), (B, 5, 8)).Play(pipeline);
                var detector = new VoiceTurnTakingDetector(pipeline, new VoiceTurnTakingDetectorConfiguration { ParticipantIds = new List<uint> { A, B, C } });
                timeline.NumbersOf("Voice0").Select(value => value > 0.5).PipeTo(detector.GetVoiceInput(A));
                timeline.NumbersOf("Voice1").Select(value => value > 0.5).PipeTo(detector.GetVoiceInput(B));
                timeline.NumbersOf("Voice2").Where((value, envelope) => envelope.OriginatingTime < SyntheticData.At(2)).Select(value => value > 0.5).PipeTo(detector.GetVoiceInput(C));
                detector.Out.Do((detected, envelope) => found.Add((detected.Clone(), Seconds(envelope.OriginatingTime))));
            });

            CollectionAssert.AreEqual(new[] { $"{Without} 1<-0 @5.00" }, found.Select(Describe).ToList());
        }

        [TestMethod]
        public void AWithdrawnTurnTaking_IsNoLongerCounted()
        {
            var counts = new List<(double Seconds, double Count)>();
            IndicesHarness.Run(1, pipeline =>
            {
                // A turn taking at 6 s, withdrawn at 8.5 s; a window of 5 s and a tick per second.
                Timeline timeline = new Timeline().Ticks("Clock", 0.5, 14, 1)
                    .Event("Turns", 6.0, Without, B, A)
                    .Number("Cancel", 8.6, 1)
                    .Play(pipeline);

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(A, B)
                    .WithWindow(TimeSpan.FromSeconds(5))
                    .WithClock(timeline.TicksOf("Clock"))
                    .AddIndicator(Indicators.TurnTaking)
                    .Build();
                TurnTakingComponent component = indices.Get(Indicators.TurnTaking).Component;
                timeline.EventsOf("Turns").PipeTo(component.EventIn);
                timeline.NumbersOf("Cancel").Select(_ => new InteractionEvent(SyntheticData.At(6.0), Without, B, A)).PipeTo(component.CancelIn);
                component.GetGroupEmitter(Without).Do((value, envelope) => counts.Add((Seconds(envelope.OriginatingTime), value)));
            });

            Assert.IsTrue(counts.Where(count => count.Seconds > 6 && count.Seconds < 8.6).All(count => count.Count > 0), "Counted while it stands.");
            Assert.IsTrue(counts.Any(count => count.Seconds > 6 && count.Seconds < 8.6));
            Assert.IsTrue(counts.Where(count => count.Seconds > 8.6).All(count => count.Count == 0), "No longer counted once withdrawn, though it is still in the window.");
            Assert.IsTrue(counts.Any(count => count.Seconds > 8.6 && count.Seconds < 11));
        }

        [TestMethod]
        public void AWithdrawnDetection_IsNoLongerInTheStateOfTheInstant()
        {
            var states = new List<(double Seconds, GroupInstant State)>();
            IndicesHarness.Run(1, pipeline =>
            {
                Timeline timeline = new Timeline().Ticks("Clock", 0.05, 6, 0.1)
                    .Event("Detection", 2.0, Without, B, A)
                    .Number("Cancel", 3.02, 1)
                    .Play(pipeline);

                var instant = new InstantStateComponent(pipeline, new InstantStateConfiguration { ParticipantIds = new List<uint> { A, B } });
                timeline.TicksOf("Clock").PipeTo(instant.TickIn);
                timeline.EventsOf("Detection").PipeTo(instant.DetectionIn);
                timeline.NumbersOf("Cancel").Select(_ => new InteractionEvent(SyntheticData.At(2.0), Without, B, A)).PipeTo(instant.CancelledDetectionIn);
                instant.Out.Do((state, envelope) => states.Add((Seconds(envelope.OriginatingTime), state.DeepClone())));
            });

            Assert.IsTrue(states.Where(state => state.Seconds > 2.1 && state.Seconds < 3).All(state => state.State.Events.Count == 1), "Listed while it stands.");
            Assert.IsTrue(states.Where(state => state.Seconds > 3.1).All(state => state.State.Events.Count == 0), "Not listed once withdrawn, well before the four seconds an event stays.");
            Assert.IsTrue(states.Any(state => state.Seconds > 3.1 && state.Seconds < 5));
        }

        // ------------------------------------------------------------------

        private static double Seconds(DateTime time) => Math.Round((time - SyntheticData.Start).TotalSeconds, 3);

        private static string Describe((InteractionEvent Event, double Published) found)
            => $"{found.Event.Category} {found.Event.ParticipantId}<-{found.Event.TargetId} @{Seconds(found.Event.OriginatingTime).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}";

        // The voice activity of the three participants: a sample every 20 ms, true inside the spans given.
        private static Timeline Voices(double seconds, params (uint Who, double From, double To)[] spans)
        {
            var timeline = new Timeline();
            for (int i = 0; i * 0.02 < seconds; i++)
            {
                double time = Math.Round(i * 0.02, 2);
                foreach (uint who in new[] { A, B, C })
                {
                    bool voice = spans.Any(span => span.Who == who && time >= span.From - 1e-9 && time < span.To - 1e-9);
                    timeline.Number("Voice" + who, time, voice ? 1 : 0);
                }
            }

            return timeline;
        }

        private static (List<(InteractionEvent Event, double Published)> Found, List<(InteractionEvent Event, double Published)> Cancelled) Detect(
            Timeline timeline, Action<VoiceTurnTakingDetectorConfiguration> configure = null, bool transcription = false)
        {
            var found = new List<(InteractionEvent Event, double Published)>();
            var cancelled = new List<(InteractionEvent Event, double Published)>();
            IndicesHarness.Run(1, pipeline =>
            {
                // A stream of the timeline exists once something is on it.
                timeline.Interval("Speech", -2, -1, IndexCategories.Speaking, 99).Play(pipeline);
                var configuration = new VoiceTurnTakingDetectorConfiguration { ParticipantIds = new List<uint> { A, B, C }, ValidateWithTranscription = transcription };
                configure?.Invoke(configuration);
                var detector = new VoiceTurnTakingDetector(pipeline, configuration);
                foreach (uint who in new[] { A, B, C })
                {
                    timeline.NumbersOf("Voice" + who).Select(value => value > 0.5).PipeTo(detector.GetVoiceInput(who));
                }

                timeline.IntervalsOf("Speech").PipeTo(detector.SpeechIn);
                detector.Out.Do((detected, envelope) => found.Add((detected.Clone(), Seconds(envelope.OriginatingTime))));
                detector.CancelledOut.Do((detected, envelope) => cancelled.Add((detected.Clone(), Seconds(envelope.OriginatingTime))));
            });

            return (found, cancelled);
        }
    }
}
