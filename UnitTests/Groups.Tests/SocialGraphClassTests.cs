// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Psi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.Groups;

namespace Groups.Tests
{
    /// <summary>
    /// The classes of the sociogram (IndividualPairCharacteristics and its nodes, edges and
    /// group): any number of participants, metrics kept by name so that one that is not
    /// computed is absent, and the properties the code written before still reads and writes.
    /// </summary>
    [TestClass]
    public class SocialGraphClassTests
    {
        [TestMethod]
        public void Graph_HasANodePerParticipantAndAnEdgePerPair()
        {
            foreach (int participants in new[] { 0, 1, 2, 3, 4, 7, 12 })
            {
                IndividualPairCharacteristics graph = IndividualPairCharacteristics.ForParticipants(participants);
                CollectionAssert.AreEqual(
                    Enumerable.Range(1, participants).Select(number => number.ToString()).ToList(),
                    graph.PersonNodes.Select(node => node.Id).ToList(),
                    $"{participants} participants.");
                Assert.AreEqual(participants * (participants - 1) / 2, graph.PersonEdges.Count, $"{participants} participants: an edge per pair.");
                Assert.IsNotNull(graph.PersonGroup);

                for (int a = 1; a <= participants; a++)
                {
                    for (int b = a + 1; b <= participants; b++)
                    {
                        PersonEdge edge = graph.FindEdge(b.ToString(), a.ToString());
                        Assert.IsNotNull(edge, $"Edge of {a} and {b}, asked in the other order.");
                        Assert.AreSame(edge, graph.FindEdge(a.ToString(), b.ToString()));
                        Assert.AreEqual(a.ToString(), edge.First, "The lower number first, by number and not as text: 2 before 10.");
                        Assert.AreEqual(b.ToString(), edge.Second);
                    }
                }

                Assert.IsNull(graph.FindNode("99"));
                Assert.IsNull(graph.FindEdge("1", "99"));
            }

            // The first edges are those of three participants, in the order the code written for three expects.
            IndividualPairCharacteristics trio = IndividualPairCharacteristics.ForParticipants(3);
            CollectionAssert.AreEqual(new[] { "1|2", "1|3", "2|3" }, trio.PersonEdges.Select(edge => GraphMetrics.PairKey(edge.First, edge.Second)).ToList());
            IndividualPairCharacteristics five = IndividualPairCharacteristics.ForParticipants(5);
            CollectionAssert.AreEqual(new[] { "1|2", "1|3", "2|3" }, five.PersonEdges.Take(3).Select(edge => GraphMetrics.PairKey(edge.First, edge.Second)).ToList());

            // Any identifiers.
            IndividualPairCharacteristics named = IndividualPairCharacteristics.ForParticipants(new[] { "Alice", "Bob", "Alice" });
            Assert.AreEqual(2, named.PersonNodes.Count, "A participant named twice has one node.");
            Assert.AreEqual("Alice", named.PersonEdges.Single().First);
        }

        [TestMethod]
        public void Edge_ReadsItsDirectionsFromTheNumbersOfItsParticipants()
        {
            // Written as the pipeline before the collaboration indices wrote it: the higher number first.
            var edge = new PersonEdge { FromId_1 = "3", ToId_1 = "1", FromId_2 = "1", ToId_2 = "3" };
            Assert.AreEqual("1", edge.First);
            Assert.AreEqual("3", edge.Second);
            Assert.IsTrue(edge.Links("1", "3") && edge.Links("3", "1"));
            Assert.IsFalse(edge.Links("1", "2"));

            edge.GazeOnPeers12 = 4;
            edge.GazeOnPeers21 = 1;
            Assert.AreEqual(4, edge.ForwardMetrics[GraphMetricNames.GazeOnPeers], "12 is from the first participant to the second.");
            Assert.AreEqual(1, edge.BackwardMetrics[GraphMetricNames.GazeOnPeers]);

            // Whatever the order the pair is given in.
            foreach (PersonEdge between in new[] { PersonEdge.Between("2", "10"), PersonEdge.Between("10", "2") })
            {
                Assert.AreEqual("2", between.First);
                Assert.AreEqual("10", between.Second);
            }

            // An edge that names one participant only, or none.
            Assert.AreEqual("4", new PersonEdge { FromId_1 = "4" }.Second);
            Assert.AreEqual(string.Empty, new PersonEdge().First);
        }

