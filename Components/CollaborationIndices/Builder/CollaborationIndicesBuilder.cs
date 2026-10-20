// <copyright file="CollaborationIndicesBuilder.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Psi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAAC.PipelineServices;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Declares which collaboration indices are computed, and builds them.
    ///
    /// <code>
    /// var indices = new CollaborationIndicesBuilder(pipeline)
    ///     .WithParticipants(0, 1, 2)
    ///     .WithWindow(TimeSpan.FromSeconds(30))
    ///     .AddIndicator(Indicators.Movement)
    ///     .AddIndicator(Indicators.Synchrony, o => o.SubsetSize = 3)
    ///     .WithCollaborationScore()
    ///     .Build();
    /// </code>
    ///
    /// Nothing is added to the pipeline before <see cref="Build"/>, which first checks the
    /// whole declaration and reports every problem at once. The same declaration can be
    /// written as data, see <see cref="CollaborationIndicesConfiguration"/>.
    /// </summary>
    public sealed class CollaborationIndicesBuilder
    {
        /// <summary>Name of an instance when none is given.</summary>
        public const string DefaultName = "CollaborationIndices";

        private readonly CollaborationIndicesBlueprint blueprint;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollaborationIndicesBuilder"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline or subpipeline that will host the components.</param>
        public CollaborationIndicesBuilder(Pipeline pipeline)
        {
            this.blueprint = new CollaborationIndicesBlueprint(pipeline ?? throw new ArgumentNullException(nameof(pipeline)));
        }

        private CollaborationIndicesBuilder(CollaborationIndicesBlueprint blueprint)
        {
            this.blueprint = blueprint;
        }

        /// <summary>Creates a builder from a configuration object.</summary>
        /// <param name="pipeline">Pipeline or subpipeline that will host the components.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The builder, which can still be completed in code before Build.</returns>
        public static CollaborationIndicesBuilder FromConfiguration(Pipeline pipeline, CollaborationIndicesConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var builder = new CollaborationIndicesBuilder(pipeline)
                .WithName(configuration.Name)
                .WithParticipants(configuration.ParticipantIds ?? new List<uint>())
                .WithWindow(configuration.WindowDuration)
                .WithComputationInterval(configuration.ComputationInterval)
                .WithPhaseGate(configuration.RequirePhase, configuration.LogGateTransitions);

            if (configuration.Calibration != null)
            {
                builder.WithCalibration(configuration.Calibration);
            }

            if (configuration.UseInternalClock)
            {
                builder.WithInternalClock();
            }

            foreach (IndicatorConfiguration indicator in configuration.Indicators ?? new List<IndicatorConfiguration>())
            {
                builder.AddIndicator(indicator?.Type ?? string.Empty, indicator?.Options);
            }

            if (configuration.ComputeCollaborationScores)
            {
                builder.WithCollaborationScore(configuration.Dimensions);
            }

            if (configuration.GenerateGraph)
            {
                builder.WithInteractionGraph();
            }

            if (!string.IsNullOrWhiteSpace(configuration.ExportPath))
            {
                builder.WithCsvExport(configuration.ExportPath!, configuration.ExportColumns);
            }

            return builder;
        }

        /// <summary>Sets the name of the instance, prefix of every component and stream name.</summary>
        /// <param name="name">The name.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithName(string name)
        {
            this.blueprint.Name = name;
            return this;
        }

        /// <summary>Sets the participants of the session. Any number, contiguous or not.</summary>
        /// <param name="participantIds">Identifiers of the participants.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithParticipants(params uint[] participantIds)
            => this.WithParticipants((IEnumerable<uint>)participantIds);

        /// <summary>Sets the participants of the session. Any number, contiguous or not.</summary>
        /// <param name="participantIds">Identifiers of the participants.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithParticipants(IEnumerable<uint> participantIds)
        {
            this.blueprint.ParticipantIds = new List<uint>(participantIds ?? Enumerable.Empty<uint>());
            return this;
        }

        /// <summary>
        /// Sets the sliding window: every index describes the last <paramref name="window"/> of
        /// data. An indicator can still have its own through its options.
        /// </summary>
        /// <param name="window">Duration of the window.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithWindow(TimeSpan window)
        {
            this.blueprint.WindowDuration = window;
            return this;
        }

        /// <summary>Sets the publication period of the indices.</summary>
        /// <param name="interval">Period of the clock.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithComputationInterval(TimeSpan interval)
        {
            this.blueprint.ComputationInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the calibration of the normalized indices. Without it, the calibration measured
        /// for the window is used (<see cref="IndexCalibration.ForWindow"/>).
        /// </summary>
        /// <param name="calibration">The calibration.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithCalibration(IndexCalibration calibration)
        {
            this.blueprint.Calibration = calibration;
            return this;
        }

        /// <summary>
        /// Configures the phase gate. By default the indices run as soon as the clock does,
        /// after one window of warm-up.
        /// </summary>
        /// <param name="requirePhase">
        /// If true, the indices only run inside a phase: connect the phase boundaries to
        /// Gate.PhaseStartIn and Gate.PhaseEndIn, otherwise nothing is ever published.
        /// </param>
        /// <param name="logTransitions">Traces the gate transitions on the console.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithPhaseGate(bool requirePhase, bool logTransitions = false)
        {
            this.blueprint.RequirePhase = requirePhase;
            this.blueprint.LogGateTransitions = logTransitions;
            return this;
        }

        /// <summary>
        /// Makes the instance create its own clock, from the clock of the pipeline. Fine for a
        /// live pipeline; for a replay prefer <see cref="WithDataClock{T}"/>.
        /// </summary>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithInternalClock()
        {
            this.blueprint.UseInternalClock = true;
            return this;
        }

        /// <summary>
        /// Drives the indices from a tick stream. The originating time of each tick is the end
        /// of the window that is computed. Without any clock declared here, connect one to
        /// ClockIn of the built instance.
        /// </summary>
        /// <param name="ticks">The tick stream; the values are ignored.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithClock(IProducer<bool> ticks)
        {
            this.blueprint.ClockStream = ticks ?? throw new ArgumentNullException(nameof(ticks));
            return this;
        }

        /// <summary>
        /// Drives the indices from the data itself: one tick per computation interval of data
        /// time, taken from a dense stream (head positions). The ticks are on the data timeline
        /// by construction, at any replay speed. See <see cref="DataClock"/>.
        /// </summary>
        /// <typeparam name="T">Type of the source, whose values are ignored.</typeparam>
        /// <param name="source">Dense stream used as the time reference.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithDataClock<T>(IProducer<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            this.blueprint.ClockFactory = interval => DataClock.FromStream(source, interval);
            return this;
        }

        /// <summary>
        /// Stores the streams the components have always stored, through a dataset pipeline.
        /// Without it nothing is stored, and no dataset pipeline is needed.
        /// </summary>
        /// <param name="server">Dataset pipeline owning the connectors and the stores.</param>
        /// <param name="sessionName">Session receiving the streams.</param>
        /// <param name="storeName">Store receiving the streams.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithStore(DatasetPipeline server, string sessionName = IndexStore.DefaultSessionName, string storeName = IndexStore.DefaultStoreName)
        {
            this.blueprint.Store = new IndexStore(server, sessionName, storeName);
            return this;
        }

        /// <summary>
        /// Declares an indicator of the catalogue:
        /// <c>AddIndicator(Indicators.Synchrony, o => o.SubsetSize = 3)</c>.
        /// </summary>
        /// <typeparam name="TIndicator">Class of the indicator.</typeparam>
        /// <typeparam name="TOptions">Options of the indicator.</typeparam>
        /// <param name="type">Entry of <see cref="Indicators"/>, or a kind you registered.</param>
        /// <param name="configure">Sets the options that differ from the defaults.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder AddIndicator<TIndicator, TOptions>(IndicatorType<TIndicator, TOptions> type, Action<TOptions>? configure = null)
            where TIndicator : CollaborationIndicator<TOptions>
            where TOptions : IndicatorOptions, new()
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            this.blueprint.Recipes.Add(new IndicatorRecipe(type.Name, _ =>
            {
                TIndicator indicator = type.CreateTyped();
                configure?.Invoke(indicator.Options);
                return indicator;
            }));

            return this;
        }

        /// <summary>
        /// Declares an indicator by its class, for one that is not in a catalogue:
        /// <c>AddIndicator&lt;MyIndicator&gt;(i => i.Options.Threshold = 0.5)</c>.
        /// </summary>
        /// <typeparam name="TIndicator">Class of the indicator.</typeparam>
        /// <param name="configure">Sets the options that differ from the defaults.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder AddIndicator<TIndicator>(Action<TIndicator>? configure = null)
            where TIndicator : ICollaborationIndicator, new()
        {
            this.blueprint.Recipes.Add(new IndicatorRecipe(IndicatorRecipe.ClassTypeName(typeof(TIndicator)), _ =>
            {
                var indicator = new TIndicator();
                configure?.Invoke(indicator);
                return indicator;
            }));

            return this;
        }

        /// <summary>
        /// Declares an indicator by name, the way a configuration file does:
        /// <c>AddIndicator("Synchrony", new Dictionary&lt;string, object&gt; { { "SubsetSize", 3 } })</c>.
        /// An unknown name or option is reported by Build.
        /// </summary>
        /// <param name="typeName">A name of <see cref="Indicators"/> or of <see cref="IndicatorRegistry"/>.</param>
        /// <param name="options">Options by property name. Null keeps the defaults.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder AddIndicator(string typeName, IDictionary<string, object>? options = null)
        {
            this.blueprint.Recipes.Add(IndicatorRecipe.FromName(typeName ?? string.Empty, options == null ? null : new Dictionary<string, object>(options)));
            return this;
        }

        /// <summary>
        /// Adds the collaboration score: the normalized group indices merged into dimension
        /// scores and a global score.
        /// </summary>
        /// <param name="dimensions">Dimensions of the score. Null uses the default model.</param>
        /// <param name="configure">Further settings of the score component.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithCollaborationScore(IEnumerable<ScoreDimension>? dimensions = null, Action<CollaborationScoreConfiguration>? configure = null)
        {
            this.blueprint.ComputeScore = true;
            this.blueprint.Dimensions = dimensions?.ToList();
            this.blueprint.ConfigureScore = configure;
            return this;
        }

        /// <summary>Adds the interaction graph: one message per tick with every node and edge metric.</summary>
        /// <param name="configure">Further settings of the graph component.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithInteractionGraph(Action<InteractionGraphConfiguration>? configure = null)
        {
            this.blueprint.GenerateGraph = true;
            this.blueprint.ConfigureGraph = configure;
            return this;
        }

        /// <summary>Adds the CSV export: one row per tick. The writer is neither flushed nor closed by the library.</summary>
        /// <param name="writer">Destination of the rows.</param>
        /// <param name="columns">Columns, in order. Null exports every declared output.</param>
        /// <param name="configure">Further settings of the export component.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithCsvExport(TextWriter writer, IEnumerable<string>? columns = null, Action<IndexExportConfiguration>? configure = null)
        {
            this.blueprint.Export = true;
            this.blueprint.ExportWriter = writer;
            this.blueprint.ExportPath = null;
            this.blueprint.ExportColumns = columns?.ToList();
            this.blueprint.ConfigureExport = configure;
            return this;
        }

        /// <summary>Adds the CSV export into a file, created by Build and closed when the pipeline completes.</summary>
        /// <param name="path">Path of the file.</param>
        /// <param name="columns">Columns, in order. Null exports every declared output.</param>
        /// <param name="configure">Further settings of the export component.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithCsvExport(string path, IEnumerable<string>? columns = null, Action<IndexExportConfiguration>? configure = null)
        {
            this.blueprint.Export = true;
            this.blueprint.ExportWriter = null;
            this.blueprint.ExportPath = path;
            this.blueprint.ExportColumns = columns?.ToList();
            this.blueprint.ConfigureExport = configure;
            return this;
        }

        /// <summary>
        /// Sets how many ticks may wait for an announced index before the score, the graph and
        /// the export go on without it. See <see cref="IndexSnapshotAssemblerConfiguration"/>.
        /// </summary>
        /// <param name="maximumPendingTicks">Number of ticks; zero waits for ever.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder WithMaximumPendingTicks(int maximumPendingTicks)
        {
            this.blueprint.MaximumPendingTicks = maximumPendingTicks;
            return this;
        }

        /// <summary>
        /// Checks the declaration without building anything.
        /// </summary>
        /// <returns>One message per problem, empty when Build would succeed.</returns>
        public IReadOnlyList<string> Validate() => this.blueprint.Validate(out _);

        /// <summary>
        /// Checks the declaration, then creates the components in the pipeline and connects them.
        /// </summary>
        /// <returns>The instance: connect the source streams to the inputs of its indicators.</returns>
        /// <exception cref="CollaborationIndicesConfigurationException">The declaration is invalid; every problem is listed.</exception>
        public SlidingAverageComputation Build() => new SlidingAverageComputation(this.blueprint.Clone());

        /// <summary>
        /// Builds one instance per window, side by side, on shared clocks. The name of each
        /// instance is the name of the builder followed by the window in seconds.
        /// </summary>
        /// <param name="windows">Window durations.</param>
        /// <returns>The set of instances.</returns>
        public SlidingAverageComputationSet BuildSet(params TimeSpan[] windows)
            => this.BuildSet((windows ?? new TimeSpan[0]).Select(window => new KeyValuePair<TimeSpan, TextWriter?>(window, null)));

        /// <summary>
        /// Builds one instance per window, side by side, on shared clocks, each one with its
        /// own CSV export. The name of each instance is the name of the builder followed by
        /// the window in seconds.
        /// </summary>
        /// <param name="windows">Window durations and the writer of each one; a null writer disables the export of that window.</param>
        /// <param name="calibrationFor">
        /// Calibration of each window. By default the one measured for the window: a single
        /// calibration cannot fit several windows, so WithCalibration is ignored here.
        /// </param>
        /// <returns>The set of instances.</returns>
        public SlidingAverageComputationSet BuildSet(IEnumerable<KeyValuePair<TimeSpan, TextWriter?>> windows, Func<TimeSpan, IndexCalibration>? calibrationFor = null)
        {
            List<KeyValuePair<TimeSpan, TextWriter?>> declared = (windows ?? Enumerable.Empty<KeyValuePair<TimeSpan, TextWriter?>>()).ToList();

            var errors = new List<string>();
            if (declared.Count == 0)
            {
                errors.Add("No window declared for the set.");
            }

            foreach (var duplicate in declared.GroupBy(window => window.Key).Where(group => group.Count() > 1))
            {
                errors.Add($"The window {duplicate.Key} is declared {duplicate.Count()} times.");
            }

            foreach (var collision in declared.Select(window => window.Key).Distinct().GroupBy(window => (int)window.TotalSeconds).Where(group => group.Count() > 1))
            {
                errors.Add($"The windows {string.Join(" and ", collision)} would both be named '{this.blueprint.Name}_{collision.Key}s': instance names are built from whole seconds.");
            }

            if (errors.Count > 0)
            {
                throw new CollaborationIndicesConfigurationException(errors);
            }

            // One generator for the whole set: two generators of the same period produce two
            // slightly different tick sequences, and the rows of the windows could not be aligned.
            IProducer<bool>? sharedClock = null;
            if (this.blueprint.UseInternalClock && this.blueprint.ComputationInterval > TimeSpan.Zero)
            {
                sharedClock = Generators.Repeat(this.blueprint.Pipeline, true, this.blueprint.ComputationInterval);
            }

            var instances = new List<KeyValuePair<TimeSpan, SlidingAverageComputation>>();
            foreach (var window in declared)
            {
                CollaborationIndicesBlueprint instance = this.blueprint.Clone();
                instance.WindowDuration = window.Key;
                instance.Name = $"{this.blueprint.Name}_{(int)window.Key.TotalSeconds}s";
                instance.Calibration = calibrationFor != null ? calibrationFor(window.Key) : IndexCalibration.ForWindow(window.Key);
                instance.Export = window.Value != null;
                instance.ExportWriter = window.Value;
                instance.ExportPath = null;

                if (sharedClock != null)
                {
                    instance.UseInternalClock = false;
                    instance.ClockStream = sharedClock;
                }

                instances.Add(new KeyValuePair<TimeSpan, SlidingAverageComputation>(window.Key, new SlidingAverageComputation(instance)));
            }

            return new SlidingAverageComputationSet(instances);
        }

        /// <summary>Independent copy of the builder, to derive a variant of a declaration.</summary>
        /// <returns>The copy.</returns>
        public CollaborationIndicesBuilder Clone() => new CollaborationIndicesBuilder(this.blueprint.Clone());

        /// <summary>The declaration, for the constructors kept for compatibility.</summary>
        /// <returns>A copy of the declaration.</returns>
        internal CollaborationIndicesBlueprint CreateBlueprint() => this.blueprint.Clone();

        /// <summary>
        /// The declaration as a configuration object, for instance to save next to the results
        /// what was computed. Clocks, stores, writers and the Configure hooks of the options
        /// are code, not data, and are not part of it.
        /// </summary>
        /// <returns>The configuration.</returns>
        public CollaborationIndicesConfiguration ToConfiguration()
        {
            var configuration = new CollaborationIndicesConfiguration
            {
                Name = this.blueprint.Name,
                ParticipantIds = new List<uint>(this.blueprint.ParticipantIds),
                WindowDuration = this.blueprint.WindowDuration,
                ComputationInterval = this.blueprint.ComputationInterval,
                UseInternalClock = this.blueprint.UseInternalClock,
                RequirePhase = this.blueprint.RequirePhase,
                LogGateTransitions = this.blueprint.LogGateTransitions,
                Calibration = this.blueprint.Calibration,
                ComputeCollaborationScores = this.blueprint.ComputeScore,
                Dimensions = this.blueprint.Dimensions,
                GenerateGraph = this.blueprint.GenerateGraph,
                ExportPath = this.blueprint.ExportPath,
                ExportColumns = this.blueprint.ExportColumns,
            };

            JsonSerializer serializer = JsonSerializer.Create(CollaborationIndicesConfiguration.JsonSettings);
            var errors = new List<string>();
            foreach (IndicatorRecipe recipe in this.blueprint.Recipes)
            {
                ICollaborationIndicator? indicator = recipe.Create(errors);
                if (indicator == null)
                {
                    continue;
                }

                configuration.Indicators.Add(new IndicatorConfiguration
                {
                    Type = recipe.TypeName,
                    Options = JObject.FromObject(indicator.Options, serializer).ToObject<Dictionary<string, object>>(),
                });
            }

            if (errors.Count > 0)
            {
                throw new CollaborationIndicesConfigurationException(errors);
            }

            return configuration;
        }
    }
}
