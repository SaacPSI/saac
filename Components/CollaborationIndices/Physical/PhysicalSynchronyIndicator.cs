// <copyright file="PhysicalSynchronyIndicator.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Options of <see cref="PhysicalSynchronyIndicator"/>.
    /// </summary>
    public class PhysicalSynchronyOptions : ComponentIndicatorOptions<PhysicalSynchronyConfiguration>
    {
        /// <summary>Gets or sets the body parts whose movement is compared.</summary>
        public List<string> BodyParts { get; set; } = new List<string> { BodyPartNames.Head };

        /// <summary>
        /// Gets or sets the size of the sub-groups that also get a score (3 for triads).
        /// Zero disables them; they are skipped anyway when the group is smaller.
        /// </summary>
        public int SubsetSize { get; set; } = 3;

        /// <summary>Gets or sets how the correlation is mapped before being published.</summary>
        public SynchronyNormalization Normalization { get; set; } = SynchronyNormalization.ZeroToOne;

        /// <summary>
        /// Gets or sets a value indicating whether a grid point is used only when every
        /// participant is tracked on it.
        /// </summary>
        public bool RequireAllParticipants { get; set; } = true;
    }

    /// <summary>
    /// Synchrony: correlation of the movements of each pair, and of the group
    /// (<see cref="PhysicalSynchronyComponent"/>). Connect the positions to
    /// <c>Component.GetPositionInput(participantId, bodyPart)</c>.
    /// </summary>
    public class PhysicalSynchronyIndicator : ComponentIndicator<PhysicalSynchronyOptions, PhysicalSynchronyComponent>
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.Synchrony;

        /// <inheritdoc/>
        public override IEnumerable<string> Validate(IndicatorBuildContext context)
        {
            foreach (string error in this.RequireParticipants(context, 2))
            {
                yield return error;
            }

            if (this.Options.BodyParts == null || this.Options.BodyParts.Count == 0)
            {
                yield return $"Indicator '{this.Name}' needs at least one body part (Options.BodyParts).";
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

            var configuration = new PhysicalSynchronyConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                BodyParts = new List<string>(this.Options.BodyParts),
                WindowDuration = context.WindowDuration,
                ComputationInterval = TimeSpan.Zero,
                ComputeSubsets = withSubsets,
                SubsetSize = withSubsets ? this.Options.SubsetSize : 3,
                ComputeOnDataReception = false,
                Normalization = this.Options.Normalization,
                RequireAllParticipants = this.Options.RequireAllParticipants,
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new PhysicalSynchronyComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("Synchrony")), context.Store);

            context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);
            context.PublishPair(this.Name, this.Component.Out);

            // The group score is only posted when the component is asked for it: declaring
            // it otherwise would make the score wait for a value that never comes.
            if (configuration.ComputeGroupScore)
            {
                context.PublishGroup(this.Name, this.Component.GroupSynchronyOut);
                context.PublishScoreInput(this.Name, this.Component.GroupSynchronyOut);
            }
        }
    }
}