        [TestMethod]
        public void AMetricThatIsNotComputedIsAbsent_NotZero()
        {
            var node = new PersonNode { Id = "1" };
            Assert.IsFalse(node.Has(GraphMetricNames.VerbalParticipation));
            Assert.AreEqual(0.0, node.SpeakingTime, "Read as before, it gives 0.");
            Assert.AreEqual(-1.0, node.Get(GraphMetricNames.VerbalParticipation, -1), "Or what the caller asks for.");

            node.SpeakingTime = 0;
            Assert.IsTrue(node.Has(GraphMetricNames.VerbalParticipation), "A speaking time of zero is a value.");

            node.Set(GraphMetricNames.Movement, double.NaN);
            Assert.IsFalse(node.Has(GraphMetricNames.Movement), "What is not a number is not a value.");
            Assert.AreEqual(0.0, node.MovementScore);

            var edge = PersonEdge.Between("1", "2");
            Assert.IsFalse(edge.Has(GraphMetricNames.Synchrony) || edge.HasDirected(GraphMetricNames.GazeOnPeers));
            edge.GazeOnPeers21 = 0;
            Assert.IsTrue(edge.HasDirected(GraphMetricNames.GazeOnPeers), "Known in one direction is known.");

            var group = new PersonGroup();
            Assert.IsFalse(group.Has(GraphMetricNames.CollaborationScore));
            group.Collaboration_score = 0.4;
            Assert.IsTrue(group.Has(GraphMetricNames.CollaborationScore));

            // Metrics left null by whoever built the graph.
            var bare = new PersonNode { Id = "2", Metrics = null };
            Assert.IsFalse(bare.Has(GraphMetricNames.Movement));
            Assert.AreEqual(0.0, bare.MovementScore);
            bare.MovementScore = 0.3;
            Assert.AreEqual(0.3, bare.Metrics[GraphMetricNames.Movement]);
        }

        [TestMethod]
        public void TheNamedPropertiesReadAndWriteTheMetricsByName()
        {
            var node = new PersonNode { Id = "1", SpeakingTime = 0.1, MovementScore = 0.2, TaskParticipation = 0.3 };
            CollectionAssert.AreEquivalent(
                new[] { "VerbalParticipation=0.1", "Movement=0.2", "TaskParticipation=0.3" },
                node.Metrics.Select(metric => $"{metric.Key}={metric.Value}").ToList(),
                "Under the names of the collaboration indices.");

            var edge = PersonEdge.Between("1", "2");
            edge.Proximity = 1.5;
            edge.Synchrony = 0.6;
            edge.JVAEvent = 0.7;
            edge.JVAIntensity = 0.8;
            edge.SpeechEquality = -1;
            edge.TaskEquality = 0.9;
            CollectionAssert.AreEquivalent(
                new[] { "Distance=1.5", "Synchrony=0.6", "JointVisualAttention=0.7", "JointVisualAttentionIntensity=0.8", "SpeechEquality=-1", "TaskEquality=0.9" },
                edge.Metrics.Select(metric => $"{metric.Key}={metric.Value}").ToList());

            var group = new PersonGroup
            {
                TaskingMost = 1, TalkingMost = 2, WatchedMost = 3, LeadVA = 4,
                Dominance_score = 0.1, Ja_score = 0.2, Engagement_score = 0.3, Cpm_score = 0.4, Spatial_score = 0.5, Dimension2_score = 0.6, Collaboration_score = 0.7,
            };
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "TaskingMost=1", "TalkingMost=2", "WatchedMost=3", "LeadVisualAttention=4",
                    "Dominance=0.1", "JointAttention=0.2", "Engagement=0.3", "CommunicationProcessManagement=0.4", "SpatialBehaviour=0.5", "Dimension2=0.6", "CollaborationScore=0.7",
                },
                group.Metrics.Select(metric => $"{metric.Key}={metric.Value}").ToList());

