// <copyright file="DominanceIndicator.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Options of <see cref="DominanceIndicator"/>.
    /// </summary>
    public class DominanceOptions : ComponentIndicatorOptions<DominanceIdentityConfiguration>
    {
        /// <summary>
        /// Gets or sets the name of the indicator whose per participant values are compared.
        /// It must provide a distribution (verbal participation, task participation, movement).
        /// </summary>
        public string? Source { get; set; } = null;
    }

    /// <summary>
    /// Identity of the participant who does the most of something
    /// (<see cref="DominanceIdentityComponent"/>): talking most from the verbal participation,
    /// tasking most from the task participation, or any other distribution through
    /// <c>Options.Source</c>. The identity is published as id + 1, and 0 when nobody stands out.
    /// </summary>
    public class DominanceIndicator : ComponentIndicator<DominanceOptions, DominanceIdentityComponent>
    {
        private readonly string defaultName;

        /// <summary>
        /// Initializes a new instance of the <see cref="DominanceIndicator"/> class, whose
        /// source has to be set in the options.
        /// </summary>
        public DominanceIndicator()
            : this("Dominance", null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DominanceIndicator"/> class.
        /// </summary>
        /// <param name="defaultName">Default name of the indicator.</param>
        /// <param name="defaultSource">Default name of the indicator providing the distribution.</param>
        public DominanceIndicator(string defaultName, string? defaultSource)
        {
            this.defaultName = defaultName;
            this.Options.Source = defaultSource;
        }

        /// <inheritdoc/>
        public override string DefaultName => this.defaultName;

        /// <inheritdoc/>
        public override IEnumerable<string> Dependencies
        {
            get
            {
                if (!string.IsNullOrEmpty(this.Options.Source))
                {
                    yield return this.Options.Source!;
                }
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<string> Validate(IndicatorBuildContext context)
        {
            foreach (string error in this.RequireParticipants(context, 2))
            {
                yield return error;
            }

            if (string.IsNullOrEmpty(this.Options.Source))
            {
                yield return $"Indicator '{this.Name}' needs the name of the indicator it compares (Options.Source).";
            }
            else if (context.Find(this.Options.Source!) is ICollaborationIndicator source && !(source is IDistributionSource))
            {
                yield return $"Indicator '{this.Name}' cannot use '{this.Options.Source}' as its source: it does not provide one value per participant.";
            }
        }

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            var source = (IDistributionSource)context.Find(this.Options.Source!)!;

            var configuration = new DominanceIdentityConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new DominanceIdentityComponent(context.Pipeline, configuration, context.ComponentName(this.Name));
            source.Distribution.PipeTo(this.Component.In);

            // Computed from the source, so published exactly when the source publishes.
            context.FollowPulseOf(this.Options.Source!);
            context.PublishGroup(this.Name, this.Component.Out);
            context.PublishPair(this.Name, this.Component.PairOut, IndexUsage.Export);
        }
    }
}
