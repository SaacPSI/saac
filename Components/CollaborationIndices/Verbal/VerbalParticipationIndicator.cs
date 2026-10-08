// <copyright file="VerbalParticipationIndicator.cs" company="SAAC">
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
    /// Options of <see cref="VerbalParticipationIndicator"/>.
    /// </summary>
    public class VerbalParticipationOptions : ComponentIndicatorOptions<VerbalParticipationConfiguration>
    {
        /// <summary>
        /// Gets or sets a value indicating whether the index is the share of the window spent
        /// speaking (0 to 1) rather than a duration in seconds.
        /// </summary>
        public bool AsRatioOfWindow { get; set; } = true;

        /// <summary>
        /// Gets or sets the group participation below which the group is too silent for an
        /// equality index to be meaningful.
        /// </summary>
        public double MinimumRatioForEquality { get; set; } = 0.15;
    }

    /// <summary>
    /// Verbal participation: time spent speaking by each participant and by the group
    /// (<see cref="VerbalParticipationComponent"/>). Connect the speech intervals to
    /// <c>Component.GetIntervalInput(participantId)</c> or <c>Component.IntervalsIn</c>.
    /// </summary>
    public class VerbalParticipationIndicator : ComponentIndicator<VerbalParticipationOptions, VerbalParticipationComponent>, IDistributionSource
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.VerbalParticipation;

        /// <inheritdoc/>
        public IProducer<Dictionary<uint, double>> Distribution => this.Component.SpeakingTimesOut;

        /// <inheritdoc/>
        public IProducer<bool>? DistributionUsable => this.Component.EqualityUsableOut;

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            var configuration = new VerbalParticipationConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                WindowDuration = context.WindowDuration,
                ComputationInterval = TimeSpan.Zero,
                ComputeOnDataReception = false,
                AsRatioOfWindow = this.Options.AsRatioOfWindow,
                MinimumRatioForEquality = this.Options.MinimumRatioForEquality,
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new VerbalParticipationComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("VerbalParticipation")), context.Store);

            context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);
            context.PublishIndividual(this.Name, this.Component.Out);
            context.PublishGroup(this.Name, this.Component.GroupOut);
            context.PublishScoreInput(this.Name, this.Component.GroupOut);
        }
    }
}
