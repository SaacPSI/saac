// <copyright file="SimultaneousSpeechIndicators.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Options of <see cref="SilenceIndicator"/> and <see cref="CrossTalkIndicator"/>.
    /// </summary>
    public class SimultaneousSpeechOptions : ComponentIndicatorOptions<SimultaneousSpeechConfiguration>
    {
        /// <summary>
        /// Gets or sets a value indicating whether the index is the share of the window
        /// (0 to 1) rather than a duration in seconds.
        /// </summary>
        public bool AsRatioOfWindow { get; set; } = false;
    }

    /// <summary>
    /// Silence: cumulated time, over the window, during which no participant speaks
    /// (<see cref="SimultaneousSpeechComponent"/>). Connect the speech intervals to
    /// <c>Component.GetIntervalInput(participantId)</c> or <c>Component.IntervalsIn</c>, as
    /// for the verbal participation.
    /// </summary>
    public class SilenceIndicator : ComponentIndicator<SimultaneousSpeechOptions, SimultaneousSpeechComponent>
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.Silence;

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            var configuration = new SimultaneousSpeechConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                WindowDuration = context.WindowDuration,
                ComputationInterval = TimeSpan.Zero,
                ComputeOnDataReception = false,
                MinimumSpeakers = 0,
                MaximumSpeakers = 0,
                AsRatioOfWindow = this.Options.AsRatioOfWindow,
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new SimultaneousSpeechComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("Silence")));
            context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);
            context.PublishGroup(this.Name, this.Component.Out);
        }
    }

    /// <summary>
    /// Cross-talk: cumulated time, over the window, during which at least two participants
    /// speak at the same time (<see cref="SimultaneousSpeechComponent"/>), for the group and
    /// for each pair. Only the time spoken together counts, not the whole utterances.
    /// Connect the speech intervals to <c>Component.GetIntervalInput(participantId)</c> or
    /// <c>Component.IntervalsIn</c>, as for the verbal participation.
    /// </summary>
    public class CrossTalkIndicator : ComponentIndicator<SimultaneousSpeechOptions, SimultaneousSpeechComponent>
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.CrossTalk;

        /// <inheritdoc/>
        public override IEnumerable<string> Validate(IndicatorBuildContext context) => this.RequireParticipants(context, 2);

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            var configuration = new SimultaneousSpeechConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                WindowDuration = context.WindowDuration,
                ComputationInterval = TimeSpan.Zero,
                ComputeOnDataReception = false,
                MinimumSpeakers = 2,
                MaximumSpeakers = int.MaxValue,
                AsRatioOfWindow = this.Options.AsRatioOfWindow,
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new SimultaneousSpeechComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("CrossTalk")));
            context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);
            context.PublishGroup(this.Name, this.Component.Out);
            context.PublishPair(this.Name, this.Component.PairOut, IndexUsage.Export);
        }
    }
}
