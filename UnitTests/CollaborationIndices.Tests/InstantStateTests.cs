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
using SAAC.Groups;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// The state of the group at each instant (GroupInstant of the Groups component), made
    /// to be drawn: places, voices, looks, formations and
    /// objects in hand as they are at the tick, and the detections of the last seconds.
    /// </summary>
    [TestClass]
    public class InstantStateTests
    {
        [TestMethod]
        public void State_IsWhatEachSignalSaysAtTheTick()
        {
            var states = new List<(double Seconds, GroupInstant State)>();

            IndicesHarness.Run(1, pipeline =>
            {
                var timeline = new Timeline().Ticks("Clock", 0.05, 8, 0.1);

                // Participants 0 and 1 face each other, two metres apart; participant 2 is not tracked.
                for (int i = 0; i <= 80; i++)
                {
                    double t = i * 0.1;
                    timeline.Position("Head0", t + 0.001, new Vector3(0, 1.7f, 0));
                    timeline.Position("Head1", t + 0.002, new Vector3(2, 1.7f, 0));

                    // Participant 0 speaks from 1 s to 2 s.
                    timeline.Number("Voice0", t + 0.003, t >= 1.0 && t < 2.0 ? 1 : 0);
                    timeline.Number("Voice1", t + 0.004, 0);
                }

                // 0 looks at 1 from 1 s to 3 s, 1 looks back from 1.5 s; 2 glances at 0 for a tenth of a second.
                timeline.Event("LookStart", 1.0, IndexCategories.GazeOnPeer, 0, 1);
                timeline.Event("LookStart", 1.5, IndexCategories.GazeOnPeer, 1, 0);
                timeline.Event("LookStart", 2.0, IndexCategories.GazeOnPeer, 2, 0);
                timeline.Event("LookEnd", 2.1, IndexCategories.GazeOnPeer, 2, 0);
                timeline.Event("LookEnd", 3.0, IndexCategories.GazeOnPeer, 0, 1);
                timeline.Event("LookEnd", 3.3, IndexCategories.GazeOnPeer, 1, 0);

                // Both look at the cube from 3.5 s to 4.5 s; the detector names the joint attention when the looks end.
                timeline.Event("LookStart", 3.5, DetectionCategories.GazeOnObject, 0, null, "Cube");
                timeline.Event("LookStart", 3.6, DetectionCategories.GazeOnObject, 1, null, "Cube");
                timeline.Event("LookEnd", 4.5, DetectionCategories.GazeOnObject, 0, null, "Cube");
                timeline.Event("LookEnd", 4.51, DetectionCategories.GazeOnObject, 1, null, "Cube");
                timeline.Event("Detection", 4.52, IndexCategories.JointVisualAttention, 0, 1, "Cube");

                // Face to face from 1.2 s to 4 s.
                timeline.Event("FormationStart", 1.2, DetectionCategories.FormationStart, 0, 1, FormationTypes.FaceToFace);
                timeline.Event("FormationEnd", 4.0, IndexCategories.FormationEnd, 0, 1, FormationTypes.FaceToFace);

                // Participant 1 holds a piece from 2 s to 3 s.
                timeline.Event("Task", 2.0, IndexCategories.Grab, 1, null, "Piece_3@Table");
                timeline.Event("Task", 3.0, IndexCategories.Place, 1, null, "Piece_3@Board");
                timeline.Play(pipeline);

                var instant = new InstantStateComponent(pipeline, new InstantStateConfiguration { ParticipantIds = new List<uint> { 0, 1, 2 } });
                timeline.PositionsOf("Head0").Select(position => Tuple.Create(position, new Vector3(1, 0, 0))).PipeTo(instant.GetHeadPoseInput(0));
                timeline.PositionsOf("Head1").Select(position => Tuple.Create(position, new Vector3(-1, 0, 0))).PipeTo(instant.GetHeadPoseInput(1));
                timeline.NumbersOf("Voice0").Select(value => value > 0.5).PipeTo(instant.GetVoiceInput(0));
                timeline.NumbersOf("Voice1").Select(value => value > 0.5).PipeTo(instant.GetVoiceInput(1));
                timeline.EventsOf("LookStart").PipeTo(instant.LookStartIn);
                timeline.EventsOf("LookEnd").PipeTo(instant.LookEndIn);
                timeline.EventsOf("FormationStart").PipeTo(instant.FormationStartIn);
                timeline.EventsOf("FormationEnd").PipeTo(instant.FormationEndIn);
                timeline.EventsOf("Task").PipeTo(instant.TaskActionIn);
                timeline.EventsOf("Detection").PipeTo(instant.DetectionIn);
                timeline.TicksOf("Clock").PipeTo(instant.TickIn);
                instant.Out.Do((state, envelope) => states.Add(((envelope.OriginatingTime - SyntheticData.Start).TotalSeconds, state.DeepClone())));
            });

            GroupInstant At(double seconds) => states.OrderBy(state => Math.Abs(state.Seconds - seconds)).First().State;
            Assert.IsTrue(states.Count >= 79, $"A state per tick ({states.Count}).");

            // Places and headings: on the floor, whatever the height of the heads.
            GroupInstant state = At(1.05);
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, state.Persons.Select(person => person.Id).ToList());
            PersonInstant first = state.FindPerson("1"), second = state.FindPerson("2"), third = state.FindPerson("3");
            Assert.IsTrue(first.HasPlace && first.X == 0 && first.Y == 0 && first.HeadingX == 1 && first.HeadingY == 0);
            Assert.IsTrue(second.HasPlace && second.X == 2 && second.Y == 0 && second.HeadingX == -1);
            Assert.IsFalse(third.HasPlace, "A participant whose head is not tracked has no place.");

            // The voice: at once.
            Assert.IsTrue(first.Speaking && !second.Speaking && !third.Speaking, "At 1.05 s participant 1 is speaking.");
            Assert.IsFalse(At(0.95).FindPerson("1").Speaking);
            Assert.IsFalse(At(2.05).FindPerson("1").Speaking);

            // A look is part of the state once it has lasted 200 ms, until it ends.
            Assert.AreEqual(0, state.FindPerson("1").LooksAtPersons.Count, "Started 50 ms ago: not yet.");
            CollectionAssert.AreEqual(new[] { "2" }, At(1.35).FindPerson("1").LooksAtPersons);
            Assert.IsFalse(At(1.35).Pairs.Any(pair => pair.MutualGaze), "One look only.");
            Assert.IsTrue(At(1.85).FindPair("1", "2").MutualGaze, "Both are looking at each other.");
            Assert.AreEqual(0, At(2.05).FindPerson("3").LooksAtPersons.Count + At(2.15).FindPerson("3").LooksAtPersons.Count, "A glance of a tenth of a second is never shown.");
            Assert.IsFalse(At(3.15).FindPair("1", "2").MutualGaze, "The first look has ended.");
            CollectionAssert.AreEqual(new[] { "1" }, At(3.15).FindPerson("2").LooksAtPersons);

            // The formation, from its start to its end.
            Assert.IsNull(At(1.15).FindPair("1", "2"), "Nothing in common yet.");
            Assert.AreEqual(FormationTypes.FaceToFace, At(1.25).FindPair("2", "1").Formation);
            Assert.AreEqual(FormationTypes.FaceToFace, At(3.95).FindPair("1", "2").Formation);
            Assert.AreEqual(string.Empty, At(4.05).FindPair("1", "2").Formation);

            // The same object looked at by both.
            CollectionAssert.AreEqual(new[] { "Cube" }, At(3.95).FindPair("1", "2").SharedObjects);
            CollectionAssert.AreEqual(new[] { "Cube" }, At(3.95).FindPerson("1").LooksAtObjects);
            Assert.AreEqual(0, At(3.75).FindPair("1", "2").SharedObjects.Count, "The second look has not lasted 200 ms yet.");
            Assert.IsNull(At(4.65).FindPair("1", "2"), "No formation, no look: the pair has nothing in common.");

            // The object in hand, from the grab to the placing.
            Assert.AreEqual(0, At(1.95).FindPerson("2").Holds.Count);
            CollectionAssert.AreEqual(new[] { "Piece_3" }, At(2.05).FindPerson("2").Holds);
            Assert.AreEqual(0, At(3.05).FindPerson("2").Holds.Count);

            // The detections, listed for four seconds after they were made.
            Assert.AreEqual(0, At(1.95).Events.Count);
            InstantEvent grab = At(2.05).Events.Single();
            Assert.AreEqual($"{IndexCategories.Grab} 2  Piece_3@Table", $"{grab.Category} {grab.Person} {grab.Other} {grab.Label}");
            CollectionAssert.AreEqual(new[] { IndexCategories.Grab, IndexCategories.Place }, At(3.05).Events.Select(detection => detection.Category).ToList());
            InstantEvent joint = At(4.55).Events.Last();
            Assert.AreEqual($"{IndexCategories.JointVisualAttention} 1 2 Cube", $"{joint.Category} {joint.Person} {joint.Other} {joint.Label}");
            Assert.IsFalse(At(6.15).Events.Any(detection => detection.Category == IndexCategories.Grab), "The grab was four seconds ago.");
            Assert.IsTrue(At(7.95).Events.Any(detection => detection.Category == IndexCategories.JointVisualAttention));

            CollectionAssert.AreEqual(new[] { GroupInstant.Formations, GroupInstant.Gaze, GroupInstant.Places, GroupInstant.Task, GroupInstant.Voice }, At(7.95).Signals, "What the state was built from.");

            // Before any formation, look or action: the formations and the task are known to be
            // watched (nothing happened yet), the looks are not known before the first one.
            CollectionAssert.AreEqual(new[] { GroupInstant.Formations, GroupInstant.Places, GroupInstant.Task, GroupInstant.Voice }, At(0.55).Signals);
            CollectionAssert.Contains(At(1.05).Signals, GroupInstant.Gaze);
        }

        [TestMethod]
        public void State_OnlyHoldsWhatItsSignalsGive_ForAnyNumberOfParticipants()
        {
            foreach (int count in new[] { 1, 2, 4, 7 })
            {
                foreach (bool withVoices in new[] { false, true })
                {
                    var states = new List<GroupInstant>();
                    List<uint> participants = Enumerable.Range(0, count).Select(i => (uint)i).ToList();

                    IndicesHarness.Run(1, pipeline =>
                    {
                        var timeline = new Timeline().Ticks("Clock", 0.05, 2, 0.1);
                        for (int i = 0; i <= 20; i++)
                        {
                            foreach (uint id in participants)
                            {
                                // In the standardised frame: Z is up.
                                timeline.Position($"Head{id}", (i * 0.1) + (0.001 * id), new Vector3(id, 0.5f * id, 1.7f));
                                timeline.Number($"Voice{id}", (i * 0.1) + 0.02 + (0.001 * id), id % 2);
                            }
                        }

                        timeline.Play(pipeline);

                        var instant = new InstantStateComponent(pipeline, new InstantStateConfiguration { ParticipantIds = participants, UpAxis = Vector3.UnitZ });
                        foreach (uint id in participants)
                        {
                            timeline.PositionsOf($"Head{id}").Select(position => Tuple.Create(position, new Vector3(0, 0, 1))).PipeTo(instant.GetHeadPoseInput(id));
                            if (withVoices)
                            {
                                timeline.NumbersOf($"Voice{id}").Select(value => value > 0.5).PipeTo(instant.GetVoiceInput(id));
                            }
                        }

                        timeline.TicksOf("Clock").PipeTo(instant.TickIn);
                        instant.Out.Do(state => states.Add(state.DeepClone()));
                    });

                    string context = $"{count} participants, {(withVoices ? "places and voices" : "places only")}";
                    GroupInstant last = states.Last();
                    Assert.AreEqual(count, last.Persons.Count, context);
                    CollectionAssert.AreEqual(withVoices ? new[] { GroupInstant.Places, GroupInstant.Voice } : new[] { GroupInstant.Places }, last.Signals, context);
                    Assert.AreEqual(0, last.Pairs.Count, $"{context}: no pair has anything in common.");
                    Assert.AreEqual(0, last.Events.Count, context);
                    foreach (uint id in participants)
                    {
                        PersonInstant person = last.FindPerson(SocialGraph.NodeId(id));
                        Assert.IsTrue(person.HasPlace && person.X == id && Math.Abs(person.Y - (0.5 * id)) < 1e-6, $"{context}: the floor is (X, Y) when Z is up.");
                        Assert.IsTrue(person.HeadingX == 0 && person.HeadingY == 0, $"{context}: a head that looks straight up faces nowhere on the floor.");
                        Assert.AreEqual(withVoices && id % 2 == 1, person.Speaking, context);
                        Assert.AreEqual(0, person.LooksAtPersons.Count + person.LooksAtObjects.Count + person.Holds.Count, context);
                    }
                }
            }
        }

        [TestMethod]
        public void State_KeepsAParticipantWhoseStreamsAreALittleAheadOfTheClock()
        {
            // The clock follows the head of one participant, and the streams are not read in
            // step: the last pose and voice sample of another one may be dated after the tick.
            var states = new List<GroupInstant>();
            IndicesHarness.Run(1, pipeline =>
            {
                var timeline = new Timeline().Ticks("Clock", 0.05, 3, 0.1);
                timeline.Play(pipeline);
                var instant = new InstantStateComponent(pipeline, new InstantStateConfiguration { ParticipantIds = new List<uint> { 0 } });
                Tuple<Vector3, Vector3> pose = Tuple.Create(new Vector3(1, 1.7f, 2), new Vector3(0, 0, 1));
                IProducer<bool> clock = timeline.TicksOf("Clock");
                clock.Process<bool, Tuple<Vector3, Vector3>>((_, envelope, emitter) => emitter.Post(pose, envelope.OriginatingTime.AddMilliseconds(150)))
                    .PipeTo(instant.GetHeadPoseInput(0));
                clock.Process<bool, bool>((_, envelope, emitter) => emitter.Post(true, envelope.OriginatingTime.AddMilliseconds(150)))
                    .PipeTo(instant.GetVoiceInput(0));
                clock.PipeTo(instant.TickIn);
                instant.Out.Do(state => states.Add(state.DeepClone()));
            });

            Assert.IsTrue(states.Count >= 25, $"A state per tick ({states.Count}).");
            List<PersonInstant> later = states.Skip(5).Select(state => state.Persons[0]).ToList();
            Assert.AreEqual(later.Count, later.Count(person => person.HasPlace && person.X == 1 && person.Y == 2), "The place, in every state once the first pose has come.");
            Assert.AreEqual(later.Count, later.Count(person => person.Speaking), "The voice as well.");
        }

        [TestMethod]
        public void State_ListsADetectionFromTheInstantItArrives_EvenWhenItIsDatedBefore()
        {
            // A turn taking decided from a transcription that came late is published with the
            // date of that transcription, seconds before the instant it reaches the state.
            var states = new List<(double Seconds, GroupInstant State)>();
            IndicesHarness.Run(1, pipeline =>
            {
                var timeline = new Timeline().Ticks("Clock", 0.05, 13, 0.1);
                timeline.Play(pipeline);
                var instant = new InstantStateComponent(pipeline, new InstantStateConfiguration { ParticipantIds = new List<uint> { 0, 1 } });
                IProducer<bool> clock = timeline.TicksOf("Clock");
                bool sent = false;
                clock.Process<bool, InteractionEvent>((_, envelope, emitter) =>
                {
                    // At 7 s, a detection of what happened at 1 s, dated 1.5 s.
                    if (!sent && envelope.OriginatingTime >= SyntheticData.Start.AddSeconds(7))
                    {
                        sent = true;
                        emitter.Post(new InteractionEvent(SyntheticData.Start.AddSeconds(1), IndexCategories.JointVisualAttention, 0, 1), SyntheticData.Start.AddSeconds(1.5));
                    }
                }).PipeTo(instant.DetectionIn);
                clock.PipeTo(instant.TickIn);
                instant.Out.Do((state, envelope) => states.Add(((envelope.OriginatingTime - SyntheticData.Start).TotalSeconds, state.DeepClone())));
            });

            List<(double Seconds, GroupInstant State)> listed = states.Where(state => state.State.Events.Count > 0).ToList();
            Assert.IsTrue(listed.Count >= 35, $"Listed for four seconds ({listed.Count} states).");
            Assert.IsTrue(listed.First().Seconds >= 7.0 && listed.First().Seconds < 7.4 && listed.Last().Seconds > 10.8 && listed.Last().Seconds < 11.5, $"From the instant it arrived: {listed.First().Seconds:0.00} s to {listed.Last().Seconds:0.00} s.");
            InstantEvent detection = listed.First().State.Events.Single();
            Assert.AreEqual(1.0, (detection.Time - SyntheticData.Start).TotalSeconds, 1e-6, "When it happened is kept.");
            Assert.IsTrue((detection.DetectedAt - SyntheticData.Start).TotalSeconds >= 6.9, "When it was detected is the instant it reached the state.");
        }

        [TestMethod]
        public void State_ForgetsAParticipantThatIsNoLongerSeenOrHeard()
        {
            var states = new List<(double Seconds, GroupInstant State)>();
            IndicesHarness.Run(1, pipeline =>
            {
                // The head and the voice of participant 0 stop at 1 s; the clock goes on.
                var timeline = new Timeline().Ticks("Clock", 0.05, 5, 0.1);
                for (int i = 0; i <= 10; i++)
                {
                    timeline.Position("Head0", i * 0.1, new Vector3(1, 1.7f, 1));
                    timeline.Number("Voice0", (i * 0.1) + 0.01, 1);
                }

                timeline.Play(pipeline);
                var instant = new InstantStateComponent(pipeline, new InstantStateConfiguration { ParticipantIds = new List<uint> { 0 } });
                timeline.PositionsOf("Head0").Select(position => Tuple.Create(position, new Vector3(0, 0, 1))).PipeTo(instant.GetHeadPoseInput(0));
                timeline.NumbersOf("Voice0").Select(value => value > 0.5).PipeTo(instant.GetVoiceInput(0));
                timeline.TicksOf("Clock").PipeTo(instant.TickIn);
                instant.Out.Do((state, envelope) => states.Add(((envelope.OriginatingTime - SyntheticData.Start).TotalSeconds, state.DeepClone())));
            });

            PersonInstant At(double seconds) => states.OrderBy(state => Math.Abs(state.Seconds - seconds)).First().State.Persons[0];
            Assert.IsTrue(At(0.95).HasPlace && At(0.95).Speaking && At(0.95).HeadingY == 1);
            Assert.IsTrue(At(1.85).Speaking, "Heard less than a second ago.");
            Assert.IsTrue(At(2.15).Speaking == false && At(2.95).HasPlace, "The voice stands for one second, the place for two.");
            Assert.IsFalse(At(3.15).HasPlace, "Not seen for more than two seconds.");
        }
    }
}
