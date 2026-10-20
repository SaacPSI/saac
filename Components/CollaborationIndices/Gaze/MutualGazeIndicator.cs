// <copyright file="MutualGazeIndicator.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Options of <see cref="MutualGazeIndicator"/>.
    /// </summary>
    public class MutualGazeOptions : ComponentIndicatorOptions<EventCountIndexConfiguration>
    {
    }

    /// <summary>
    /// Mutual gaze: number of times two participants look at each other at the same time,
    /// over the window, for the group and for each pair (<see cref="EventCountIndexComponent"/>).
    /// Connect the mutual gaze events (<see cref="MutualGazeDetector"/>) to
    /// <c>Component.EventIn</c> or <c>Component.EventsIn</c>, with the two participants in
    /// ParticipantId and TargetId.
    /// </summary>
    public class MutualGazeIndicator : ComponentIndicator<MutualGazeOptions, EventCountIndexComponent>
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.MutualGaze;

        /// <inheritdoc/>
        public override IEnumerable<string> Validate(IndicatorBuildContext context) => this.RequireParticipants(context, 2);

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            // An event belongs to one pair and is counted once in the group: the group is the
            // sum of the events, each one carried by the first participant of its pair.
            var configuration = new EventCountIndexConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                WindowDuration = context.WindowDuration,
                ComputationInterval = TimeSpan.Zero,
                ComputeOnDataReception = false,
                Categories = new List<string> { IndexCategories.MutualGaze },
                Levels = IndexLevel.UndirectedPair | IndexLevel.Group,
                GroupAggregation = GroupAggregation.Sum,
                GroupNormalizer = context.NormalizerFor(IndexNames.MutualGaze),
                PairNormalizer = context.NormalizerFor(IndexNames.MutualGaze),
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new EventCountIndexComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("MutualGaze")));
            context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);

            // Only the levels the component is configured to post are declared.
            if (configuration.Levels.HasFlag(IndexLevel.Group))
            {
                context.PublishGroup(this.Name, this.Component.GroupOut);
            }

            if (configuration.Levels.HasFlag(IndexLevel.UndirectedPair))
            {
                context.PublishPair(this.Name, this.Component.PairOut, IndexUsage.Export);
            }
        }
    }
}
