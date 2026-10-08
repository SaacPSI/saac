// <copyright file="PhysicalActivityIndicator.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Options of <see cref="PhysicalActivityIndicator"/>.
    /// </summary>
    public class PhysicalActivityOptions : ComponentIndicatorOptions<PhysicalActivityLevelConfiguration>
    {
        /// <summary>Gets or sets the body parts whose movement is measured.</summary>
        public List<string> BodyParts { get; set; } = new List<string> { BodyPartNames.Head, BodyPartNames.LeftHand, BodyPartNames.RightHand };

        /// <summary>Gets or sets the weight of each body part. Null keeps the weights of the component (head 0.4, hands 0.3).</summary>
        public Dictionary<string, double>? BodyPartWeights { get; set; } = null;

        /// <summary>Gets or sets shorter or longer windows computed alongside the main one, each on its own stream.</summary>
        public List<TimeSpan> AdditionalWindows { get; set; } = new List<TimeSpan>();

        /// <summary>Gets or sets the unit of the movement.</summary>
        public MovementUnit Unit { get; set; } = MovementUnit.DisplacementPerSecond;
    }

    /// <summary>
    /// Movement: physical activity level of each participant and of the group
    /// (<see cref="PhysicalActivityLevelComponent"/>). Connect the positions to
    /// <c>Component.GetPositionInput(participantId, bodyPart)</c>.
    /// </summary>
    public class PhysicalActivityIndicator : ComponentIndicator<PhysicalActivityOptions, PhysicalActivityLevelComponent>, IDistributionSource
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.Movement;

        /// <inheritdoc/>
        public IProducer<Dictionary<uint, double>> Distribution => this.Component.Out;

        /// <inheritdoc/>
        public IProducer<bool>? DistributionUsable => null;

        /// <inheritdoc/>
        public override IEnumerable<string> Validate(IndicatorBuildContext context)
        {
            if (this.Options.BodyParts == null || this.Options.BodyParts.Count == 0)
            {
                yield return $"Indicator '{this.Name}' needs at least one body part (Options.BodyParts).";
            }

            if (this.Options.AdditionalWindows != null && this.Options.AdditionalWindows.Any(window => window <= TimeSpan.Zero))
            {
                yield return $"Indicator '{this.Name}': every additional window must be strictly positive.";
            }
        }

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            var configuration = new PhysicalActivityLevelConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                BodyParts = new List<string>(this.Options.BodyParts),
                WindowDuration = context.WindowDuration,
                ComputationInterval = TimeSpan.Zero,
                AdditionalWindows = new List<TimeSpan>(this.Options.AdditionalWindows ?? new List<TimeSpan>()),
                ComputeOnDataReception = false,
                Unit = this.Options.Unit,
            };

            if (this.Options.BodyPartWeights != null)
            {
                configuration.BodyPartWeights = new Dictionary<string, double>(this.Options.BodyPartWeights);
            }

            this.Options.Configure?.Invoke(configuration);

            this.Component = new PhysicalActivityLevelComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("ActivityLevel")), context.Store);

            context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);
            context.PublishIndividual(this.Name, this.Component.Out);
            context.PublishGroup(this.Name, this.Component.GroupActivityLevelOut);
            context.PublishScoreInput(this.Name, this.Component.GroupActivityLevelOut);
        }
    }
}
