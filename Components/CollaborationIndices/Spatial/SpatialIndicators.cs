// <copyright file="SpatialIndicators.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Options of <see cref="TimeInAreaIndicator"/>.
    /// </summary>
    public class TimeInAreaOptions : ComponentIndicatorOptions<TimeInAreaConfiguration>
    {
        /// <summary>Gets or sets the areas of the environment that are tracked.</summary>
        public List<string> Areas { get; set; } = new List<string>();

        /// <summary>Gets or sets the areas whose group occupancy is aggregated in one value (planning area).</summary>
        public List<string> PlanningAreas { get; set; } = new List<string>();

        /// <summary>Gets or sets a value indicating whether the time is the share of the window (0 to 1) rather than seconds.</summary>
        public bool AsRatioOfWindow { get; set; } = false;
    }

    /// <summary>
    /// Time in area: time spent by each participant in each area over the window
    /// (<see cref="TimeInAreaComponent"/>). Connect the presence intervals to
    /// <c>Component.GetIntervalInput(participantId)</c> or <c>Component.IntervalsIn</c>.
    /// Each area is an index of its own, named "TimeInArea_&lt;area&gt;".
    /// </summary>
    public class TimeInAreaIndicator : ComponentIndicator<TimeInAreaOptions, TimeInAreaComponent>
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.TimeInArea;

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            var configuration = new TimeInAreaConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                Areas = new List<string>(this.Options.Areas ?? new List<string>()),
                GroupAggregatedAreas = new List<string>(this.Options.PlanningAreas ?? new List<string>()),
                WindowDuration = context.WindowDuration,
                ComputationInterval = TimeSpan.Zero,
                AsRatioOfWindow = this.Options.AsRatioOfWindow,
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new TimeInAreaComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("TimeInArea")));
            context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);

            foreach (string area in configuration.Areas)
            {
                string key = area;
                context.PublishIndividual(
                    $"{this.Name}_{key}",
                    this.Component.Out.Select(times =>
                    {
                        var perParticipant = new Dictionary<uint, double>();
                        foreach (var entry in times)
                        {
                            if (entry.Value.TryGetValue(key, out double value))
                            {
                                perParticipant[entry.Key] = value;
                            }
                        }

                        return perParticipant;
                    }),
                    IndexUsage.Export);
            }

            // The group time is only posted when some areas are aggregated.
            if (configuration.GroupAggregatedAreas != null && configuration.GroupAggregatedAreas.Count > 0)
            {
                context.PublishGroup(this.Name, this.Component.GroupAreaTimeOut);
            }
        }
    }

    /// <summary>
    /// Options of <see cref="FFormationIndicator"/>.
    /// </summary>
    public class FFormationOptions : ComponentIndicatorOptions<FFormationConfiguration>
    {
        /// <summary>
        /// Gets or sets the size of the sub-groups that also get a count (3 for the formation
        /// of a triad). Zero disables them; they are skipped anyway when the group is smaller.
        /// </summary>
        public int SubsetSize { get; set; } = 3;
    }

    /// <summary>
    /// Formation: number of shared spatial formations (F-formations) over the window
    /// (<see cref="FFormationComponent"/>). Connect the formation events to
    /// <c>Component.EventIn</c> or <c>Component.EventsIn</c>, with the two participants in
    /// ParticipantId and TargetId.
    /// </summary>
    public class FFormationIndicator : ComponentIndicator<FFormationOptions, FFormationComponent>
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.Formation;

        /// <inheritdoc/>
        public override IEnumerable<string> Validate(IndicatorBuildContext context)
        {
            foreach (string error in this.RequireParticipants(context, 2))
            {
                yield return error;
            }

            if (this.Options.SubsetSize != 0 && this.Options.SubsetSize < 3)
            {
                yield return $"Indicator '{this.Name}': SubsetSize must be 0 (no sub-group) or at least 3, not {this.Options.SubsetSize}.";
            }
        }

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            bool withSubsets = this.Options.SubsetSize >= 3 && context.ParticipantIds.Count >= this.Options.SubsetSize;

            var configuration = new FFormationConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                WindowDuration = context.WindowDuration,
                ComputationInterval = TimeSpan.Zero,
                GroupNormalizer = context.NormalizerFor(IndexNames.Formation),
                PairNormalizer = context.NormalizerFor(IndexNames.Formation),
                SubsetSize = withSubsets ? this.Options.SubsetSize : 0,
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new FFormationComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("FFormation")));
            context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);

            // Only the levels the component is configured to post are declared.
            if (configuration.Levels.HasFlag(IndexLevel.Group))
            {
                context.PublishGroup(this.Name, this.Component.GroupOut);
                context.PublishScoreInput(this.Name, this.Component.GroupOut);
            }

            if (configuration.Levels.HasFlag(IndexLevel.UndirectedPair))
            {
                context.PublishPair(this.Name, this.Component.PairOut, IndexUsage.Export);
            }
        }
    }

    /// <summary>
    /// Options of <see cref="ProximityIndicator"/>.
    /// </summary>
    public class ProximityOptions : ComponentIndicatorOptions<ProximityConfiguration>
    {
        /// <summary>Gets or sets the distance beyond which the proximity score is zero, in the unit of the input.</summary>
        public double MaximumDistance { get; set; } = 3.0;

        /// <summary>Gets or sets a value indicating whether a proximity score in [0, 1] is published instead of the raw distance.</summary>
        public bool AsProximityScore { get; set; } = true;
    }

    /// <summary>
    /// Proximity: interpersonal distance of each pair, sampled on the clock of the indices
    /// (<see cref="ProximityComponent"/>). Connect the distances to
    /// <c>Component.GetDistanceInput(participantA, participantB)</c>.
    /// </summary>
    public class ProximityIndicator : ComponentIndicator<ProximityOptions, ProximityComponent>
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.Proximity;

        /// <inheritdoc/>
        public override IEnumerable<string> Validate(IndicatorBuildContext context) => this.RequireParticipants(context, 2);

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            var configuration = new ProximityConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                ComputationInterval = TimeSpan.Zero,
                MaximumDistance = this.Options.MaximumDistance,
                AsProximityScore = this.Options.AsProximityScore,
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new ProximityComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("Proximity")));
            context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);
            context.PublishPair(this.Name, this.Component.Out);
        }
    }
}
