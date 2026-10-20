// <copyright file="CollaborationIndicesBlueprint.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Psi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// How to create one indicator. A recipe rather than an instance, so that the same
    /// declaration can be built several times (one instance per window).
    /// </summary>
    internal sealed class IndicatorRecipe
    {
        public IndicatorRecipe(string typeName, Func<List<string>, ICollaborationIndicator?> create)
        {
            this.TypeName = typeName;
            this.Create = create;
        }

        /// <summary>Gets the name the recipe is written under in a configuration.</summary>
        public string TypeName { get; }

        /// <summary>Gets the factory: it returns null and adds to the errors when the indicator cannot be created.</summary>
        public Func<List<string>, ICollaborationIndicator?> Create { get; }

        /// <summary>Recipe of an indicator declared by its registered name and a bag of options.</summary>
        public static IndicatorRecipe FromName(string typeName, IDictionary<string, object>? options)
        {
            return new IndicatorRecipe(typeName, errors =>
            {
                if (!IndicatorRegistry.TryResolve(typeName, out IndicatorType? type) || type == null)
                {
                    errors.Add($"Unknown indicator type '{typeName}'. Known types: {string.Join(", ", IndicatorRegistry.Names)}.");
                    return null;
                }

                ICollaborationIndicator indicator = type.Create();
                string? error = ApplyOptions(typeName, indicator.Options, options);
                if (error != null)
                {
                    errors.Add(error);
                    return null;
                }

                return indicator;
            });
        }

        /// <summary>Name a class can be found again by, short enough to be written in a file.</summary>
        public static string ClassTypeName(Type indicatorClass) => $"{indicatorClass.FullName}, {indicatorClass.Assembly.GetName().Name}";

        /// <summary>Copies a bag of options onto a typed options object.</summary>
        /// <returns>Null on success, the problem otherwise.</returns>
        private static string? ApplyOptions(string typeName, IndicatorOptions target, IDictionary<string, object>? options)
        {
            if (options == null || options.Count == 0)
            {
                return null;
            }

            JsonSerializer serializer = JsonSerializer.Create(CollaborationIndicesConfiguration.JsonSettings);
            try
            {
                JObject json = JObject.FromObject(options, serializer);
                using (JsonReader reader = json.CreateReader())
                {
                    serializer.Populate(reader, target);
                }

                return null;
            }
            catch (JsonException exception)
            {
                IEnumerable<string> valid = target.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.CanWrite && property.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                    .Select(property => property.Name)
                    .OrderBy(name => name, StringComparer.Ordinal);

                return $"Indicator '{typeName}': invalid options. {exception.Message} Valid options: {string.Join(", ", valid)}.";
            }
        }
    }

    /// <summary>
    /// Everything declared on a <see cref="CollaborationIndicesBuilder"/>. The builder fills
    /// it, validates it, and <see cref="SlidingAverageComputation"/> is constructed from it.
    /// </summary>
    internal sealed class CollaborationIndicesBlueprint
    {
        public CollaborationIndicesBlueprint(Pipeline pipeline)
        {
            this.Pipeline = pipeline;
        }

        public Pipeline Pipeline { get; }

        public string Name { get; set; } = CollaborationIndicesBuilder.DefaultName;

        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        public TimeSpan WindowDuration { get; set; } = TimeSpan.FromSeconds(30);

        public TimeSpan ComputationInterval { get; set; } = TimeSpan.FromSeconds(1);

        public IndexCalibration? Calibration { get; set; }

        public bool RequirePhase { get; set; }

        public bool LogGateTransitions { get; set; }

        public bool UseInternalClock { get; set; }

        public IProducer<bool>? ClockStream { get; set; }

        public Func<TimeSpan, IProducer<bool>>? ClockFactory { get; set; }

        public IndexStore? Store { get; set; }

        public List<IndicatorRecipe> Recipes { get; set; } = new List<IndicatorRecipe>();

        public bool ComputeScore { get; set; }

        public List<ScoreDimension>? Dimensions { get; set; }

        public Action<CollaborationScoreConfiguration>? ConfigureScore { get; set; }

        public bool GenerateGraph { get; set; }

        public Action<InteractionGraphConfiguration>? ConfigureGraph { get; set; }

        public bool Export { get; set; }

        public TextWriter? ExportWriter { get; set; }

        public string? ExportPath { get; set; }

        public List<string>? ExportColumns { get; set; }

        public Action<IndexExportConfiguration>? ConfigureExport { get; set; }

        public int MaximumPendingTicks { get; set; } = new IndexSnapshotAssemblerConfiguration().MaximumPendingTicks;

        /// <summary>Gets the calibration to use: the declared one, or the one measured for the window.</summary>
        public IndexCalibration EffectiveCalibration => this.Calibration ?? IndexCalibration.ForWindow(this.WindowDuration);

        /// <summary>Gets the dimensions to use: the declared ones, or the default model.</summary>
        public List<ScoreDimension> EffectiveDimensions => this.Dimensions ?? SlidingAverageComputation.DefaultDimensions();

        public CollaborationIndicesBlueprint Clone()
        {
            return new CollaborationIndicesBlueprint(this.Pipeline)
            {
                Name = this.Name,
                ParticipantIds = new List<uint>(this.ParticipantIds),
                WindowDuration = this.WindowDuration,
                ComputationInterval = this.ComputationInterval,
                Calibration = this.Calibration,
                RequirePhase = this.RequirePhase,
                LogGateTransitions = this.LogGateTransitions,
                UseInternalClock = this.UseInternalClock,
                ClockStream = this.ClockStream,
                ClockFactory = this.ClockFactory,
                Store = this.Store,
                Recipes = new List<IndicatorRecipe>(this.Recipes),
                ComputeScore = this.ComputeScore,
                Dimensions = this.Dimensions,
                ConfigureScore = this.ConfigureScore,
                GenerateGraph = this.GenerateGraph,
                ConfigureGraph = this.ConfigureGraph,
                Export = this.Export,
                ExportWriter = this.ExportWriter,
                ExportPath = this.ExportPath,
                ExportColumns = this.ExportColumns == null ? null : new List<string>(this.ExportColumns),
                ConfigureExport = this.ConfigureExport,
                MaximumPendingTicks = this.MaximumPendingTicks,
            };
        }

        /// <summary>
        /// Checks the whole declaration and creates the indicators, in an order where each one
        /// comes after those it depends on. Nothing is added to the pipeline.
        /// </summary>
        /// <param name="indicators">The indicators, ordered. Meaningful only when no error is returned.</param>
        /// <returns>One message per problem.</returns>
        public List<string> Validate(out List<ICollaborationIndicator> indicators)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(this.Name))
            {
                errors.Add("The instance needs a name (WithName).");
            }

            if (this.WindowDuration <= TimeSpan.Zero)
            {
                errors.Add($"The sliding window must be strictly positive (WithWindow), not {this.WindowDuration}.");
            }

            if (this.ComputationInterval <= TimeSpan.Zero)
            {
                errors.Add($"The computation interval must be strictly positive (WithComputationInterval), not {this.ComputationInterval}.");
            }

            if (this.ParticipantIds.Count == 0)
            {
                errors.Add("No participant declared (WithParticipants).");
            }

            foreach (var duplicate in this.ParticipantIds.GroupBy(id => id).Where(group => group.Count() > 1))
            {
                errors.Add($"Participant {duplicate.Key} is declared {duplicate.Count()} times.");
            }

            int clocks = (this.UseInternalClock ? 1 : 0) + (this.ClockStream != null ? 1 : 0) + (this.ClockFactory != null ? 1 : 0);
            if (clocks > 1)
            {
                errors.Add("Several clocks are declared: keep one of WithInternalClock, WithClock and WithDataClock.");
            }

            if (this.Recipes.Count == 0)
            {
                errors.Add("No indicator declared: call AddIndicator at least once.");
            }

            var created = new List<ICollaborationIndicator>();
            foreach (IndicatorRecipe recipe in this.Recipes)
            {
                ICollaborationIndicator? indicator = recipe.Create(errors);
                if (indicator != null)
                {
                    created.Add(indicator);
                }
            }

            foreach (var duplicate in created.GroupBy(indicator => indicator.Name).Where(group => group.Count() > 1))
            {
                errors.Add(
                    $"Indicator '{duplicate.Key}' is declared {duplicate.Count()} times. " +
                    "If two of the same kind are intended, give each one its own name (Options.Name).");
            }

            foreach (ICollaborationIndicator indicator in created)
            {
                if (indicator.Options.WindowDuration.HasValue && indicator.Options.WindowDuration.Value <= TimeSpan.Zero)
                {
                    errors.Add($"Indicator '{indicator.Name}': its own window must be strictly positive, not {indicator.Options.WindowDuration.Value}.");
                }

                foreach (string dependency in indicator.Dependencies)
                {
                    if (!created.Any(other => other.Name == dependency))
                    {
                        errors.Add($"Indicator '{indicator.Name}' needs indicator '{dependency}', which is not declared. Add it with AddIndicator.");
                    }
                }
            }

            indicators = Order(created, errors);

            // The indicators validate themselves against the context: participants, window, each other.
            var state = new IndicesBuildState(this.Pipeline, this.Name, this.ParticipantIds, this.ComputationInterval, this.EffectiveCalibration, this.Store, created);
            foreach (ICollaborationIndicator indicator in created)
            {
                TimeSpan window = indicator.Options.WindowDuration ?? this.WindowDuration;
                errors.AddRange(indicator.Validate(new IndicatorBuildContext(state, indicator, window)));
            }

            if (this.ComputeScore)
            {
                List<ScoreDimension> dimensions = this.EffectiveDimensions;
                if (dimensions.Count == 0)
                {
                    errors.Add("The collaboration score needs at least one dimension.");
                }

                if (dimensions.Any(dimension => string.IsNullOrWhiteSpace(dimension.Name)))
                {
                    errors.Add("Every dimension of the collaboration score needs a name.");
                }

                foreach (var duplicate in dimensions.Where(d => !string.IsNullOrWhiteSpace(d.Name)).GroupBy(d => d.Name).Where(group => group.Count() > 1))
                {
                    errors.Add($"The score dimension '{duplicate.Key}' is declared {duplicate.Count()} times.");
                }
            }

            if (this.Export)
            {
                bool hasWriter = this.ExportWriter != null;
                bool hasPath = !string.IsNullOrWhiteSpace(this.ExportPath);
                if (!hasWriter && !hasPath)
                {
                    errors.Add("The CSV export needs a writer or a file path (WithCsvExport).");
                }
                else if (hasWriter && hasPath)
                {
                    errors.Add("The CSV export has both a writer and a file path: keep one.");
                }
            }

            return errors;
        }

        /// <summary>
        /// Orders the indicators so that each one comes after its dependencies, keeping the
        /// declaration order otherwise.
        /// </summary>
        private static List<ICollaborationIndicator> Order(List<ICollaborationIndicator> declared, List<string> errors)
        {
            var ordered = new List<ICollaborationIndicator>();
            var remaining = new List<ICollaborationIndicator>(declared);
            var known = new HashSet<string>(declared.Select(indicator => indicator.Name));

            while (remaining.Count > 0)
            {
                // A dependency that is not declared at all has already been reported: ignore it here.
                ICollaborationIndicator? next = remaining.FirstOrDefault(indicator =>
                    indicator.Dependencies.All(dependency => !known.Contains(dependency) || ordered.Any(placed => placed.Name == dependency)));

                if (next == null)
                {
                    errors.Add($"The indicators {string.Join(", ", remaining.Select(indicator => $"'{indicator.Name}'"))} depend on each other in a cycle.");
                    ordered.AddRange(remaining);
                    break;
                }

                ordered.Add(next);
                remaining.Remove(next);
            }

            return ordered;
        }
    }
}