            // And the other way: what is given by name is read by the property.
            var byName = new PersonGroup();
            byName.Set("CollaborationScore", 0.42);
            byName.Set("Engagement", 0.24);
            Assert.AreEqual(0.42, byName.Collaboration_score);
            Assert.AreEqual(0.24, byName.Engagement_score);
            var edgeByName = PersonEdge.Between("1", "2");
            edgeByName.Set("Synchrony", 0.9);
            edgeByName.ForwardMetrics["GazeOnPeers"] = 3;
            Assert.AreEqual(0.9, edgeByName.Synchrony);
            Assert.AreEqual(3, edgeByName.GazeOnPeers12);

            // A metric the sociogram does not know is kept all the same.
            edgeByName.Set("CrossTalk", 4.5);
            Assert.AreEqual(4.5, edgeByName.Get("CrossTalk"));
        }

        [TestMethod]
        public void Graph_IsReadBackFromAStoreAsItWasWritten()
        {
            // Two, four and six participants, with all, some and none of the metrics.
            var written = new List<IndividualPairCharacteristics>();
            foreach (int participants in new[] { 2, 4, 6 })
            {
                IndividualPairCharacteristics graph = IndividualPairCharacteristics.ForParticipants(participants);
                graph.WindowSeconds = 10 * participants;
                if (participants != 6)
                {
                    foreach (PersonNode node in graph.PersonNodes)
                    {
                        node.SpeakingTime = 0.1 * int.Parse(node.Id);
                    }

                    foreach (PersonEdge edge in graph.PersonEdges)
                    {
                        edge.Synchrony = 0.01 * int.Parse(edge.First) * int.Parse(edge.Second);
                        edge.GazeOnPeers21 = int.Parse(edge.Second);
                    }

                    graph.PersonGroup.Collaboration_score = 0.5;
                }

                if (participants == 4)
                {
                    graph.PersonGroup.ScoreNames = new List<string> { "Coordination", GraphMetricNames.CollaborationScore };
                    graph.PersonGroup.Set("Coordination", 0.75);
                    graph.FindEdge("1", "4").Proximity = 2.5;
                }

                written.Add(graph);
            }

            string folder = Path.Combine(Path.GetTempPath(), "SaacSocialGraphTests_" + Guid.NewGuid().ToString("N"));
            var read = new List<IndividualPairCharacteristics>();
            try
            {
                DateTime start = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
                using (Pipeline pipeline = Pipeline.Create("Write"))
                {
                    var store = PsiStore.Create(pipeline, "Graph", folder);
                    Generators.Sequence(pipeline, written.Select((graph, index) => (graph, start.AddSeconds(index))).ToList()).Write("SocialGraph", store);
                    pipeline.Run();
                }

                using (Pipeline pipeline = Pipeline.Create("Read"))
                {
                    var store = PsiStore.Open(pipeline, "Graph", folder);
                    store.OpenStream<IndividualPairCharacteristics>("SocialGraph").Do(graph => read.Add(graph.DeepClone()));
                    pipeline.Run(ReplayDescriptor.ReplayAll);
                }
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }

            Assert.AreEqual(written.Count, read.Count);
            for (int i = 0; i < written.Count; i++)
            {
                Assert.AreEqual(Describe(written[i]), Describe(read[i]), $"Graph {i}.");
            }

            StringAssert.Contains(Describe(read[1]), "Coordination=0.75");
            StringAssert.Contains(Describe(read[1]), "1-4 Distance=2.5");
            Assert.IsTrue(read[2].PersonNodes.All(node => node.Metrics.Count == 0), "A graph without metrics comes back without metrics.");
        }

        // Everything a graph holds, as text.
        private static string Describe(IndividualPairCharacteristics graph)
        {
            string Metrics(Dictionary<string, double> metrics) => string.Join(",", metrics.OrderBy(metric => metric.Key).Select(metric => $"{metric.Key}={metric.Value}"));
            var lines = new List<string> { $"window {graph.WindowSeconds}" };
            lines.AddRange(graph.PersonNodes.Select(node => $"{node.Id} {Metrics(node.Metrics)}"));
            lines.AddRange(graph.PersonEdges.Select(edge => $"{edge.First}-{edge.Second} {Metrics(edge.Metrics)} > {Metrics(edge.ForwardMetrics)} < {Metrics(edge.BackwardMetrics)}"));
            lines.Add($"group {Metrics(graph.PersonGroup.Metrics)} scores {string.Join(",", graph.PersonGroup.ScoreNames)}");
            return string.Join("\n", lines);
        }
    }
}
