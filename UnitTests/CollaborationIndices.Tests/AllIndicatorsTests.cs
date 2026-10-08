// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.Psi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.CollaborationIndices;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// The fifteen indicators of the catalogue computing together on a small scripted session,
    /// with the score, the graph and the export. It checks that every one of them publishes on
    /// every tick: an indicator that announced a value without posting it would hold back the
    /// snapshots, and the counts below would not match.
    /// </summary>
    [TestClass]
    public class AllIndicatorsTests
    {
        private static readonly uint[] Participants = { 0, 1, 2 };

        [TestMethod]
        public void EveryIndicator_PublishesOnEveryTick()
        {
            Run(1, out List<IndexSnapshot> snapshots, out List<InteractionGraph> graphs, out StringWriter csv, out int gateTicks);

            Assert.AreEqual(8, gateTicks, "Ticks from 4.02 s to 11.02 s, after 4 s of warm-up.");
            Assert.AreEqual(gateTicks, snapshots.Count, "One snapshot per tick: no indicator holds the others back.");
            Assert.AreEqual(gateTicks, graphs.Count, "One graph per tick.");
            Assert.AreEqual(gateTicks + 1, IndicesHarness.Rows(csv).Count, "One row per tick, plus the header.");

            IndexSnapshot last = snapshots[snapshots.Count - 1];

            foreach (string index in new[]
            {
                "Movement", "Synchrony", "VerbalParticipation", "SpeechEquality", "TalkingMost",
                "TurnTakingWithOverlap", "TurnTakingWithoutOverlap", "Overlap", "Silence",
                "JointVisualAttention", "GazeOnPeers", "TaskParticipation", "TaskEquality", "TaskingMost",
                "TimeInArea", "Formation", "CollaborationScore",
            })
            {
                Assert.IsTrue(last.Group.ContainsKey(index), $"Group value of {index}");
            }

            foreach (string index in new[] { "Movement", "VerbalParticipation", "TaskParticipation", "TimeInArea_Table", "TimeInArea_Board", "AttentionLevel" })
            {
                Assert.IsTrue(last.Individual.ContainsKey(index), $"Values per participant of {index}");
            }

            foreach (string index in new[] { "Synchrony", "SpeechEquality", "TaskEquality", "JointVisualAttention", "Formation", "Proximity", "TurnTakingWithoutOverlap" })
            {
                Assert.IsTrue(last.Pair.ContainsKey(index), $"Values per pair of {index}");
            }

            Assert.IsTrue(last.DirectedPair.ContainsKey("GazeOnPeers"));
        }

        [TestMethod]
        public void EveryIndicator_ComputesItsWindow()
        {
            Run(1, out List<IndexSnapshot> snapshots, out _, out _, out _);

            // First tick, 4.02 s: the window is [0.02, 4.02].
            IndexSnapshot first = snapshots[0];
            Assert.AreEqual(1.0, first.Group["JointVisualAttention"], "One episode, at 2.0 s.");
            Assert.AreEqual(1.0, first.Pair["JointVisualAttention"][new ParticipantPair(0, 1)]);
            Assert.AreEqual(1.0, first.Group["GazeOnPeers"], "One gaze, at 2.5 s.");
            Assert.AreEqual(1.0, first.DirectedPair["GazeOnPeers"][new DirectedParticipantPair(0, 1)]);
            Assert.AreEqual(0.0, first.DirectedPair["GazeOnPeers"][new DirectedParticipantPair(1, 0)], "A gaze has a direction.");
            Assert.AreEqual(1.0, first.Individual["TaskParticipation"][0], "One piece placed by participant 0, at 1.5 s.");
            Assert.AreEqual(3.0, first.Individual["TimeInArea_Table"][0], 1e-9, "Participant 0 stood at the table from 0.5 to 3.5 s.");
            Assert.AreEqual(0.0, first.Individual["TimeInArea_Board"][0], 1e-9);
            Assert.AreEqual(1.0, first.Group["TurnTakingWithoutOverlap"], "One turn taking, at 3.0 s.");
            Assert.AreEqual(1.0, first.Pair["TurnTakingWithoutOverlap"][new ParticipantPair(0, 1)]);
            Assert.AreEqual(0.6, first.Pair["Proximity"][new ParticipantPair(0, 1)], 1e-9, "1.2 m apart, on a scale where 3 m is zero.");
            Assert.AreEqual(1.5 / 4.0, first.Individual["VerbalParticipation"][0], 1e-9, "Participant 0 spoke 1.5 s of the 4 s window.");
            Assert.IsTrue(first.Individual["AttentionLevel"][0] > 0, "Participant 0 has been attentive since 1 s.");
            Assert.AreEqual(0.0, first.Individual["AttentionLevel"][1]);

            // Second tick, 5.02 s: the formation ended at 4.5 s has entered the window.
            Assert.AreEqual(0.0, first.Group["Formation"]);
            Assert.AreEqual(1.0, snapshots[1].Group["Formation"]);
            Assert.AreEqual(1.0, snapshots[1].Pair["Formation"][new ParticipantPair(0, 1)]);

            // Last tick, 11.02 s: the window is [7.02, 11.02], the early events have left it.
            IndexSnapshot last = snapshots[snapshots.Count - 1];
            Assert.AreEqual(0.0, last.Group["JointVisualAttention"]);
            Assert.AreEqual(1.0, last.Group["GazeOnPeers"], "The gaze of 7.5 s.");
            Assert.AreEqual(1.0, last.Group["TurnTakingWithoutOverlap"], "The turn taking of 8.0 s.");
            Assert.AreEqual(0.0, last.Individual["TimeInArea_Table"][0], 1e-9);
        }

        [TestMethod]
        public void Graph_HoldsTheMetricsOfItsTick()
        {
            Run(0, out List<IndexSnapshot> snapshots, out List<InteractionGraph> graphs, out _, out _);

            Assert.AreEqual(snapshots.Count, graphs.Count);
            for (int i = 0; i < graphs.Count; i++)
            {
                InteractionGraph graph = graphs[i];
                IndexSnapshot snapshot = snapshots[i];

                Assert.AreEqual(snapshot.OriginatingTime, graph.OriginatingTime);
                Assert.AreEqual(3, graph.Nodes.Count);
                Assert.AreEqual(3, graph.Edges.Count);

                foreach (IndexNode node in graph.Nodes)
                {
                    Assert.AreEqual(snapshot.Individual["Movement"][node.ParticipantId], node.Metrics["Movement"]);
                    Assert.AreEqual(snapshot.Individual["VerbalParticipation"][node.ParticipantId], node.Metrics["VerbalParticipation"]);
                    Assert.IsFalse(node.Metrics.ContainsKey("TimeInArea_Table"), "Declared for the export only.");
                }

                foreach (IndexEdge edge in graph.Edges)
                {
                    Assert.AreEqual(snapshot.Pair["Proximity"][edge.Pair], edge.Metrics["Proximity"]);
                    Assert.AreEqual(snapshot.Pair["JointVisualAttention"][edge.Pair], edge.Metrics["JointVisualAttention"]);
                    Assert.AreEqual(
                        snapshot.DirectedPair["GazeOnPeers"][new DirectedParticipantPair(edge.Pair.A, edge.Pair.B)],
                        edge.ForwardMetrics["GazeOnPeers"]);
                    Assert.AreEqual(
                        snapshot.DirectedPair["GazeOnPeers"][new DirectedParticipantPair(edge.Pair.B, edge.Pair.A)],
                        edge.BackwardMetrics["GazeOnPeers"]);
                }

                Assert.AreEqual(snapshot.Group["CollaborationScore"], graph.Group.Metrics["CollaborationScore"]);
            }
        }

        private static void Run(int threads, out List<IndexSnapshot> snapshots, out List<InteractionGraph> graphs, out StringWriter csv, out int gateTicks)
        {
            var snapshotList = new List<IndexSnapshot>();
            var graphList = new List<InteractionGraph>();
            var writer = new StringWriter();
            int ticks = 0;

            IndicesHarness.Run(threads, pipeline =>
            {
                Timeline timeline = Script().Play(pipeline);

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(Participants)
                    .WithWindow(TimeSpan.FromSeconds(4))
                    .WithCalibration(new IndexCalibration())
                    .WithClock(timeline.TicksOf("Clock"))
                    .AddIndicator(Indicators.Movement, options => options.BodyParts = new List<string> { BodyPartNames.Head })
                    .AddIndicator(Indicators.Synchrony)
                    .AddIndicator(Indicators.VerbalParticipation)
                    .AddIndicator(Indicators.SpeechEquality)
                    .AddIndicator(Indicators.TalkingMost)
                    .AddIndicator(Indicators.TurnTaking)
                    .AddIndicator(Indicators.JointVisualAttention)
                    .AddIndicator(Indicators.GazeOnPeers)
                    .AddIndicator(Indicators.AttentionLevel)
                    .AddIndicator(Indicators.TaskParticipation)
                    .AddIndicator(Indicators.TaskEquality)
                    .AddIndicator(Indicators.TaskingMost)
                    .AddIndicator(Indicators.TimeInArea, options =>
                    {
                        options.Areas = new List<string> { "Table", "Board" };
                        options.PlanningAreas = new List<string> { "Board" };
                    })
                    .AddIndicator(Indicators.Formation)
                    .AddIndicator(Indicators.Proximity)
                    .WithCollaborationScore()
                    .WithInteractionGraph()
                    .WithCsvExport(writer)
                    .Build();

                foreach (uint id in Participants)
                {
                    timeline.PositionsOf($"Head{id}").PipeTo(indices.Get(Indicators.Movement).Component.GetPositionInput(id, BodyPartNames.Head));
                    timeline.PositionsOf($"Head{id}").PipeTo(indices.Get(Indicators.Synchrony).Component.GetPositionInput(id, BodyPartNames.Head));
                    timeline.IntervalsOf($"Speech{id}").PipeTo(indices.Get(Indicators.VerbalParticipation).Component.GetIntervalInput(id));
                    timeline.IntervalsOf($"Area{id}").PipeTo(indices.Get(Indicators.TimeInArea).Component.GetIntervalInput(id));
                }

                timeline.EventsOf("Turns").PipeTo(indices.Get(Indicators.TurnTaking).Component.EventIn);
                timeline.EventsOf("Jva").PipeTo(indices.Get(Indicators.JointVisualAttention).Component.EventIn);
                timeline.EventsOf("Gaze").PipeTo(indices.Get(Indicators.GazeOnPeers).Component.EventIn);
                timeline.EventsOf("Task").PipeTo(indices.Get(Indicators.TaskParticipation).Component.EventIn);
                timeline.EventsOf("Formation").PipeTo(indices.Get(Indicators.Formation).Component.EventIn);
                timeline.NumbersOf("Distance01").PipeTo(indices.Get(Indicators.Proximity).Component.GetDistanceInput(0, 1));
                timeline.NumbersOf("Distance02").PipeTo(indices.Get(Indicators.Proximity).Component.GetDistanceInput(0, 2));
                timeline.NumbersOf("Distance12").PipeTo(indices.Get(Indicators.Proximity).Component.GetDistanceInput(1, 2));

                AttentionLevelIndicator attention = indices.Get(Indicators.AttentionLevel);
                timeline.TicksOf("Fast").PipeTo(attention.ClockIn);
                timeline.TicksOf("Attentive0").PipeTo(attention.Component.GetAttentiveInput(0));

                indices.Gate.Out.Do(_ => ticks++);
                indices.SnapshotOut.Do(snapshot => snapshotList.Add(snapshot.Clone()));
                indices.Out.Do(graph => graphList.Add(Copy(graph)));
            });

            snapshots = snapshotList;
            graphs = graphList;
            csv = writer;
            gateTicks = ticks;
        }

        private static Timeline Script()
        {
            Timeline timeline = new Timeline()
                .Ticks("Clock", 0.02, 12, 1.0)
                .Ticks("Fast", 0.005, 12, 0.1)
                .Ticks("Attentive0", 1.0, 1.05, 1.0)

                .Interval("Speech0", 1.0, 2.5, IndexCategories.Speaking, 0)
                .Interval("Speech1", 3.0, 4.5, IndexCategories.Speaking, 1)
                .Interval("Speech2", 6.0, 7.0, IndexCategories.Speaking, 2)
                .Interval("Speech0", 8.0, 9.5, IndexCategories.Speaking, 0)

                .Event("Turns", 3.0, IndexCategories.TurnTakingWithoutOverlap, 1, 0)
                .Event("Turns", 4.4, IndexCategories.Overlap, 1, 0)
                .Event("Turns", 6.0, IndexCategories.TurnTakingWithoutOverlap, 2, 1)
                .Event("Turns", 8.0, IndexCategories.TurnTakingWithoutOverlap, 0, 2)

                .Event("Jva", 2.0, IndexCategories.JointVisualAttention, 0, 1)
                .Event("Jva", 5.0, IndexCategories.JointVisualAttention, 2, 1)

                .Event("Gaze", 2.5, IndexCategories.GazeOnPeer, 0, 1)
                .Event("Gaze", 5.5, IndexCategories.GazeOnPeer, 1, 2)
                .Event("Gaze", 7.5, IndexCategories.GazeOnPeer, 2, 0)

                .Event("Task", 1.5, IndexCategories.Place, 0, null, "piece1")
                .Event("Task", 5.2, IndexCategories.Place, 1, null, "piece2")
                .Event("Task", 9.0, IndexCategories.Color, 2, null, "piece3")

                .Interval("Area0", 0.5, 3.5, IndexCategories.InArea, 0, "Table")
                .Interval("Area1", 2.0, 6.0, IndexCategories.InArea, 1, "Board")
                .Interval("Area2", 7.5, 9.0, IndexCategories.InArea, 2, "Table")

                .Event("Formation", 4.5, IndexCategories.FormationEnd, 0, 1);

            for (int i = 0; i < 12; i++)
            {
                timeline.Number("Distance01", i + 0.3, 1.2);
                timeline.Number("Distance02", i + 0.31, 2.4);
                timeline.Number("Distance12", i + 0.32, 4.5);
            }

            // Heads at 20 Hz, each participant on its own rhythm.
            for (int i = 0; i < 240; i++)
            {
                double t = i * 0.05;
                foreach (uint id in Participants)
                {
                    float x = (float)(id + (0.1 * Math.Sin((1.0 + (0.3 * id)) * t)) + (0.02 * t));
                    timeline.Position($"Head{id}", t + (0.001 * (id + 1)), new Vector3(x, 1.6f, 0));
                }
            }

            return timeline;
        }

        private static InteractionGraph Copy(InteractionGraph graph)
            => new InteractionGraph
            {
                OriginatingTime = graph.OriginatingTime,
                Nodes = graph.Nodes.Select(node => new IndexNode { ParticipantId = node.ParticipantId, Metrics = new Dictionary<string, double>(node.Metrics) }).ToList(),
                Edges = graph.Edges.Select(edge => new IndexEdge
                {
                    Pair = edge.Pair,
                    Metrics = new Dictionary<string, double>(edge.Metrics),
                    ForwardMetrics = new Dictionary<string, double>(edge.ForwardMetrics),
                    BackwardMetrics = new Dictionary<string, double>(edge.BackwardMetrics),
                }).ToList(),
                Group = new IndexGroup { Metrics = new Dictionary<string, double>(graph.Group.Metrics) },
            };
    }
}
