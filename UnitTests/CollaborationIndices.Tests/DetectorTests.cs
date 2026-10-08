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
using SAAC.PsiFormats;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// The detectors that turn the raw streams of a session into the events and intervals the
    /// indicators count. Each scenario is a handful of timestamped messages written by hand.
    /// </summary>
    [TestClass]
    public class DetectorTests
    {
        private static readonly List<uint> Trio = new List<uint> { 0, 1, 2 };

        [TestMethod]
        public void TaskPhases_FollowTheTaskStartAndThePuzzleStatuses()
        {
            var starts = new List<double>();
            var ends = new List<double>();
            var puzzleIds = new List<int>();

            IndicesHarness.Run(1, pipeline =>
            {
                var script = new Script()
                    .At(10, "task", new Entry { Text = "start;True;17000;0;0" })
                    .At(10.1, "task", new Entry { Text = "start;True;16960;8000;13568" })
                    .At(50, "puzzle", new Entry { Puzzle = new PuzzleStatus { puzzleID = 0, optimalPiecesNumber = 8, currentPiecesNumber = 10 } })
                    .At(50, "task", new Entry { Text = "start;True;15000;7201;9440" })
                    .At(55, "puzzle", new Entry { Puzzle = new PuzzleStatus { puzzleID = 1, optimalPiecesNumber = 12, currentPiecesNumber = 0 } })
                    .At(90, "puzzle", new Entry { Puzzle = new PuzzleStatus { puzzleID = 1, optimalPiecesNumber = 12, currentPiecesNumber = 14 } })
                    .At(95, "puzzle", new Entry { Puzzle = new PuzzleStatus { puzzleID = 2, optimalPiecesNumber = 0, currentPiecesNumber = 0 } })
                    .Play(pipeline);

                var detector = new TaskPhaseDetector(pipeline);
                script.Of("task", e => e.Text).PipeTo(detector.TaskEventIn);
                script.Of("puzzle", e => e.Puzzle).PipeTo(detector.PuzzleStatusIn);
                detector.PhaseStartOut.Do((_, e) => starts.Add(Seconds(e.OriginatingTime)));
                detector.PhaseEndOut.Do((_, e) => ends.Add(Seconds(e.OriginatingTime)));
                detector.PuzzleIdOut.Do(id => puzzleIds.Add(id));
            });

            CollectionAssert.AreEqual(new[] { 10.0, 55.0 }, starts, "A puzzle starts with the task, then with each status of a puzzle not started yet.");
            CollectionAssert.AreEqual(new[] { 50.0, 90.0 }, ends, "A puzzle ends with the status that carries its pieces.");
            CollectionAssert.AreEqual(new[] { 0, 1 }, puzzleIds);
        }

        [TestMethod]
        public void AreaPresence_OpensAnIntervalOnEnterAndClosesItOnLeave()
        {
            var intervals = new List<InteractionInterval>();
            var current = new List<string>();

            IndicesHarness.Run(1, pipeline =>
            {
                var script = new Script()
                    .At(1, "area", new Entry { Text = "User1;Out;Out;Generator2Area" })
                    .At(2, "area", new Entry { Text = "User1;In;In;CentraleTableArea" })
                    .At(3, "area", new Entry { Text = "User1;In;In;IterationArea" })
                    .At(5, "area", new Entry { Text = "User1;Out;Out;IterationArea" })
                    .At(9, "area", new Entry { Text = "User1;Out;Out;CentraleTableArea" })
                    .Play(pipeline);

                var detector = new AreaPresenceDetector(pipeline, new AreaPresenceDetectorConfiguration { ParticipantIds = Trio });
                script.Of("area", e => e.Text).PipeTo(detector.GetAreaInput(0));
                detector.GetPresenceEmitter(0).Do(i => intervals.Add(i.Clone()));
                detector.GetCurrentAreaEmitter(0).Do(a => current.Add(a));
            });

            Assert.AreEqual(4, intervals.Count, "The leave without enter is ignored; each presence is published open, then closed.");
            Assert.IsTrue(intervals[0].IsOpen && intervals[0].Label == "CentraleTableZone" && intervals[0].Category == IndexCategories.InArea);
            InteractionInterval table = intervals.Last();
            Assert.AreEqual("CentraleTableZone", table.Label);
            Assert.AreEqual(2.0, Seconds(table.StartTime));
            Assert.AreEqual(9.0, Seconds(table.EndTime));
            CollectionAssert.AreEqual(
                new[] { "CentraleTableZone", "IterationTable", "CentraleTableZone", "Outside" },
                current,
                "Leaving a nested area gives back the area still occupied.");
        }

        [TestMethod]
        public void GazeEpisodes_KeepTheLooksOfAtLeast200MsAndFollowEachTarget()
        {
            var each = new List<InteractionInterval>();
            var oneAtATime = new List<InteractionInterval>();

            IndicesHarness.Run(1, pipeline =>
            {
                var script = new Script()
                    .At(1.00, "eye", Gaze("object", "A", true))
                    .At(1.10, "eye", Gaze("object", "A", false))     // 100 ms: dropped
                    .At(2.00, "eye", Gaze("object", "A", true))
                    .At(2.10, "eye", Gaze("object", "B", true))      // B starts while A is looked at
                    .At(2.50, "eye", Gaze("object", "A", false))     // A: 500 ms
                    .At(2.90, "eye", Gaze("object", "B", false))     // B: 800 ms
                    .At(3.00, "eye", Gaze("avatar", "1", true))      // not an object: ignored on this input
                    .At(4.00, "eye", Gaze("avatar", "1", false))
                    .Play(pipeline);

                var perTarget = new GazeEpisodeDetector(pipeline, new GazeEpisodeDetectorConfiguration { ParticipantIds = Trio }, "Each");
                var legacy = new GazeEpisodeDetector(pipeline, new GazeEpisodeDetectorConfiguration { ParticipantIds = Trio, TrackEachTarget = false }, "One");
                script.Of("eye", e => e.Gaze).PipeTo(perTarget.GetObjectGazeInput(0));
                script.Of("eye", e => e.Gaze).PipeTo(legacy.GetObjectGazeInput(0));
                perTarget.ObjectGazeOut.Do(i => each.Add(i.Clone()));
                legacy.ObjectGazeOut.Do(i => oneAtATime.Add(i.Clone()));
            });

            Assert.AreEqual(2, each.Count);
            Assert.AreEqual("A", each[0].Label);
            Assert.AreEqual(0.5, (each[0].EndTime - each[0].StartTime).TotalSeconds, 1e-6);
            Assert.AreEqual("B", each[1].Label);
            Assert.AreEqual(0.8, (each[1].EndTime - each[1].StartTime).TotalSeconds, 1e-6);

            Assert.AreEqual(1, oneAtATime.Count, "One gaze at a time: the start on B is ignored and the first end closes the gaze.");
            Assert.AreEqual(2.0, Seconds(oneAtATime[0].StartTime));
        }

        [TestMethod]
        public void PeerGazes_AreDirectedAndNotCountedTwiceWithin500Ms()
        {
            var looks = new List<InteractionEvent>();
            var gazing = new List<bool>();

            IndicesHarness.Run(1, pipeline =>
            {
                var script = new Script()
                    .At(1.0, "peer", Gaze("avatar", "2", true))
                    .At(1.5, "peer", Gaze("avatar", "2", false))     // 500 ms on participant 2
                    .At(1.6, "peer", Gaze("avatar", "2", true))
                    .At(1.9, "peer", Gaze("avatar", "2", false))     // ends 400 ms after the previous one: same look
                    .At(5.0, "peer", Gaze("avatar", "1", true))
                    .At(5.3, "peer", Gaze("avatar", "1", false))     // 300 ms on participant 1
                    .Play(pipeline);

                var detector = new GazeEpisodeDetector(pipeline, new GazeEpisodeDetectorConfiguration { ParticipantIds = Trio });
                script.Of("peer", e => e.Gaze).PipeTo(detector.GetPeerGazeInput(0));
                detector.PeerGazeOut.Do(e => looks.Add(e.Clone()));
                detector.GetGazingAtPeerEmitter(0).Do(g => gazing.Add(g));
            });

            Assert.AreEqual(2, looks.Count);
            Assert.AreEqual(IndexCategories.GazeOnPeer, looks[0].Category);
            Assert.AreEqual(0u, looks[0].ParticipantId);
            Assert.AreEqual(2u, looks[0].TargetId);
            Assert.AreEqual(1.5, Seconds(looks[0].OriginatingTime));
            Assert.AreEqual(1u, looks[1].TargetId);
            CollectionAssert.AreEqual(new[] { true, false, true, false, true, false }, gazing);
        }

        [TestMethod]
        public void JointAttention_NamesTheFirstToLookAsInitiator()
        {
            var episodes = new List<InteractionEvent>();

            IndicesHarness.Run(1, pipeline =>
            {
                var script = new Script()
                    .At(11.0, "gaze", Look(1, "piece", 10.0, 11.0))     // participant 1 looks first
                    .At(12.5, "gaze", Look(0, "piece", 12.0, 12.5))     // participant 0 follows 1 s after the end
                    .At(13.0, "gaze", Look(2, "other", 12.0, 13.0))     // another object
                    .At(20.0, "gaze", Look(2, "piece", 19.0, 20.0))     // too late: 6.5 s after the last look
                    .Play(pipeline);

                var detector = new JointVisualAttentionDetector(pipeline, new JointVisualAttentionDetectorConfiguration { ParticipantIds = Trio });
                script.Of("gaze", e => e.Interval).PipeTo(detector.ObjectGazeIn);
                detector.Out.Do(e => episodes.Add(e.Clone()));
            });

            Assert.AreEqual(1, episodes.Count);
            Assert.AreEqual(IndexCategories.JointVisualAttention, episodes[0].Category);
            Assert.AreEqual(1u, episodes[0].ParticipantId, "The initiator is the participant who looked first.");
            Assert.AreEqual(0u, episodes[0].TargetId);
            Assert.AreEqual(12.0, Seconds(episodes[0].OriginatingTime), "The episode is stamped with the start of the second look.");
            Assert.AreEqual("piece", episodes[0].Label);
        }

        [TestMethod]
        public void JointAttention_CanRequireTheHeadsToConverge()
        {
            var accepted = new List<InteractionEvent>();
            var rejected = new List<InteractionEvent>();

            IndicesHarness.Run(1, pipeline =>
            {
                // Participants 0 and 1 both look at the point (0, 0, 1); participant 2 looks away from it.
                var script = new Script()
                    .At(9.0, "pose0", new Entry { Pose = Tuple.Create(new Vector3(-1, 0, 0), new Vector3(1, 0, 1)) })
                    .At(9.0, "pose1", new Entry { Pose = Tuple.Create(new Vector3(1, 0, 0), new Vector3(-1, 0, 1)) })
                    .At(9.0, "pose2", new Entry { Pose = Tuple.Create(new Vector3(0, 0, 3), new Vector3(0, 0, 1)) })
                    .At(9.5, "gaze", Look(0, "piece", 9.1, 9.5))
                    .At(9.6, "gaze", Look(1, "piece", 9.2, 9.6))
                    .At(9.7, "gaze", Look(2, "piece", 9.3, 9.7))
                    .Play(pipeline);

                var detector = new JointVisualAttentionDetector(
                    pipeline,
                    new JointVisualAttentionDetectorConfiguration { ParticipantIds = Trio, RequireConvergingHeads = true });
                script.Of("gaze", e => e.Interval).PipeTo(detector.ObjectGazeIn);
                foreach (uint id in Trio)
                {
                    script.Of($"pose{id}", e => e.Pose).PipeTo(detector.GetHeadPoseInput(id));
                }

                detector.Out.Do(e => accepted.Add(e.Clone()));
                detector.RejectedOut.Do(e => rejected.Add(e.Clone()));
            });

            Assert.AreEqual(1, accepted.Count, "Only the pair whose heads point to the same place.");
            Assert.AreEqual(new ParticipantPair(0, 1), new ParticipantPair(accepted[0].ParticipantId, accepted[0].TargetId.Value));
            Assert.AreEqual(2, rejected.Count);

            Assert.IsTrue(JointVisualAttentionDetector.Converge(new Vector3(-1, 0, 0), new Vector3(1, 0, 1), new Vector3(1, 0, 0), new Vector3(-1, 0, 1), 1f));
            Assert.IsFalse(JointVisualAttentionDetector.Converge(new Vector3(-1, 0, 0), new Vector3(-1, 0, -1), new Vector3(1, 0, 0), new Vector3(-1, 0, 1), 1f), "Behind the first participant.");
            Assert.IsFalse(JointVisualAttentionDetector.Converge(Vector3.Zero, Vector3.UnitX, Vector3.UnitZ, Vector3.UnitX, 1f), "Parallel directions do not converge.");
        }

        [TestMethod]
        public void TurnTakings_AreClassifiedFromTheSpeechIntervals()
        {
            var events = new List<InteractionEvent>();

            IndicesHarness.Run(1, pipeline =>
            {
                // Each interval is published when it ends.
                var script = new Script()
                    .At(3.0, "speech", Speech(0, 1.0, 3.0))       // first to speak: nothing
                    .At(6.0, "speech", Speech(1, 4.0, 6.0))       // after a silence: 1 takes the turn from 0
                    .At(6.5, "speech", Speech(1, 6.2, 6.5))       // same speaker again: nothing
                    .At(9.0, "speech", Speech(0, 6.4, 9.0))       // starts while 1 speaks and outlasts it: with overlap
                    .At(12.0, "speech", Speech(2, 10.5, 11.0))    // after a silence: 2 takes the turn from 0
                    .At(13.0, "speech", Speech(0, 10.0, 13.0))    // contains the interval of 2: overlap of 2 over 0
                    .At(30.0, "tick", new Entry())                // lets the turn takings held back be published
                    .Play(pipeline);

                var detector = new TurnTakingDetector(pipeline, new TurnTakingDetectorConfiguration { ParticipantIds = Trio });
                script.Of("speech", e => e.Interval).PipeTo(detector.SpeechIn);
                script.Of("tick", e => true).PipeTo(detector.TickIn);
                detector.Out.Do(e => events.Add(e.Clone()));
            });

            string Describe(InteractionEvent e) => $"{e.Category} {e.ParticipantId}<{e.TargetId} @{Seconds(e.OriginatingTime):0.0}";
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "TurnTakingWithoutOverlap 1<0 @4.0",
                    "TurnTakingWithOverlap 0<1 @6.4",
                    "Overlap 2<0 @10.5",
                },
                events.Select(Describe).ToList(),
                "The short interval of 2 is said over 0: an overlap, not a turn taking. More scenarios in TurnTakingScenarioTests.");
        }

        [TestMethod]
        public void Formations_AreClassifiedFromDistanceAndHeadings()
        {
            // The classification of one instant needs no stream: the pipeline is not run.
            using (Pipeline pipeline = Pipeline.Create("Classification"))
            {
                var detector = new FFormationDetector(pipeline, new FFormationDetectorConfiguration { ParticipantIds = Trio });
                Vector3 origin = Vector3.Zero;

                Assert.AreEqual(FormationTypes.FaceToFace, detector.Classify(origin, Vector3.UnitZ, new Vector3(0, 0, 1.2f), -Vector3.UnitZ));
                Assert.AreEqual(FormationTypes.SideBySide, detector.Classify(origin, Vector3.UnitZ, new Vector3(1.0f, 0, 0), Vector3.UnitZ));
                Assert.AreEqual(
                    FormationTypes.LShape,
                    detector.Classify(origin, Vector3.Normalize(new Vector3(1, 0, 1)), new Vector3(1.0f, 0, 0), Vector3.Normalize(new Vector3(-1, 0, 1))));
                Assert.IsNull(detector.Classify(origin, Vector3.UnitZ, new Vector3(0, 0, 3f), -Vector3.UnitZ), "Too far.");
                Assert.IsNull(detector.Classify(origin, -Vector3.UnitZ, new Vector3(0, 0, 1.2f), Vector3.UnitZ), "Back to back.");

                // Looking down at the table between them: still face to face on the horizontal plane.
                Vector3 down = Vector3.Normalize(new Vector3(0, -1, 1));
                Assert.AreEqual(FormationTypes.FaceToFace, detector.Classify(origin, down, new Vector3(0, 0, 1.2f), Vector3.Normalize(new Vector3(0, -1, -1))));
            }
        }

        [TestMethod]
        public void Formations_StartAndEndAfterOneSecond()
        {
            var ended = new List<InteractionEvent>();
            var formations = new List<InteractionInterval>();
            var distances = new List<double>();

            IndicesHarness.Run(1, pipeline =>
            {
                // Face to face from 0 s to 4 s, then participant 1 turns its back until 8 s.
                var script = new Script();
                for (int i = 0; i <= 80; i++)
                {
                    double t = i * 0.1;
                    script.At(t, "pose0", new Entry { Pose = Tuple.Create(Vector3.Zero, Vector3.UnitZ) });
                    script.At(t, "pose1", new Entry { Pose = Tuple.Create(new Vector3(0, 0, 1.2f), t <= 4.0 ? -Vector3.UnitZ : Vector3.UnitZ) });
                }

                script.Play(pipeline);
                var detector = new FFormationDetector(pipeline, new FFormationDetectorConfiguration { ParticipantIds = new List<uint> { 0, 1 } });
                script.Of("pose0", e => e.Pose).PipeTo(detector.GetHeadPoseInput(0));
                script.Of("pose1", e => e.Pose).PipeTo(detector.GetHeadPoseInput(1));
                detector.FormationEndOut.Do(e => ended.Add(e.Clone()));
                detector.FormationOut.Do(i => formations.Add(i.Clone()));
                detector.GetDistanceEmitter(0, 1).Do(d => distances.Add(d));
            });

            Assert.AreEqual(1, ended.Count);
            Assert.AreEqual(IndexCategories.FormationEnd, ended[0].Category);
            Assert.AreEqual(FormationTypes.FaceToFace, ended[0].Label);
            Assert.AreEqual(0u, ended[0].ParticipantId);
            Assert.AreEqual(1u, ended[0].TargetId);
            Assert.AreEqual(4.0, Seconds(ended[0].OriginatingTime), 0.25, "Stamped with the last time the formation was observed.");
            Assert.AreEqual(0.0, Seconds(formations[0].StartTime), 0.25);
            Assert.IsTrue(distances.Count > 30 && distances.All(d => Math.Abs(d - 1.2) < 1e-5));
        }

        [TestMethod]
        public void TaskActions_AreLocatedAndHandoversDetected()
        {
            var actions = new List<InteractionEvent>();
            var handovers = new List<InteractionEvent>();

            IndicesHarness.Run(1, pipeline =>
            {
                var script = new Script()
                    .At(1.0, "area0", new Entry { Text = "CentraleTableZone" })
                    .At(1.0, "area1", new Entry { Text = "CentraleTableZone" })
                    .At(2.0, "piece0", Piece(State.Grab, "angle", Location.Hand))
                    .At(3.0, "piece0", Piece(State.Colored, "cube", Location.IterationTable))     // not in the colour area: dropped
                    .At(4.0, "piece0", Piece(State.Ungrab, "angle", Location.Hand))
                    .At(5.0, "piece1", Piece(State.Grab, "angle", Location.Hand))                 // 1 s after the release by 0: handover
                    .At(6.0, "generator0", new Entry { Generator = new GeneratorInteraction { generatorID = 2, interactionType = GenInteraction.Spawnrequest, objectID = "angle" } })
                    .At(7.0, "area0", new Entry { Text = "IterationTable" })
                    .At(8.00, "piece0", Piece(State.Colored, "cube", Location.IterationTable))
                    .At(8.05, "piece0", Piece(State.Colored, "cube", Location.IterationTable))    // burst: dropped
                    .At(20.0, "piece0", Piece(State.Ungrab, "angle", Location.Hand))
                    .At(25.0, "piece1", Piece(State.Grab, "angle", Location.Hand))                // 5 s later: no handover
                    .Play(pipeline);

                var detector = new TaskActionDetector(pipeline, new TaskActionDetectorConfiguration { ParticipantIds = Trio });
                var handoverDetector = new ObjectHandoverDetector(pipeline);
                for (uint id = 0; id < 2; id++)
                {
                    script.Of($"area{id}", e => e.Text).PipeTo(detector.GetCurrentAreaInput(id));
                    script.Of($"piece{id}", e => e.Piece).PipeTo(detector.GetPieceInput(id));
                }

                script.Of("generator0", e => e.Generator).PipeTo(detector.GetGeneratorInput(0, 2));
                detector.Out.PipeTo(handoverDetector.ActionsIn);
                detector.Out.Do(e => actions.Add(e.Clone()));
                handoverDetector.Out.Do(e => handovers.Add(e.Clone()));
            });

            CollectionAssert.AreEqual(
                new[] { "Grab", "Ungrab", "Grab", "GeneratorInteraction", "Color", "Ungrab", "Grab" },
                actions.Select(a => a.Category).ToList());
            Assert.AreEqual("angle@CentraleTableZone", actions[0].Label, "A grab is located by the area of the participant.");
            Assert.AreEqual(0u, actions[0].ParticipantId, "The participant is the one of the input, not the identifier inside the status.");
            Assert.AreEqual(1u, actions[2].ParticipantId);

            Assert.AreEqual(1, handovers.Count);
            Assert.AreEqual(DetectionCategories.ObjectHandover, handovers[0].Category);
            Assert.AreEqual(0u, handovers[0].ParticipantId, "The giver.");
            Assert.AreEqual(1u, handovers[0].TargetId, "The receiver.");
            Assert.AreEqual(5.0, Seconds(handovers[0].OriginatingTime));
        }

        private static double Seconds(DateTime time) => Math.Round((time - SyntheticData.Start).TotalSeconds, 3);

        private static Entry Gaze(string type, string target, bool status)
            => new Entry { Gaze = new ObjectGazeEvent(type, 1, target, status) };

        private static Entry Look(uint participant, string target, double start, double end)
            => new Entry { Interval = new InteractionInterval(SyntheticData.At(start), SyntheticData.At(end), DetectionCategories.GazeOnObject, participant, null, target) };

        private static Entry Speech(uint participant, double start, double end)
            => new Entry { Interval = new InteractionInterval(SyntheticData.At(start), SyntheticData.At(end), IndexCategories.Speaking, participant) };

        private static Entry Piece(State state, string objectId, Location location)
            => new Entry { Piece = new PieceStatus(9, objectId, state, true, string.Empty, location) };

        /// <summary>One message of a scenario; only the field of its stream is set.</summary>
        internal sealed class Entry
        {
            public string Stream { get; set; }

            public string Text { get; set; }

            public ObjectGazeEvent Gaze { get; set; }

            public PieceStatus Piece { get; set; }

            public GeneratorInteraction Generator { get; set; }

            public PuzzleStatus Puzzle { get; set; }

            public InteractionInterval Interval { get; set; }

            public Tuple<Vector3, Vector3> Pose { get; set; }
        }

        /// <summary>
        /// A scenario played from a single source, so that the messages of its streams reach
        /// the detectors in the order of their timestamps.
        /// </summary>
        private sealed class Script
        {
            private readonly List<(Entry, DateTime)> entries = new List<(Entry, DateTime)>();
            private IProducer<Entry> source;

            public Script At(double seconds, string stream, Entry entry)
            {
                entry.Stream = stream;
                this.entries.Add((entry, SyntheticData.At(seconds)));
                return this;
            }

            public Script Play(Pipeline pipeline)
            {
                // Two messages cannot share a timestamp on one stream: nudge the later ones by a tick each.
                var ordered = new List<(Entry, DateTime)>();
                DateTime last = DateTime.MinValue;
                foreach ((Entry entry, DateTime time) in this.entries.OrderBy(e => e.Item2))
                {
                    DateTime stamped = time > last ? time : last.AddTicks(1);
                    ordered.Add((entry, stamped));
                    last = stamped;
                }

                this.source = Generators.Sequence(pipeline, ordered);
                return this;
            }

            public IProducer<T> Of<T>(string stream, Func<Entry, T> select)
                => this.source.Where(entry => entry.Stream == stream).Select(select);
        }
    }
}
