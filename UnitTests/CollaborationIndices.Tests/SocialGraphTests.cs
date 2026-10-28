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
    /// The graph of the sociogram (IndividualPairCharacteristics of the Groups component)
    /// built from the collaboration indices: for any number of participants, and with
    /// whatever indicators are declared, down to none.
    /// </summary>
    [TestClass]
    public class SocialGraphTests
    {
        [TestMethod]
        public void Graph_HasANodePerParticipantAndAnEdgePerPair_WhateverTheirNumber()
        {
            foreach (int count in new[] { 1, 2, 3, 4, 5, 6, 10, 12 })
            {
                List<uint> participants = Enumerable.Range(0, count).Select(i => (uint)i).ToList();
                IndividualPairCharacteristics graph = SocialGraph.From(FullSnapshot(participants), participants);

                CollectionAssert.AreEqual(
                    Enumerable.Range(1, count).Select(number => number.ToString()).ToList(),
                    graph.PersonNodes.Select(node => node.Id).ToList(),
                    $"{count} participants: a node per participant, numbered from 1.");
                Assert.AreEqual(count * (count - 1) / 2, graph.PersonEdges.Count, $"{count} participants: an edge per pair.");
                Assert.AreEqual(graph.PersonEdges.Count, graph.PersonEdges.Select(edge => GraphMetrics.PairKey(edge.First, edge.Second)).Distinct().Count(), "Each pair once.");

                foreach (uint id in participants)
                {
                    PersonNode node = graph.FindNode(SocialGraph.NodeId(id));
                    Assert.IsNotNull(node);
                    Assert.AreEqual(0.1 * (id + 1), node.SpeakingTime, 1e-12, "The speaking time of the node is the verbal participation of its participant.");
                    Assert.AreEqual(id + 0.5, node.MovementScore, 1e-12);
                    Assert.AreEqual(id + 0.25, node.TaskParticipation, 1e-12);
                }

                foreach (ParticipantPair pair in Combinatorics.Pairs(participants))
                {
                    // The edge is found in both orders, and its directions do not depend on the order asked.
                    PersonEdge edge = graph.FindEdge(SocialGraph.NodeId(pair.B), SocialGraph.NodeId(pair.A));
                    Assert.IsNotNull(edge, $"Edge of {pair}.");
                    Assert.AreEqual(SocialGraph.NodeId(pair.A), edge.First);
                    Assert.AreEqual(SocialGraph.NodeId(pair.B), edge.Second);
                    Assert.AreEqual((pair.A * 100) + pair.B, edge.Synchrony, 1e-12);
                    Assert.AreEqual((pair.A * 100) + pair.B + 0.5, edge.JVAEvent, 1e-12);
                    Assert.AreEqual((pair.A * 10) + pair.B, edge.GazeOnPeers12, 1e-12, $"Looks of {pair.A} at {pair.B}.");
                    Assert.AreEqual((pair.B * 10) + pair.A, edge.GazeOnPeers21, 1e-12, $"Looks of {pair.B} at {pair.A}.");
                }

                Assert.AreEqual(0.7, graph.PersonGroup.Collaboration_score, 1e-12);
                Assert.AreEqual(0.2, graph.PersonGroup.Dominance_score, 1e-12);
                Assert.AreEqual(1, graph.PersonGroup.TalkingMost, 1e-12, "TalkingMost names a participant by its number, which is the identifier of its node.");
            }
        }

        [TestMethod]
        public void Graph_OnlyHoldsTheIndicesThatAreComputed()
        {
            var participants = new List<uint> { 0, 1, 2, 3 };

            // Only the speech indicators: what the server computes when only those boxes are checked.
            var speechOnly = new IndexSnapshot();
            speechOnly.Individual[IndexNames.VerbalParticipation] = participants.ToDictionary(id => id, id => 0.2);
            speechOnly.Group[IndexNames.SpeechEquality] = 0.9;
            IndividualPairCharacteristics graph = SocialGraph.From(speechOnly, participants);

            Assert.AreEqual(4, graph.PersonNodes.Count);
            Assert.AreEqual(6, graph.PersonEdges.Count, "The edges exist without any index of the pairs.");
            Assert.IsTrue(graph.PersonNodes.All(node => node.Has(GraphMetricNames.VerbalParticipation)));
            Assert.IsTrue(graph.PersonNodes.All(node => !node.Has(GraphMetricNames.Movement) && !node.Has(GraphMetricNames.TaskParticipation)), "An index that is not computed is absent, not zero.");
            Assert.IsTrue(graph.PersonEdges.All(edge => edge.Metrics.Count == 0 && edge.ForwardMetrics.Count == 0 && edge.BackwardMetrics.Count == 0));
            Assert.IsTrue(graph.PersonEdges.All(edge => !edge.Has(GraphMetricNames.Distance)), "No distance without the proximity indicator.");
            Assert.AreEqual(0.0, graph.PersonEdges[0].Synchrony, "Read as before, an absent index gives 0.");
            Assert.IsTrue(graph.PersonGroup.Has(IndexNames.SpeechEquality));
            Assert.IsFalse(graph.PersonGroup.Has(GraphMetricNames.CollaborationScore), "No score when it is not computed.");
            Assert.IsFalse(graph.PersonGroup.Has(GraphMetricNames.TalkingMost));

            // An index computed for some participants only, as while a stream has not started.
            var partial = new IndexSnapshot();
            partial.Individual[IndexNames.Movement] = new Dictionary<uint, double> { { 1, 0.4 } };
            partial.Pair[IndexNames.Synchrony] = new Dictionary<ParticipantPair, double> { { new ParticipantPair(0, 3), 0.6 } };
            graph = SocialGraph.From(partial, participants);
            Assert.AreEqual("2", string.Join(",", graph.PersonNodes.Where(node => node.Has(GraphMetricNames.Movement)).Select(node => node.Id)));
            Assert.AreEqual("1|4", string.Join(",", graph.PersonEdges.Where(edge => edge.Has(GraphMetricNames.Synchrony)).Select(edge => GraphMetrics.PairKey(edge.First, edge.Second))));

            // No index at all, then no snapshot at all: the participants and their pairs alone.
            foreach (IndexSnapshot nothing in new[] { new IndexSnapshot(), null })
            {
                graph = SocialGraph.From(nothing, participants);
                Assert.AreEqual(4, graph.PersonNodes.Count);
                Assert.AreEqual(6, graph.PersonEdges.Count);
                Assert.IsTrue(graph.PersonNodes.All(node => node.Metrics.Count == 0));
                Assert.AreEqual(0, graph.PersonGroup.Metrics.Count);
            }

            // What is in the snapshot for somebody who is not a participant is left out.
            var foreign = new IndexSnapshot();
            foreign.Individual[IndexNames.Movement] = new Dictionary<uint, double> { { 7, 0.4 } };
            foreign.Pair[IndexNames.Synchrony] = new Dictionary<ParticipantPair, double> { { new ParticipantPair(0, 7), 0.6 } };
            foreign.DirectedPair[IndexNames.GazeOnPeers] = new Dictionary<DirectedParticipantPair, double> { { new DirectedParticipantPair(7, 0), 2 } };
            graph = SocialGraph.From(foreign, participants);
            Assert.IsTrue(graph.PersonNodes.All(node => node.Metrics.Count == 0) && graph.PersonEdges.All(edge => edge.Metrics.Count == 0 && edge.BackwardMetrics.Count == 0));
        }

        [TestMethod]
        public void Graph_NamesTheParticipantsByTheirNumber_WhateverTheirIdentifiers()
        {
            // Participants 4 and 9, given in any order: nodes "5" and "10", the lower number first.
            var snapshot = new IndexSnapshot();
            snapshot.DirectedPair[IndexNames.GazeOnPeers] = new Dictionary<DirectedParticipantPair, double>
            {
                { new DirectedParticipantPair(9, 4), 3 },
                { new DirectedParticipantPair(4, 9), 1 },
            };
            snapshot.Group[IndexNames.TalkingMost] = 10;   // identifier 9, plus one

            IndividualPairCharacteristics graph = SocialGraph.From(snapshot, new uint[] { 9, 4 });
            CollectionAssert.AreEqual(new[] { "5", "10" }, graph.PersonNodes.Select(node => node.Id).ToList());
            PersonEdge edge = graph.PersonEdges.Single();
            Assert.AreEqual("5", edge.First, "By number, 5 comes before 10.");
            Assert.AreEqual("10", edge.Second);
            Assert.AreEqual(1, edge.GazeOnPeers12, "Looks of the first at the second.");
            Assert.AreEqual(3, edge.GazeOnPeers21);
            Assert.IsNotNull(graph.FindNode(graph.PersonGroup.TalkingMost.ToString("0")), "The participant who talks most is a node of the graph.");
        }

        [TestMethod]
        public void Graph_PlacesItsNodesFromDistancesInMetres()
        {
            var participants = new List<uint> { 0, 1, 2 };
            var snapshot = new IndexSnapshot();
            snapshot.Pair[IndexNames.Proximity] = new Dictionary<ParticipantPair, double>
            {
                { new ParticipantPair(0, 1), 1.0 },    // together
                { new ParticipantPair(0, 2), 0.5 },    // half of the maximum distance
                { new ParticipantPair(1, 2), 0.0 },    // at the maximum distance, or further
            };

            // The proximity score of the indicator: 1 - d / 3 m by default.
            IndividualPairCharacteristics graph = SocialGraph.From(snapshot, participants);
            Assert.AreEqual(0.0, graph.FindEdge("1", "2").Proximity, 1e-12);
            Assert.AreEqual(1.5, graph.FindEdge("1", "3").Proximity, 1e-12);
            Assert.AreEqual(3.0, graph.FindEdge("2", "3").Proximity, 1e-12);
            Assert.AreEqual(0.5, graph.FindEdge("1", "3").Get(IndexNames.Proximity), 1e-12, "The index itself is kept under its name.");

            graph = SocialGraph.From(snapshot, participants, new SocialGraphOptions { MaximumDistance = 5 });
            Assert.AreEqual(2.5, graph.FindEdge("1", "3").Proximity, 1e-12);

            // The indicator set to publish the distance itself.
            graph = SocialGraph.From(snapshot, participants, new SocialGraphOptions { ProximityIsScore = false });
            Assert.AreEqual(0.5, graph.FindEdge("1", "3").Proximity, 1e-12);
        }

        [TestMethod]
        public void MeanDistance_IsTheMeanOfTheDistancesOfTheWindow()
        {
            var participants = new List<uint> { 0, 1, 2 };
            IndexSnapshot At(double seconds, double first, double? second)
            {
                var snapshot = new IndexSnapshot { OriginatingTime = SyntheticData.At(seconds) };
                snapshot.Pair[IndexNames.Proximity] = new Dictionary<ParticipantPair, double> { { new ParticipantPair(0, 1), first } };
                if (second.HasValue)
                {
                    snapshot.Pair[IndexNames.Proximity][new ParticipantPair(0, 2)] = second.Value;
                }

                return snapshot;
            }

            // A snapshot per second, a window of 5 s. The pair 1-2 is t metres apart at t seconds;
            // the pair 1-3 is only measured on even seconds, 10 m apart; the pair 2-3 never is.
            var assembler = new SocialGraphAssembler(participants, new SocialGraphOptions { Window = TimeSpan.FromSeconds(5), ProximityIsScore = false });
            for (int t = 1; t <= 12; t++)
            {
                IndividualPairCharacteristics graph = assembler.Next(At(t, t, t % 2 == 0 ? 10 : (double?)null));
                PersonEdge first = graph.FindEdge("1", "2");
                Assert.AreEqual(t, first.Proximity, 1e-12, "The distance of the tick.");
                double expected = Enumerable.Range(Math.Max(1, t - 4), Math.Min(t, 5)).Average();
                Assert.AreEqual(expected, first.Get(GraphMetricNames.MeanDistance), 1e-12, $"At {t} s: the mean of the last five ticks, of those there are at the start.");

                PersonEdge second = graph.FindEdge("1", "3");
                Assert.AreEqual(t % 2 == 0, second.Has(GraphMetricNames.Distance), "Not measured at this tick.");
                Assert.AreEqual(t >= 2, second.Has(GraphMetricNames.MeanDistance), "Measured within the window.");
                Assert.AreEqual(t >= 2 ? 10.0 : 0.0, second.Get(GraphMetricNames.MeanDistance), 1e-12);

                PersonEdge never = graph.FindEdge("2", "3");
                Assert.IsFalse(never.Has(GraphMetricNames.Distance) || never.Has(GraphMetricNames.MeanDistance), "A pair that is never measured has no distance.");
            }

            // A pair that is no longer measured keeps its mean as long as the window holds a tick, not after.
            Assert.IsTrue(assembler.Next(At(16, 16, null)).FindEdge("1", "3").Has(GraphMetricNames.MeanDistance), "Measured at 12 s, within the 5 s before 16 s.");
            Assert.IsFalse(assembler.Next(At(17, 17, null)).FindEdge("1", "3").Has(GraphMetricNames.MeanDistance), "Nothing within the 5 s before 17 s.");

            // The session starts again: nothing is kept of what followed.
            Assert.AreEqual(3.0, assembler.Next(At(3, 3, null)).FindEdge("1", "2").Get(GraphMetricNames.MeanDistance), 1e-12);

            // The proximity score of the indicator, the default: metres all the same.
            var scores = new SocialGraphAssembler(participants, new SocialGraphOptions { Window = TimeSpan.FromSeconds(5) });
            scores.Next(At(1, 1.0, null));
            IndividualPairCharacteristics half = scores.Next(At(2, 0.5, null));
            Assert.AreEqual(1.5, half.FindEdge("1", "2").Proximity, 1e-12, "A score of 0.5 is half of 3 m.");
            Assert.AreEqual(0.75, half.FindEdge("1", "2").Get(GraphMetricNames.MeanDistance), 1e-12, "Mean of 0 m and 1.5 m.");

            // Without the length of the window the mean is the distance of the tick.
            var unknown = new SocialGraphAssembler(participants, new SocialGraphOptions { ProximityIsScore = false });
            unknown.Next(At(1, 1, null));
            Assert.AreEqual(4.0, unknown.Next(At(2, 4, null)).FindEdge("1", "2").Get(GraphMetricNames.MeanDistance), 1e-12);

            // A graph made on its own has the distance of its tick and no mean.
            Assert.IsFalse(SocialGraph.From(At(1, 1, null), participants).FindEdge("1", "2").Has(GraphMetricNames.MeanDistance));
        }

        [TestMethod]
        public void MeanDistance_ComesWithTheGraphsOfABuiltPipeline()
        {
            var graphs = new List<(DateTime Time, IndividualPairCharacteristics Graph)>();
            var participants = new uint[] { 0, 1, 2 };
            TimeSpan window = TimeSpan.FromSeconds(4);

            IndicesHarness.Run(1, pipeline =>
            {
                // The participants 0 and 1 walk away from each other; 2 stays 1.2 m from 0 and 2 m from 1.
                var timeline = new Timeline().Ticks("Clock", 0.05, 16, 1.0);
                for (int i = 0; i < 160; i++)
                {
                    double t = i * 0.1;
                    timeline.Number("Distance01", t, 0.6 + (0.1 * t));
                    timeline.Number("Distance02", t + 0.001, 1.2);
                    timeline.Number("Distance12", t + 0.002, 2.0);
                }

                timeline.Play(pipeline);

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(participants)
                    .WithWindow(window)
                    .WithClock(timeline.TicksOf("Clock"))
                    .AddIndicator(Indicators.Proximity)
                    .Build();

                ProximityComponent proximity = indices.Get(Indicators.Proximity).Component;
                timeline.NumbersOf("Distance01").PipeTo(proximity.GetDistanceInput(0, 1));
                timeline.NumbersOf("Distance02").PipeTo(proximity.GetDistanceInput(0, 2));
                timeline.NumbersOf("Distance12").PipeTo(proximity.GetDistanceInput(1, 2));

                indices.ToSocialGraph().Do((graph, envelope) => graphs.Add((envelope.OriginatingTime, graph.DeepClone())));
            });

            Assert.IsTrue(graphs.Count >= 10, $"A graph per tick ({graphs.Count}).");
            foreach (var (time, graph) in graphs)
            {
                // The distance of the tick is the one of the proximity index, back in metres.
                double seconds = (time - SyntheticData.Start).TotalSeconds;
                PersonEdge walking = graph.FindEdge("1", "2");
                Assert.AreEqual(0.6 + (0.1 * Math.Floor(seconds * 10) / 10), walking.Proximity, 0.011, $"At {seconds:0.00} s.");
                Assert.AreEqual(1.2, graph.FindEdge("1", "3").Proximity, 1e-9);
                Assert.AreEqual(2.0, graph.FindEdge("2", "3").Get(GraphMetricNames.MeanDistance), 1e-9, "A pair that does not move: its mean is its distance.");

                // The mean is the one of the distances of the graphs of the window.
                List<double> ofTheWindow = graphs.Where(other => other.Time > time - window && other.Time <= time).Select(other => other.Graph.FindEdge("1", "2").Proximity).ToList();
                Assert.AreEqual(ofTheWindow.Average(), walking.Get(GraphMetricNames.MeanDistance), 1e-9, $"At {seconds:0.00} s, over {ofTheWindow.Count} ticks.");
            }

            var last = graphs.Last().Graph.FindEdge("1", "2");
            Assert.IsTrue(last.Get(GraphMetricNames.MeanDistance) < last.Proximity - 0.1, "Walking away: the mean of the window is behind the distance of the instant.");
            Assert.AreEqual(4.0, graphs.Last().Graph.WindowSeconds);
        }

        [TestMethod]
        public void Places_AreWhereTheParticipantsAreAtTheTick_AndOnAverageOverTheWindow()
        {
            var participants = new List<uint> { 0, 1, 2 };
            var assembler = new SocialGraphAssembler(participants, new SocialGraphOptions { Window = TimeSpan.FromSeconds(4) });

            // Four places a second for ten seconds: participant 0 walks along X at 1 m/s,
            // participant 1 stands at (2, 3). Participant 2 has no place.
            for (int i = 0; i <= 40; i++)
            {
                double t = i * 0.25;
                assembler.Place(0, SyntheticData.At(t), t, 0);
                assembler.Place(1, SyntheticData.At(t), 2, 3);
            }

            assembler.Place(1, SyntheticData.At(10.1), double.NaN, 3);

            IndividualPairCharacteristics graph = assembler.Next(new IndexSnapshot { OriginatingTime = SyntheticData.At(6.1) });
            PersonNode walking = graph.FindNode("1");
            Assert.IsTrue(walking.TryGetPlace(false, out double x, out double y));
            Assert.AreEqual(6.0, x, 1e-9, "The last place given before the tick.");
            Assert.AreEqual(0.0, y, 1e-9);
            Assert.IsTrue(walking.TryGetPlace(true, out double meanX, out _));
            Assert.AreEqual(4.125, meanX, 1e-9, "The mean of the places of the 4 s before the tick: 2.25 m to 6 m.");
            Assert.IsTrue(graph.FindNode("2").TryGetPlace(false, out x, out y) && x == 2 && y == 3);
            Assert.IsFalse(graph.FindNode("3").TryGetPlace(false, out _, out _) || graph.FindNode("3").TryGetPlace(true, out _, out _), "No place was given for the third participant.");

            // Without the proximity indicator, the distance of a pair is the one of the places.
            Assert.AreEqual(5.0, graph.FindEdge("1", "2").Proximity, 1e-9, "4 m and 3 m apart: 5 m.");
            Assert.AreEqual(5.0, graph.FindEdge("1", "2").Get(GraphMetricNames.MeanDistance), 1e-9);
            Assert.IsFalse(graph.FindEdge("1", "3").Has(GraphMetricNames.Distance));

            graph = assembler.Next(new IndexSnapshot { OriginatingTime = SyntheticData.At(7.1) });
            Assert.AreEqual(Math.Sqrt(34), graph.FindEdge("1", "2").Proximity, 1e-9, "One second later: 5 m and 3 m apart.");
            Assert.AreEqual((5.0 + Math.Sqrt(34)) / 2, graph.FindEdge("1", "2").Get(GraphMetricNames.MeanDistance), 1e-9, "The mean of the two ticks.");

            // With the proximity indicator, the distance is the one of the index.
            var withProximity = new IndexSnapshot { OriginatingTime = SyntheticData.At(8.1) };
            withProximity.Pair[IndexNames.Proximity] = new Dictionary<ParticipantPair, double> { { new ParticipantPair(0, 1), 0.5 } };
            Assert.AreEqual(1.5, assembler.Next(withProximity).FindEdge("1", "2").Proximity, 1e-9);

            // The places stop at 10 s. Two and a half seconds later a participant has no place at
            // the instant any more, but still a mean place over the window; later, none.
            graph = assembler.Next(new IndexSnapshot { OriginatingTime = SyntheticData.At(12.6) });
            Assert.IsFalse(graph.FindNode("2").TryGetPlace(false, out _, out _), "Not seen for more than 2 s.");
            Assert.IsTrue(graph.FindNode("2").TryGetPlace(true, out x, out y) && x == 2 && y == 3, "Seen within the window.");
            graph = assembler.Next(new IndexSnapshot { OriginatingTime = SyntheticData.At(20) });
            Assert.IsFalse(graph.FindNode("1").TryGetPlace(true, out _, out _) || graph.FindNode("2").TryGetPlace(true, out _, out _));
        }

        [TestMethod]
        public void Places_ComeFromThePositionsOfABuiltPipeline_InBothFrames()
        {
            // The same room in the frame of a Unity scene (Y up) and in the standardised one (Z up):
            // a participant walks along X, 2 m deep in the room, its head 1.7 m high.
            foreach (Vector3 up in new[] { Vector3.UnitY, Vector3.UnitZ })
            {
                var graphs = new List<IndividualPairCharacteristics>();
                Vector3 InTheRoom(double across, double deep) => up == Vector3.UnitY ? new Vector3((float)across, 1.7f, (float)deep) : new Vector3((float)across, (float)deep, 1.7f);

                IndicesHarness.Run(1, pipeline =>
                {
                    var timeline = new Timeline().Ticks("Clock", 0.05, 12, 1.0);
                    for (int i = 0; i < 240; i++)
                    {
                        double t = i * 0.05;
                        timeline.Position("Head0", t, InTheRoom(0.5 * t, 2.0));
                        timeline.Position("Head1", t + 0.001, InTheRoom(1.0, -1.0));
                    }

                    timeline.Play(pipeline);

                    SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                        .WithParticipants(0, 1)
                        .WithWindow(TimeSpan.FromSeconds(4))
                        .WithClock(timeline.TicksOf("Clock"))
                        .AddIndicator(Indicators.Movement, options => options.BodyParts = new List<string> { BodyPartNames.Head })
                        .Build();

                    var heads = new Dictionary<uint, IProducer<Vector3>>
                    {
                        { 0, timeline.PositionsOf("Head0") },
                        { 1, timeline.PositionsOf("Head1") },
                    };
                    foreach (var head in heads)
                    {
                        head.Value.PipeTo(indices.Get(Indicators.Movement).Component.GetPositionInput(head.Key, BodyPartNames.Head));
                    }

                    indices.ToSocialGraph(heads, up).Do(graph => graphs.Add(graph.DeepClone()));
                });

                string frame = up == Vector3.UnitY ? "Y up" : "Z up";
                Assert.IsTrue(graphs.Count >= 6, $"{frame}: a graph per tick ({graphs.Count}).");
                IndividualPairCharacteristics last = graphs.Last();
                Assert.IsTrue(last.FindNode("1").TryGetPlace(false, out double x, out double y), frame);
                Assert.AreEqual(2.0, y, 1e-6, $"{frame}: the second coordinate is how deep in the room, not the height of the head.");
                Assert.IsTrue(x > 4.5 && x < 6.1, $"{frame}: where the participant has walked to ({x:0.00} m).");
                Assert.IsTrue(last.FindNode("1").TryGetPlace(true, out double meanX, out _) && meanX < x - 0.5, $"{frame}: the mean place of the window is behind.");
                Assert.IsTrue(last.FindNode("2").TryGetPlace(false, out double standingX, out double standingY) && Math.Abs(standingX - 1.0) < 1e-6 && Math.Abs(standingY + 1.0) < 1e-6, frame);
                Assert.AreEqual(Math.Sqrt(((x - 1.0) * (x - 1.0)) + 9.0), last.FindEdge("1", "2").Proximity, 1e-6, $"{frame}: the distance of the pair, from the places.");
                Assert.IsTrue(last.FindNode("1").Has(GraphMetricNames.Movement), $"{frame}: with the indices, as before.");
            }
        }

        [TestMethod]
        public void Graph_FollowsTheIndicesOfABuiltPipeline_ForTwoToSixParticipants()
        {
            foreach (int count in new[] { 2, 3, 4, 6 })
            {
                foreach (bool withSynchrony in new[] { true, false })
                {
                    var graphs = new List<IndividualPairCharacteristics>();
                    List<uint> participants = Enumerable.Range(0, count).Select(i => (uint)i).ToList();

                    IndicesHarness.Run(1, pipeline =>
                    {
                        // Each participant moves at its own speed, along its own direction.
                        var timeline = new Timeline().Ticks("Clock", 0.05, 14, 1.0);
                        for (int i = 0; i < 280; i++)
                        {
                            double t = i * 0.05;
                            foreach (uint id in participants)
                            {
                                double travelled = (0.2 * (id + 1) * t) + (0.1 * Math.Sin(t + id));
                                timeline.Position($"Head{id}", t + (0.001 * id), new Vector3((float)(travelled * Math.Cos(id)), 0, (float)(travelled * Math.Sin(id))));
                            }
                        }

                        timeline.Play(pipeline);

                        CollaborationIndicesBuilder builder = new CollaborationIndicesBuilder(pipeline)
                            .WithParticipants(participants.ToArray())
                            .WithWindow(TimeSpan.FromSeconds(6))
                            .WithClock(timeline.TicksOf("Clock"))
                            .AddIndicator(Indicators.Movement, options => options.BodyParts = new List<string> { BodyPartNames.Head });
                        if (withSynchrony)
                        {
                            builder.AddIndicator(Indicators.Synchrony);
                        }

                        SlidingAverageComputation indices = builder.Build();
                        foreach (uint id in participants)
                        {
                            timeline.PositionsOf($"Head{id}").PipeTo(indices.Get(Indicators.Movement).Component.GetPositionInput(id, BodyPartNames.Head));
                            if (withSynchrony)
                            {
                                timeline.PositionsOf($"Head{id}").PipeTo(indices.Get(Indicators.Synchrony).Component.GetPositionInput(id, BodyPartNames.Head));
                            }
                        }

                        indices.ToSocialGraph().Do(graph => graphs.Add(graph.DeepClone()));
                    });

                    string context = $"{count} participants, {(withSynchrony ? "movement and synchrony" : "movement only")}";
                    Assert.IsTrue(graphs.Count >= 6, $"{context}: a graph per tick after the warm-up ({graphs.Count}).");
                    IndividualPairCharacteristics last = graphs.Last();
                    Assert.AreEqual(6.0, last.WindowSeconds, context);
                    Assert.AreEqual(count, last.PersonNodes.Count, context);
                    Assert.AreEqual(count * (count - 1) / 2, last.PersonEdges.Count, context);
                    Assert.IsTrue(last.PersonNodes.All(node => node.Has(GraphMetricNames.Movement) && node.MovementScore > 0), $"{context}: every node has its movement.");
                    Assert.IsTrue(last.PersonNodes.All(node => !node.Has(GraphMetricNames.VerbalParticipation)), $"{context}: nothing about the speech, which is not declared.");
                    Assert.IsTrue(last.PersonNodes.Zip(last.PersonNodes.Skip(1), (slower, faster) => faster.MovementScore > slower.MovementScore).All(ordered => ordered), $"{context}: the participants are told apart.");
                    Assert.AreEqual(withSynchrony, last.PersonEdges.All(edge => edge.Has(GraphMetricNames.Synchrony)), $"{context}: the synchrony of every pair when it is declared.");
                    Assert.AreEqual(withSynchrony, last.PersonEdges.Any(edge => edge.Has(GraphMetricNames.Synchrony)), $"{context}: and of none when it is not.");
                    Assert.IsTrue(last.PersonEdges.All(edge => !edge.HasDirected(GraphMetricNames.GazeOnPeers) && !edge.Has(GraphMetricNames.Distance)), context);
                    Assert.IsFalse(last.PersonGroup.Has(GraphMetricNames.CollaborationScore), $"{context}: no score, it is not asked.");
                }
            }
        }

        // Every index the sociogram draws, with values that say where they come from.
        private static IndexSnapshot FullSnapshot(List<uint> participants)
        {
            var snapshot = new IndexSnapshot { OriginatingTime = SyntheticData.Start };
            snapshot.Individual[IndexNames.VerbalParticipation] = participants.ToDictionary(id => id, id => 0.1 * (id + 1));
            snapshot.Individual[IndexNames.Movement] = participants.ToDictionary(id => id, id => id + 0.5);
            snapshot.Individual[IndexNames.TaskParticipation] = participants.ToDictionary(id => id, id => id + 0.25);

            List<ParticipantPair> pairs = Combinatorics.Pairs(participants).ToList();
            snapshot.Pair[IndexNames.Synchrony] = pairs.ToDictionary(pair => pair, pair => (double)((pair.A * 100) + pair.B));
            snapshot.Pair[IndexNames.JointVisualAttention] = pairs.ToDictionary(pair => pair, pair => (pair.A * 100) + pair.B + 0.5);
            snapshot.Pair[IndexNames.SpeechEquality] = pairs.ToDictionary(pair => pair, pair => 0.5);
            snapshot.DirectedPair[IndexNames.GazeOnPeers] = pairs
                .SelectMany(pair => new[] { new DirectedParticipantPair(pair.A, pair.B), new DirectedParticipantPair(pair.B, pair.A) })
                .ToDictionary(directed => directed, directed => (double)((directed.From * 10) + directed.To));

            snapshot.Group[IndexNames.TalkingMost] = 1;
            snapshot.Group["Dominance"] = 0.2;
            snapshot.Group[IndexNames.CollaborationScore] = 0.7;
            return snapshot;
        }
    }
}
