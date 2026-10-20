// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.Psi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.CollaborationIndices;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// An indicator written outside the library: everything below lives in this test project,
    /// and the library is used as it is. It is declared like any other, by catalogue entry, by
    /// class and by name, and reaches the score, the snapshot and the export.
    /// </summary>
    [TestClass]
    public class CustomIndicatorTests
    {
        /// <summary>Catalogue entry of the custom indicator, registered once for the configuration files.</summary>
        private static readonly IndicatorType<HeadHeightIndicator, HeadHeightOptions> HeadHeight
            = IndicatorRegistry.Register<HeadHeightIndicator, HeadHeightOptions>("HeadHeight");

        [TestMethod]
        public void CustomIndicator_IsComputedScoredAndExported()
        {
            var heights = new List<Dictionary<uint, double>>();
            var snapshots = new List<IndexSnapshot>();
            var writer = new StringWriter();

            IndicesHarness.Run(1, pipeline =>
            {
                var player = new SessionPlayer(pipeline, IndicesHarness.Second);

                SlidingAverageComputation indices = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(SyntheticData.Participants)
                    .WithWindow(TimeSpan.FromSeconds(10))
                    .AddIndicator(Indicators.Movement)
                    .AddIndicator(HeadHeight, options => options.Offset = 0.5)
                    .WithCollaborationScore(new[]
                    {
                        new ScoreDimension { Name = "Posture", IndexNames = new List<string> { "HeadHeight" }, UseLogCompression = false },
                    })
                    .WithCsvExport(writer)
                    .Build();

                IndicesHarness.Connect(player, indices);

                HeadHeightComponent component = indices.Get(HeadHeight).Component;
                foreach (uint id in SyntheticData.Participants)
                {
                    player.Positions(id, BodyPartNames.Head).PipeTo(component.GetHeadInput(id));
                }

                component.Out.Do(values => heights.Add(new Dictionary<uint, double>(values)));
                indices.SnapshotOut.Do(snapshot => snapshots.Add(snapshot.Clone()));
            });

            Assert.AreEqual(50, heights.Count, "Paced by the shared clock: one value per second after the warm-up.");
            Assert.AreEqual(50, snapshots.Count);

            for (int i = 0; i < heights.Count; i++)
            {
                // The heads of the synthetic session oscillate a few centimetres around 1.6 m.
                foreach (uint id in new uint[] { 0, 1 })
                {
                    Assert.AreEqual(1.6 + 0.5, heights[i][id], 0.03, $"Mean head height of participant {id}, plus the offset set in the options");
                }

                double group = heights[i].Values.Average();
                Assert.AreEqual(group, snapshots[i].Group["HeadHeight"], 1e-12, "Same tick, same value in the snapshot.");
                Assert.AreEqual(heights[i][0], snapshots[i].Individual["HeadHeight"][0], 1e-12);
                Assert.AreEqual(group, snapshots[i].Group["Posture"], 1e-12, "The dimension holds this index only, without compression.");
                Assert.AreEqual(group, snapshots[i].Group[IndexNames.CollaborationScore], 1e-12);
            }

            List<string> rows = IndicesHarness.Rows(writer);
            string[] header = rows[0].Split(';');
            CollectionAssert.Contains(header, "HeadHeight");
            CollectionAssert.Contains(header, "HeadHeight_2");
            CollectionAssert.Contains(header, "Posture");

            string[] firstRow = rows[1].Split(';');
            Assert.AreEqual(
                heights[0].Values.Average().ToString("0.######", CultureInfo.InvariantCulture),
                firstRow[Array.IndexOf(header, "HeadHeight")]);
        }

        [TestMethod]
        public void CustomIndicator_CanBeDeclaredByClassAndByName()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                SlidingAverageComputation byClass = new CollaborationIndicesBuilder(pipeline)
                    .WithName("ByClass")
                    .WithParticipants(0, 1)
                    .AddIndicator<HeadHeightIndicator>(indicator => indicator.Options.Offset = 0.25)
                    .Build();

                CollaborationIndicesConfiguration configuration = CollaborationIndicesConfiguration.FromJson(@"{
                    ""Name"": ""ByName"",
                    ""ParticipantIds"": [0, 1],
                    ""Indicators"": [ { ""Type"": ""HeadHeight"", ""Options"": { ""Offset"": 0.25 } } ]
                }");
                SlidingAverageComputation byName = CollaborationIndicesBuilder.FromConfiguration(pipeline, configuration).Build();

                Assert.AreEqual(0.25, byClass.Get<HeadHeightIndicator>().Options.Offset);
                Assert.AreEqual(0.25, byName.Get(HeadHeight).Options.Offset);
                CollectionAssert.Contains(IndicatorRegistry.Names.ToList(), "HeadHeight");
            }
        }

        [TestMethod]
        public void CustomIndicator_ValidatesItsOwnOptions()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                CollaborationIndicesBuilder builder = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 1)
                    .AddIndicator(HeadHeight, options => options.Offset = double.NaN);

                CollaborationIndicesConfigurationException exception = Assert.ThrowsException<CollaborationIndicesConfigurationException>(() => builder.Build());

                StringAssert.Contains(exception.Message, "Indicator 'HeadHeight': Offset must be a number");
            }
        }

        /// <summary>Options of the custom indicator.</summary>
        public class HeadHeightOptions : IndicatorOptions
        {
            /// <summary>Gets or sets a constant added to every height.</summary>
            public double Offset { get; set; } = 0;
        }

        /// <summary>Configuration of the custom component.</summary>
        public class HeadHeightConfiguration : IndexComponentConfiguration
        {
            /// <summary>Gets or sets a constant added to every height.</summary>
            public double Offset { get; set; } = 0;
        }

        /// <summary>
        /// The custom component: mean height of the head of each participant over the window.
        /// The base class brings the clock input, the completion stream and the throttling.
        /// </summary>
        public class HeadHeightComponent : IndexComponentBase<HeadHeightConfiguration>
        {
            private readonly Dictionary<uint, Receiver<Vector3>> inputs = new Dictionary<uint, Receiver<Vector3>>();
            private readonly Dictionary<uint, List<KeyValuePair<DateTime, double>>> samples = new Dictionary<uint, List<KeyValuePair<DateTime, double>>>();

            /// <summary>Initializes a new instance of the <see cref="HeadHeightComponent"/> class.</summary>
            /// <param name="pipeline">Pipeline hosting the component.</param>
            /// <param name="configuration">Configuration of the component.</param>
            /// <param name="name">Name of the component.</param>
            public HeadHeightComponent(Pipeline pipeline, HeadHeightConfiguration configuration, string name)
                : base(pipeline, configuration, name)
            {
                this.Out = pipeline.CreateEmitter<Dictionary<uint, double>>(this, $"{name}-Individual");
                this.GroupOut = pipeline.CreateEmitter<double>(this, $"{name}-Group");

                foreach (uint participantId in configuration.ParticipantIds)
                {
                    uint id = participantId;
                    this.samples[id] = new List<KeyValuePair<DateTime, double>>();
                    this.inputs[id] = pipeline.CreateReceiver<Vector3>(
                        this,
                        (position, envelope) => this.samples[id].Add(new KeyValuePair<DateTime, double>(envelope.OriginatingTime, position.Y)),
                        $"{name}-Head-{id}");
                }
            }

            /// <summary>Gets the mean height of each participant.</summary>
            public Emitter<Dictionary<uint, double>> Out { get; }

            /// <summary>Gets the mean height of the group.</summary>
            public Emitter<double> GroupOut { get; }

            /// <summary>Head position input of one participant.</summary>
            /// <param name="participantId">The participant.</param>
            /// <returns>The receiver.</returns>
            public Receiver<Vector3> GetHeadInput(uint participantId) => this.inputs[participantId];

            /// <inheritdoc/>
            protected override void Prune(DateTime oldestAllowed)
            {
                foreach (var list in this.samples.Values)
                {
                    list.RemoveAll(sample => sample.Key < oldestAllowed);
                }
            }

            /// <inheritdoc/>
            protected override void Compute(DateTime originatingTime)
            {
                DateTime start = this.WindowStart(originatingTime);
                var heights = new Dictionary<uint, double>();
                foreach (var entry in this.samples)
                {
                    List<double> inWindow = entry.Value.Where(sample => sample.Key >= start && sample.Key <= originatingTime).Select(sample => sample.Value).ToList();
                    heights[entry.Key] = (inWindow.Count == 0 ? 0 : inWindow.Average()) + this.configuration.Offset;
                }

                this.Out.Post(heights, originatingTime);
                this.GroupOut.Post(heights.Values.Average(), originatingTime);
            }
        }

        /// <summary>The custom indicator: what the builder needs to know about the component.</summary>
        public class HeadHeightIndicator : ComponentIndicator<HeadHeightOptions, HeadHeightComponent>
        {
            /// <inheritdoc/>
            public override string DefaultName => "HeadHeight";

            /// <inheritdoc/>
            public override IEnumerable<string> Validate(IndicatorBuildContext context)
            {
                if (double.IsNaN(this.Options.Offset))
                {
                    yield return $"Indicator '{this.Name}': Offset must be a number.";
                }
            }

            /// <inheritdoc/>
            public override void Build(IndicatorBuildContext context)
            {
                var configuration = new HeadHeightConfiguration
                {
                    ParticipantIds = context.CopyParticipantIds(),
                    WindowDuration = context.WindowDuration,
                    ComputationInterval = TimeSpan.Zero,
                    Offset = this.Options.Offset,
                };

                this.Component = new HeadHeightComponent(context.Pipeline, configuration, context.ComponentName(this.Name));

                context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);
                context.PublishIndividual(this.Name, this.Component.Out);
                context.PublishGroup(this.Name, this.Component.GroupOut);
                context.PublishScoreInput(this.Name, this.Component.GroupOut);
            }
        }
    }
}
