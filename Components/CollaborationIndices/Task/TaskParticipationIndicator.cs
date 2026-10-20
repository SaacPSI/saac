// <copyright file="TaskParticipationIndicator.cs" company="SAAC">
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
    /// Options of <see cref="TaskParticipationIndicator"/>.
    /// </summary>
    public class TaskParticipationOptions : ComponentIndicatorOptions<TaskParticipationConfiguration>
    {
        /// <summary>Gets or sets the number of units removed from the score per inefficient action.</summary>
        public double InefficientActionPenalty { get; set; } = 2.0;

        /// <summary>Gets or sets a value indicating whether the score cannot go below zero after the penalty.</summary>
        public bool ClampToZero { get; set; } = true;
    }

    /// <summary>
    /// Task participation: number of productive actions of each participant over the window
    /// (<see cref="TaskParticipationComponent"/>). Connect the task events to
    /// <c>Component.EventsIn</c>.
    /// </summary>
    public class TaskParticipationIndicator : ComponentIndicator<TaskParticipationOptions, TaskParticipationComponent>, IDistributionSource
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.TaskParticipation;

        /// <inheritdoc/>
        public IProducer<Dictionary<uint, double>> Distribution => this.Component.RawIndividualOut;

        /// <inheritdoc/>
        public IProducer<bool>? DistributionUsable => null;

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            var configuration = new TaskParticipationConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                WindowDuration = context.WindowDuration,
                ComputationInterval = TimeSpan.Zero,
                GroupNormalizer = context.NormalizerFor(IndexNames.TaskParticipation),
                InefficientActionPenalty = this.Options.InefficientActionPenalty,
                ClampToZero = this.Options.ClampToZero,
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new TaskParticipationComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("TaskParticipation")));
            context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);

            // Only the levels the component is configured to post are declared.
            if (configuration.Levels.HasFlag(IndexLevel.Individual))
            {
                context.PublishIndividual(this.Name, this.Component.Out);
            }

            if (configuration.Levels.HasFlag(IndexLevel.Group))
            {
                context.PublishGroup(this.Name, this.Component.GroupOut);
                context.PublishScoreInput(this.Name, this.Component.GroupOut);
            }
        }
    }
}
