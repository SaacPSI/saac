// <copyright file="SlidingAverageComputation.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// A set of collaboration indices computed on a sliding window, as built by
    /// <see cref="CollaborationIndicesBuilder"/>.
    ///
    /// This class knows no indicator in particular. It creates the phase gate, asks each
    /// declared indicator to build itself, and wires the score, the graph and the export from
    /// what the indicators declared. Which indices exist, what they need and what they publish
    /// is decided by the builder and by the indicators, never here.
    ///
    /// Once built: connect the raw streams to the inputs of the indicators
    /// (<c>Get(Indicators.Movement).Component.GetPositionInput(...)</c>), a clock to
    /// <see cref="ClockIn"/> unless the builder was given one, and the phase boundaries to
    /// <see cref="Gate"/> if phases are required.
    /// </summary>
    public partial class SlidingAverageComputation
    {
        private readonly Pipeline pipeline;
        private readonly IndicesBuildState state;

        /// <summary>
        /// Initializes a new instance of the <see cref="SlidingAverageComputation"/> class.
        /// </summary>
        /// <param name="blueprint">Declaration coming from the builder.</param>
        internal SlidingAverageComputation(CollaborationIndicesBlueprint blueprint)
        {
            List<string> errors = blueprint.Validate(out List<ICollaborationIndicator> indicators);
            if (errors.Count > 0)
            {
                throw new CollaborationIndicesConfigurationException(errors);
            }

            this.pipeline = blueprint.Pipeline;
            this.Name = blueprint.Name;
            this.WindowDuration = blueprint.WindowDuration;
            this.ComputationInterval = blueprint.ComputationInterval;

            this.state = new IndicesBuildState(
                blueprint.Pipeline,
                blueprint.Name,
                new List<uint>(blueprint.ParticipantIds),
                blueprint.ComputationInterval,
                blueprint.EffectiveCalibration,
                blueprint.Store,
                indicators);

            // ---------- Clock and phase gating ----------
            // The gate stays closed until the longest window is filled with data of the phase.
            TimeSpan warmUp = indicators.Select(indicator => indicator.Options.WindowDuration ?? blueprint.WindowDuration).Max();

            this.Gate = new PhaseGateComponent(
                this.pipeline,
                new PhaseGateConfiguration
                {
                    WarmUpDuration = warmUp,
                    TickInterval = blueprint.ComputationInterval,
                    RequirePhase = blueprint.RequirePhase,
                    LogTransitions = blueprint.LogGateTransitions,
                },
                $"{this.Name}-Gate");

            this.state.Gate = this.Gate;

            IProducer<bool>? clock = blueprint.ClockStream
                ?? blueprint.ClockFactory?.Invoke(blueprint.ComputationInterval)
                ?? (blueprint.UseInternalClock ? Generators.Repeat(this.pipeline, true, blueprint.ComputationInterval) : null);
            clock?.PipeTo(this.Gate.ClockIn);

            // ---------- Indicators ----------
            // Each one creates its components and declares its clock and its outputs.
            foreach (ICollaborationIndicator indicator in indicators)
            {
                TimeSpan window = indicator.Options.WindowDuration ?? blueprint.WindowDuration;
                indicator.Build(new IndicatorBuildContext(this.state, indicator, window));
            }

            // ---------- One snapshot per tick ----------
            // The score, the graph and the export all read the indices of a tick from the same
            // message, so they cannot disagree on which value belongs to which tick.
            this.Snapshots = new IndexSnapshotAssembler(
                this.pipeline,
                new IndexSnapshotAssemblerConfiguration { MaximumPendingTicks = blueprint.MaximumPendingTicks },
                $"{this.Name}-Snapshot");

            this.Gate.Out.PipeTo(this.Snapshots.TickIn);
            this.ConnectOutputs();

            IProducer<IndexSnapshot> snapshots = this.Snapshots;

            // ---------- Fusion ----------
            if (blueprint.ComputeScore)
            {
                var scoreConfiguration = new CollaborationScoreConfiguration
                {
                    ParticipantIds = new List<uint>(blueprint.ParticipantIds),
                    Dimensions = blueprint.EffectiveDimensions,
                };

                blueprint.ConfigureScore?.Invoke(scoreConfiguration);

                this.CollaborationScore = new CollaborationScoreComponent(this.pipeline, scoreConfiguration, $"{this.Name}-CollaborationScore");
                snapshots.PipeTo(this.CollaborationScore.SnapshotIn);
                snapshots = this.CollaborationScore.SnapshotOut;
            }

            if (blueprint.GenerateGraph)
            {
                var graphConfiguration = new InteractionGraphConfiguration
                {
                    ParticipantIds = new List<uint>(blueprint.ParticipantIds),
                    NodeMetrics = NamesFor(this.Outputs.Individuals, IndexUsage.Graph),
                    EdgeMetrics = NamesFor(this.Outputs.Pairs, IndexUsage.Graph),
                    DirectedEdgeMetrics = NamesFor(this.Outputs.DirectedPairs, IndexUsage.Graph),
                    GroupMetrics = NamesFor(this.Outputs.Groups, IndexUsage.Graph).Concat(new[] { IndexNames.CollaborationScore }).ToList(),
                };

                blueprint.ConfigureGraph?.Invoke(graphConfiguration);

                this.Graph = new InteractionGraphComponent(this.pipeline, graphConfiguration, $"{this.Name}-Graph");
                snapshots.PipeTo(this.Graph.SnapshotIn);
            }

            if (blueprint.Export)
            {
                TextWriter writer = blueprint.ExportWriter ?? this.OpenExportFile(blueprint.ExportPath!);

                var exportConfiguration = new IndexExportConfiguration
                {
                    Writer = writer,
                    Columns = blueprint.ExportColumns ?? this.DefaultExportColumns(blueprint.ComputeScore ? blueprint.EffectiveDimensions : null),
                };

                blueprint.ConfigureExport?.Invoke(exportConfiguration);

                this.Export = new IndexExportComponent(this.pipeline, exportConfiguration, $"{this.Name}-Export");
                snapshots.PipeTo(this.Export.SnapshotIn);
            }

            this.SnapshotOut = snapshots;
        }

        /// <summary>Gets the name of the instance, prefix of every component and stream name.</summary>
        public string Name { get; }

        /// <summary>Gets the window of this instance, useful to label its outputs and its store.</summary>
        public TimeSpan WindowDuration { get; }

        /// <summary>Gets the publication period of the indices.</summary>
        public TimeSpan ComputationInterval { get; }

        /// <summary>Gets the participants of the session.</summary>
        public IReadOnlyList<uint> ParticipantIds => this.state.ParticipantIds;

        /// <summary>Gets the phase gate: connect the phase boundaries to its PhaseStartIn and PhaseEndIn.</summary>
        public PhaseGateComponent Gate { get; }

        /// <summary>Gets the clock of the windowed indices. Connect it unless the builder was given a clock.</summary>
        public Receiver<bool> ClockIn => this.Gate.ClockIn;

        /// <summary>Gets the indicators of the instance, each one after those it depends on.</summary>
        public IReadOnlyList<ICollaborationIndicator> Indicators => this.state.Indicators;

        /// <summary>Gets every output declared by the indicators, by level and by name.</summary>
        public IndicatorOutputs Outputs => this.state.Outputs;

        /// <summary>Gets the component gathering the indices of each tick.</summary>
        public IndexSnapshotAssembler Snapshots { get; }

        /// <summary>
        /// Gets every index of each tick in a single message, completed with the collaboration
        /// score when it is computed. The simplest stream to consume downstream.
        /// </summary>
        public IProducer<IndexSnapshot> SnapshotOut { get; }

        /// <summary>Gets the collaboration score, or null when the builder did not ask for it.</summary>
        public CollaborationScoreComponent? CollaborationScore { get; }

        /// <summary>Gets the interaction graph, or null when the builder did not ask for it.</summary>
        public InteractionGraphComponent? Graph { get; }

        /// <summary>Gets the CSV export, or null when the builder did not ask for it.</summary>
        public IndexExportComponent? Export { get; }

        /// <summary>Gets the complete state of the interaction at every tick, or null without a graph.</summary>
        public IProducer<InteractionGraph>? Out => this.Graph;

        /// <summary>
        /// Dimensions of the legacy collaboration model. Pass others to
        /// <see cref="CollaborationIndicesBuilder.WithCollaborationScore"/> to test another
        /// decomposition without touching any component.
        /// </summary>
        public static List<ScoreDimension> DefaultDimensions() => new List<ScoreDimension>
        {
            new ScoreDimension
            {
                Name = "Dominance",
                IndexNames = new List<string> { IndexNames.SpeechEquality, IndexNames.TaskEquality },
                ConditionalIndexNames = new List<string> { IndexNames.SpeechEquality, IndexNames.TaskEquality },
            },
            new ScoreDimension
            {
                Name = "JointAttention",
                IndexNames = new List<string> { IndexNames.JointVisualAttention, IndexNames.GazeOnPeers },
            },
            new ScoreDimension
            {
                Name = "CommunicationProcessManagement",
                IndexNames = new List<string> { IndexNames.VerbalParticipation, IndexNames.TurnTakingWithoutOverlap },
            },
            new ScoreDimension
            {
                Name = "SpatialBehaviour",
                IndexNames = new List<string> { IndexNames.Formation, IndexNames.Synchrony },
            },
            new ScoreDimension
            {
                Name = "Engagement",
                IndexNames = new List<string> { IndexNames.Movement, IndexNames.TaskParticipation },
            },
        };

        /// <summary>Whether an indicator is part of the instance.</summary>
        /// <param name="name">Name of the indicator.</param>
        /// <returns>True when it is.</returns>
        public bool Contains(string name) => this.state.Indicators.Any(indicator => indicator.Name == name);

        /// <summary>
        /// An indicator of the instance, from its catalogue entry:
        /// <c>Get(Indicators.Movement).Component</c>.
        /// </summary>
        /// <typeparam name="TIndicator">Class of the indicator.</typeparam>
        /// <typeparam name="TOptions">Options of the indicator.</typeparam>
        /// <param name="type">Entry of <see cref="Indicators"/>, or a kind you registered.</param>
        /// <returns>The indicator.</returns>
        /// <exception cref="InvalidOperationException">The indicator is not part of the instance.</exception>
        public TIndicator Get<TIndicator, TOptions>(IndicatorType<TIndicator, TOptions> type)
            where TIndicator : CollaborationIndicator<TOptions>
            where TOptions : IndicatorOptions, new()
            => this.state.Get<TIndicator>((type ?? throw new ArgumentNullException(nameof(type))).Name);

        /// <summary>An indicator of the instance, by class and, if several share it, by name.</summary>
        /// <typeparam name="TIndicator">Class of the indicator.</typeparam>
        /// <param name="name">Name of the indicator, needed when it was renamed or when several of that class exist.</param>
        /// <returns>The indicator.</returns>
        /// <exception cref="InvalidOperationException">The indicator is not part of the instance.</exception>
        public TIndicator Get<TIndicator>(string? name = null)
            where TIndicator : class, ICollaborationIndicator
            => this.state.Get<TIndicator>(name);

        /// <summary>Same as Get, but returns null instead of throwing when the indicator is absent.</summary>
        /// <typeparam name="TIndicator">Class of the indicator.</typeparam>
        /// <param name="name">Name of the indicator, needed when it was renamed or when several of that class exist.</param>
        /// <returns>The indicator, or null.</returns>
        public TIndicator? Find<TIndicator>(string? name = null)
            where TIndicator : class, ICollaborationIndicator
            => this.state.Find<TIndicator>(name);

        private static List<string> NamesFor<T>(IEnumerable<IndexOutput<T>> outputs, IndexUsage usage)
            => outputs.Where(output => output.Usage.HasFlag(usage)).Select(output => output.Name).ToList();

        /// <summary>
        /// Hands every declared output to the snapshot assembler, with the completion stream of
        /// the indicator it comes from when it has one.
        /// </summary>
        private void ConnectOutputs()
        {
            var pulseIds = new Dictionary<IndicatorPulse, int>();

            int? PulseIdOf(IndicatorPulse pulse)
            {
                IndicatorPulse root = pulse.Root;
                if (root.Processed == null)
                {
                    // Not paced by the clock of the indices: the snapshot carries its latest value.
                    return null;
                }

                if (!pulseIds.TryGetValue(root, out int id))
                {
                    id = this.Snapshots.AddPulse(root.Processed);
                    pulseIds[root] = id;
                }

                return id;
            }

            foreach (var output in this.Outputs.Groups)
            {
                this.Snapshots.AddGroup(output.Name, output.Stream, PulseIdOf(output.Pulse));
            }

            foreach (var output in this.Outputs.Individuals)
            {
                this.Snapshots.AddIndividual(output.Name, output.Stream, PulseIdOf(output.Pulse));
            }

            foreach (var output in this.Outputs.Pairs)
            {
                this.Snapshots.AddPair(output.Name, output.Stream, PulseIdOf(output.Pulse));
            }

            foreach (var output in this.Outputs.DirectedPairs)
            {
                this.Snapshots.AddDirectedPair(output.Name, output.Stream, PulseIdOf(output.Pulse));
            }

            foreach (var output in this.Outputs.ScoreInputs)
            {
                this.Snapshots.AddScoreInput(output.Name, output.Stream, PulseIdOf(output.Pulse));
            }

            foreach (var output in this.Outputs.Validities)
            {
                this.Snapshots.AddValidity(output.Name, output.Stream, PulseIdOf(output.Pulse));
            }
        }

        /// <summary>
        /// Columns of the export when none are given: every output declared for export, in the
        /// order of the indicators, then the dimension scores and the global score.
        /// </summary>
        private List<string> DefaultExportColumns(List<ScoreDimension>? dimensions)
        {
            var columns = new List<string>();

            foreach (ICollaborationIndicator indicator in this.state.Indicators)
            {
                string name = indicator.Name;

                columns.AddRange(this.Outputs.Groups
                    .Where(output => output.IndicatorName == name && output.Usage.HasFlag(IndexUsage.Export))
                    .Select(output => output.Name));

                foreach (var output in this.Outputs.Individuals.Where(o => o.IndicatorName == name && o.Usage.HasFlag(IndexUsage.Export)))
                {
                    columns.AddRange(this.ParticipantIds.Select(id => IndexExportComponent.ParticipantColumn(output.Name, id)));
                }

                foreach (var output in this.Outputs.Pairs.Where(o => o.IndicatorName == name && o.Usage.HasFlag(IndexUsage.Export)))
                {
                    columns.AddRange(Combinatorics.Pairs(this.ParticipantIds).Select(pair => IndexExportComponent.PairColumn(output.Name, pair)));
                }

                foreach (var output in this.Outputs.DirectedPairs.Where(o => o.IndicatorName == name && o.Usage.HasFlag(IndexUsage.Export)))
                {
                    foreach (uint from in this.ParticipantIds)
                    {
                        foreach (uint to in this.ParticipantIds)
                        {
                            if (from != to)
                            {
                                columns.Add(IndexExportComponent.DirectedPairColumn(output.Name, new DirectedParticipantPair(from, to)));
                            }
                        }
                    }
                }
            }

            if (dimensions != null)
            {
                // A dimension none of whose indices is computed would be an empty column.
                var scoreInputs = new HashSet<string>(this.Outputs.ScoreInputs.Select(output => output.Name));
                columns.AddRange(dimensions
                    .Where(dimension => dimension.IndexNames != null && dimension.IndexNames.Any(scoreInputs.Contains))
                    .Select(dimension => dimension.Name));
                columns.Add(IndexNames.CollaborationScore);
            }

            return columns.Distinct().ToList();
        }

        private TextWriter OpenExportFile(string path)
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var writer = new StreamWriter(path);

            // The file was opened here, so it is closed here, once the last row is written.
            this.pipeline.PipelineCompleted += (_, __) => writer.Dispose();
            return writer;
        }
    }
}
