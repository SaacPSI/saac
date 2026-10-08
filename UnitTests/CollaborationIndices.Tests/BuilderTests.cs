// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Psi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.CollaborationIndices;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// Construction of a set of indices, by the builder and by a configuration object.
    /// </summary>
    [TestClass]
    public class BuilderTests
    {
        private const string HandWrittenJson = @"{
  ""Name"": ""Session12"",
  ""ParticipantIds"": [0, 1, 2],
  ""WindowDuration"": ""00:00:20"",
  ""ComputationInterval"": ""00:00:01"",
  ""Indicators"": [
    { ""Type"": ""Movement"", ""Options"": { ""BodyParts"": [""Head""], ""Unit"": ""DisplacementPerSample"" } },
    { ""Type"": ""Synchrony"", ""Options"": { ""SubsetSize"": 0, ""WindowDuration"": ""00:00:05"" } },
    { ""Type"": ""VerbalParticipation"" },
    { ""Type"": ""SpeechEquality"" },
    { ""Type"": ""Equality"", ""Options"": { ""Name"": ""MovementEquality"", ""Source"": ""Movement"" } }
  ],
  ""ComputeCollaborationScores"": true
}";

        [TestMethod]
        public void Build_CreatesTheDeclaredIndicatorsAndNothingElse()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1, 2)
                    .WithWindow(TimeSpan.FromSeconds(30))
                    .AddIndicator(Indicators.Movement)
                    .AddIndicator(Indicators.Synchrony, options => options.SubsetSize = 0)
                    .Build();

                CollectionAssert.AreEqual(new[] { "Movement", "Synchrony" }, indices.Indicators.Select(i => i.Name).ToList());
                Assert.AreEqual(TimeSpan.FromSeconds(30), indices.WindowDuration);
                CollectionAssert.AreEqual(new uint[] { 0, 1, 2 }, indices.ParticipantIds.ToList());

                Assert.IsNotNull(indices.ActivityLevel);
                Assert.IsNotNull(indices.Synchrony);
                Assert.IsNull(indices.VerbalParticipation, "Not declared, so not created.");
                Assert.IsNull(indices.CollaborationScore, "The score is only computed when asked for.");
                Assert.IsNull(indices.Graph);
                Assert.IsNull(indices.Export);
                Assert.IsNotNull(indices.SnapshotOut);
            }
        }

        [TestMethod]
        public void Options_ReachTheComponents()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1, 2)
                    .WithWindow(TimeSpan.FromSeconds(30))
                    .AddIndicator(Indicators.Movement, options =>
                    {
                        options.BodyParts = new List<string> { BodyPartNames.Head };
                        options.AdditionalWindows = new List<TimeSpan> { TimeSpan.FromSeconds(5) };
                        options.Configure = configuration => configuration.MinimumSampleCount = 7;
                    })
                    .AddIndicator(Indicators.Synchrony, options => options.SubsetSize = 0)
                    .Build();

                PhysicalActivityLevelConfiguration movement = indices.Get(Indicators.Movement).Component.Configuration;
                CollectionAssert.AreEqual(new[] { BodyPartNames.Head }, movement.BodyParts);
                Assert.AreEqual(TimeSpan.FromSeconds(30), movement.WindowDuration);
                Assert.AreEqual(7, movement.MinimumSampleCount, "The Configure hook reaches every setting of the component.");
                Assert.IsFalse(movement.ComputeOnDataReception, "The indices are paced by the shared clock.");

                Assert.IsFalse(indices.Get(Indicators.Synchrony).Component.Configuration.ComputeSubsets);
            }
        }

        [TestMethod]
        public void Indicator_CanHaveItsOwnWindow()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1)
                    .WithWindow(TimeSpan.FromSeconds(30))
                    .AddIndicator(Indicators.Movement)
                    .AddIndicator(Indicators.Synchrony, options => options.WindowDuration = TimeSpan.FromSeconds(5))
                    .Build();

                Assert.AreEqual(TimeSpan.FromSeconds(30), indices.Get(Indicators.Movement).Component.Configuration.WindowDuration);
                Assert.AreEqual(TimeSpan.FromSeconds(5), indices.Get(Indicators.Synchrony).Component.Configuration.WindowDuration);
            }
        }

        [TestMethod]
        public void Get_FindsAnIndicatorByCatalogueEntryByClassAndByName()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1, 2)
                    .AddIndicator(Indicators.Movement)
                    .AddIndicator(Indicators.VerbalParticipation)
                    .AddIndicator(Indicators.SpeechEquality)
                    .AddIndicator(Indicators.Equality, options =>
                    {
                        options.Name = "MovementEquality";
                        options.Source = IndexNames.Movement;
                    })
                    .Build();

                Assert.AreSame(indices.Get(Indicators.Movement), indices.Get<PhysicalActivityIndicator>());
                Assert.AreSame(indices.Get(Indicators.SpeechEquality), indices.Get<EqualityIndicator>(IndexNames.SpeechEquality));
                Assert.AreEqual("MovementEquality", indices.Get<EqualityIndicator>("MovementEquality").Name);
                Assert.IsTrue(indices.Contains("MovementEquality"));
                Assert.IsNull(indices.Find<PhysicalSynchronyIndicator>());

                // Two equality indicators: the class alone is ambiguous.
                Assert.ThrowsException<InvalidOperationException>(() => indices.Get<EqualityIndicator>());
                Assert.ThrowsException<InvalidOperationException>(() => indices.Get(Indicators.Synchrony));
            }
        }

        [TestMethod]
        public void Dependencies_AreBuiltFirst_WhateverTheDeclarationOrder()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1, 2)
                    .AddIndicator(Indicators.TalkingMost)
                    .AddIndicator(Indicators.SpeechEquality)
                    .AddIndicator(Indicators.VerbalParticipation)
                    .Build();

                CollectionAssert.AreEqual(
                    new[] { "VerbalParticipation", "TalkingMost", "SpeechEquality" },
                    indices.Indicators.Select(i => i.Name).ToList());
            }
        }

        [TestMethod]
        public void Outputs_AreDeclaredByTheIndicators()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1, 2)
                    .AddIndicator(Indicators.Movement)
                    .AddIndicator(Indicators.Synchrony)
                    .AddIndicator(Indicators.GazeOnPeers)
                    .Build();

                CollectionAssert.AreEqual(new[] { "Movement", "Synchrony", "GazeOnPeers" }, indices.Outputs.Groups.Select(o => o.Name).ToList());
                CollectionAssert.AreEqual(new[] { "Movement" }, indices.Outputs.Individuals.Select(o => o.Name).ToList());
                CollectionAssert.AreEqual(new[] { "Synchrony" }, indices.Outputs.Pairs.Select(o => o.Name).ToList());
                CollectionAssert.AreEqual(new[] { "GazeOnPeers" }, indices.Outputs.DirectedPairs.Select(o => o.Name).ToList());
                CollectionAssert.AreEqual(new[] { "Movement", "Synchrony", "GazeOnPeers" }, indices.Outputs.ScoreInputs.Select(o => o.Name).ToList());

                Assert.IsNotNull(indices.Outputs.Group(IndexNames.Movement));
                Assert.ThrowsException<ArgumentException>(() => indices.Outputs.Group("Unknown"));
            }
        }

        [TestMethod]
        public void IncludeInScore_KeepsAnIndicatorOutOfTheScoreOnly()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1, 2)
                    .AddIndicator(Indicators.Movement)
                    .AddIndicator(Indicators.Synchrony, options => options.IncludeInScore = false)
                    .Build();

                CollectionAssert.AreEqual(new[] { "Movement" }, indices.Outputs.ScoreInputs.Select(o => o.Name).ToList());
                CollectionAssert.Contains(indices.Outputs.Groups.Select(o => o.Name).ToList(), "Synchrony");
            }
        }

        [TestMethod]
        public void Export_DefaultColumns_FollowTheDeclaredIndicators()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                var writer = new StringWriter();
                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 2)
                    .AddIndicator(Indicators.Movement)
                    .AddIndicator(Indicators.Synchrony)
                    .WithCollaborationScore(new[] { new ScoreDimension { Name = "Body", IndexNames = new List<string> { IndexNames.Movement, IndexNames.Synchrony } } })
                    .WithCsvExport(writer)
                    .Build();

                CollectionAssert.AreEqual(
                    new[] { "Movement", "Movement_0", "Movement_2", "Synchrony", "Synchrony_0-2", "Body", "CollaborationScore" },
                    indices.Export.Columns.ToList());
            }
        }

        [TestMethod]
        public void Configuration_BuildsTheSameIndicesAsTheBuilder()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                CollaborationIndicesBuilder builder = new CollaborationIndicesBuilder(pipeline)
                    .WithName("A")
                    .WithParticipants(0, 1, 2)
                    .WithWindow(TimeSpan.FromSeconds(20))
                    .AddIndicator(Indicators.Movement, options => options.BodyParts = new List<string> { BodyPartNames.Head })
                    .AddIndicator(Indicators.Synchrony, options => options.SubsetSize = 0)
                    .AddIndicator(Indicators.VerbalParticipation)
                    .AddIndicator(Indicators.SpeechEquality)
                    .WithCollaborationScore();

                var configuration = new CollaborationIndicesConfiguration
                {
                    Name = "B",
                    ParticipantIds = new List<uint> { 0, 1, 2 },
                    WindowDuration = TimeSpan.FromSeconds(20),
                    Indicators = new List<IndicatorConfiguration>
                    {
                        new IndicatorConfiguration { Type = "Movement", Options = new Dictionary<string, object> { { "BodyParts", new[] { "Head" } } } },
                        new IndicatorConfiguration { Type = "Synchrony", Options = new Dictionary<string, object> { { "SubsetSize", 0 } } },
                        new IndicatorConfiguration { Type = "VerbalParticipation" },
                        new IndicatorConfiguration { Type = "SpeechEquality" },
                    },
                    ComputeCollaborationScores = true,
                };

                SlidingAverageComputation fromBuilder = builder.Build();
                SlidingAverageComputation fromConfiguration = CollaborationIndicesBuilder.FromConfiguration(pipeline, configuration).Build();

                CollectionAssert.AreEqual(Describe(fromBuilder), Describe(fromConfiguration));
                Assert.IsNotNull(fromConfiguration.CollaborationScore);
                CollectionAssert.AreEqual(
                    fromBuilder.Get(Indicators.Movement).Component.Configuration.BodyParts,
                    fromConfiguration.Get(Indicators.Movement).Component.Configuration.BodyParts);
                Assert.IsFalse(fromConfiguration.Get(Indicators.Synchrony).Component.Configuration.ComputeSubsets);
            }
        }

        [TestMethod]
        public void Configuration_ReadsHandWrittenJson()
        {
            CollaborationIndicesConfiguration configuration = CollaborationIndicesConfiguration.FromJson(HandWrittenJson);

            Assert.AreEqual("Session12", configuration.Name);
            Assert.AreEqual(TimeSpan.FromSeconds(20), configuration.WindowDuration);
            Assert.AreEqual(5, configuration.Indicators.Count);

            using (Pipeline pipeline = Pipeline.Create())
            {
                SlidingAverageComputation indices = CollaborationIndicesBuilder.FromConfiguration(pipeline, configuration).Build();

                Assert.AreEqual("Session12", indices.Name);
                CollectionAssert.AreEqual(
                    new[] { "Movement", "Synchrony", "VerbalParticipation", "SpeechEquality", "MovementEquality" },
                    indices.Indicators.Select(i => i.Name).ToList());

                PhysicalActivityLevelConfiguration movement = indices.Get(Indicators.Movement).Component.Configuration;
                CollectionAssert.AreEqual(new[] { "Head" }, movement.BodyParts, "A list of the file replaces the default list, it is not appended to it.");
                Assert.AreEqual(MovementUnit.DisplacementPerSample, movement.Unit);

                PhysicalSynchronyConfiguration synchrony = indices.Get(Indicators.Synchrony).Component.Configuration;
                Assert.AreEqual(TimeSpan.FromSeconds(5), synchrony.WindowDuration, "This indicator has its own window.");
                Assert.IsFalse(synchrony.ComputeSubsets);

                Assert.IsNotNull(indices.CollaborationScore);
            }
        }

        [TestMethod]
        public void Configuration_SurvivesAJsonRoundTrip()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                CollaborationIndicesConfiguration original = new CollaborationIndicesBuilder(pipeline)
                    .WithName("RoundTrip")
                    .WithParticipants(4, 9)
                    .WithWindow(TimeSpan.FromSeconds(45))
                    .WithComputationInterval(TimeSpan.FromMilliseconds(500))
                    .WithPhaseGate(true)
                    .WithCalibration(IndexCalibration.Threshold45Seconds())
                    .AddIndicator(Indicators.Movement, options => options.AdditionalWindows = new List<TimeSpan> { TimeSpan.FromSeconds(5) })
                    .AddIndicator(Indicators.TimeInArea, options => options.Areas = new List<string> { "Table", "Board" })
                    .AddIndicator(Indicators.Synchrony, options => options.Normalization = SynchronyNormalization.Absolute)
                    .WithCollaborationScore()
                    .WithInteractionGraph()
                    .ToConfiguration();

                string json = original.ToJson();
                CollaborationIndicesConfiguration restored = CollaborationIndicesConfiguration.FromJson(json);

                Assert.AreEqual(json, restored.ToJson(), "Reading the JSON back and writing it again gives the same text.");
                Assert.AreEqual("RoundTrip", restored.Name);
                CollectionAssert.AreEqual(new uint[] { 4, 9 }, restored.ParticipantIds);
                Assert.AreEqual(TimeSpan.FromSeconds(45), restored.WindowDuration);
                Assert.AreEqual(TimeSpan.FromMilliseconds(500), restored.ComputationInterval);
                Assert.IsTrue(restored.RequirePhase);
                Assert.IsTrue(restored.GenerateGraph);
                Assert.AreEqual(18.0, restored.Calibration.ReferenceValues[IndexNames.JointVisualAttention]);
                StringAssert.Contains(json, "\"Absolute\"", "Enumerations are written by name.");

                SlidingAverageComputation indices = CollaborationIndicesBuilder.FromConfiguration(pipeline, restored).Build();
                Assert.AreEqual(SynchronyNormalization.Absolute, indices.Get(Indicators.Synchrony).Component.Configuration.Normalization);
                CollectionAssert.AreEqual(new[] { "Table", "Board" }, indices.Get(Indicators.TimeInArea).Component.Configuration.Areas);
                CollectionAssert.AreEqual(new[] { TimeSpan.FromSeconds(5) }, indices.Get(Indicators.Movement).Component.Configuration.AdditionalWindows);
            }
        }

        [TestMethod]
        public void Catalogue_NamesAreTheConfigurationTypes()
        {
            IReadOnlyList<string> names = IndicatorRegistry.Names;

            foreach (string expected in new[]
            {
                "Movement", "Synchrony", "VerbalParticipation", "SpeechEquality", "TalkingMost", "TurnTaking",
                "JointVisualAttention", "GazeOnPeers", "AttentionLevel", "TaskParticipation", "TaskEquality",
                "TaskingMost", "TimeInArea", "Formation", "Proximity", "Equality", "Dominance",
            })
            {
                CollectionAssert.Contains(names.ToList(), expected);
            }

            Assert.AreEqual("Synchrony", Indicators.Synchrony.Name);
        }

        [TestMethod]
        public void EveryCatalogueIndicator_CanBeBuiltTogether()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                var writer = new StringWriter();
                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1, 2)
                    .AddIndicator(Indicators.Movement)
                    .AddIndicator(Indicators.Synchrony)
                    .AddIndicator(Indicators.VerbalParticipation)
                    .AddIndicator(Indicators.SpeechEquality)
                    .AddIndicator(Indicators.TalkingMost)
                    .AddIndicator(Indicators.TurnTaking)
                    .AddIndicator(Indicators.Silence)
                    .AddIndicator(Indicators.CrossTalk)
                    .AddIndicator(Indicators.JointVisualAttention)
                    .AddIndicator(Indicators.GazeOnPeers)
                    .AddIndicator(Indicators.MutualGaze)
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

                Assert.AreEqual(18, indices.Indicators.Count);
                Assert.IsNotNull(indices.TurnTaking, "Turn taking is part of the design even though the legacy pipeline no longer instantiated it.");

                // Every index of the default dimensions is produced by some indicator.
                List<string> scoreInputs = indices.Outputs.ScoreInputs.Select(o => o.Name).ToList();
                foreach (ScoreDimension dimension in SlidingAverageComputation.DefaultDimensions())
                {
                    foreach (string index in dimension.IndexNames)
                    {
                        CollectionAssert.Contains(scoreInputs, index, $"Dimension {dimension.Name}");
                    }
                }
            }
        }

        private static List<string> Describe(SlidingAverageComputation indices)
        {
            var description = new List<string>();
            description.AddRange(indices.Indicators.Select(i => $"indicator {i.Name} ({i.GetType().Name})"));
            description.AddRange(indices.Outputs.Groups.Select(o => $"group {o.Name}"));
            description.AddRange(indices.Outputs.Individuals.Select(o => $"individual {o.Name}"));
            description.AddRange(indices.Outputs.Pairs.Select(o => $"pair {o.Name}"));
            description.AddRange(indices.Outputs.ScoreInputs.Select(o => $"score {o.Name}"));
            return description;
        }
    }
}
