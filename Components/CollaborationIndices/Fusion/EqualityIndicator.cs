// <copyright file="EqualityIndicator.cs" company="SAAC">
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
    /// Options of <see cref="EqualityIndicator"/>.
    /// </summary>
    public class EqualityOptions : ComponentIndicatorOptions<EqualityIndexConfiguration>
    {
        /// <summary>
        /// Gets or sets the name of the indicator whose per participant values are compared.
        /// It must provide a distribution (verbal participation, task participation, movement).
        /// </summary>
        public string? Source { get; set; } = null;

        /// <summary>
        /// Gets or sets the size of the sub-groups that also get an index (3 for triads).
        /// Zero disables them; they are skipped anyway when the group is smaller.
        /// </summary>
        public int SubsetSize { get; set; } = 3;

        /// <summary>
        /// Gets or sets the total below which the distribution is too sparse and the index is
        /// published as undefined.
        /// </summary>
        public double MinimumTotal { get; set; } = 0;
    }

    /// <summary>
    /// Equality of a distribution over the participants (<see cref="EqualityIndexComponent"/>):
    /// speech equality from the verbal participation, task equality from the task
    /// participation, or any other distribution through <c>Options.Source</c>.
    ///
    /// The published index is the Gini inequality (0 is perfectly equal), as it always was.
    /// The collaboration score receives the equality, 1 - Gini, and ignores it while the
    /// index is undefined or while its source reports that it is not usable.
    /// </summary>
    public class EqualityIndicator : ComponentIndicator<EqualityOptions, EqualityIndexComponent>
    {
        private readonly string defaultName;

        /// <summary>
        /// Initializes a new instance of the <see cref="EqualityIndicator"/> class, whose
        /// source has to be set in the options.
        /// </summary>
        public EqualityIndicator()
            : this("Equality", null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EqualityIndicator"/> class.
        /// </summary>
        /// <param name="defaultName">Default name of the indicator.</param>
        /// <param name="defaultSource">Default name of the indicator providing the distribution.</param>
        public EqualityIndicator(string defaultName, string? defaultSource)
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

            if (this.Options.SubsetSize != 0 && this.Options.SubsetSize < 3)
            {
                yield return $"Indicator '{this.Name}': SubsetSize must be 0 (no sub-group) or at least 3, not {this.Options.SubsetSize}.";
            }
        }

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            var source = (IDistributionSource)context.Find(this.Options.Source!)!;
            bool withSubsets = this.Options.SubsetSize >= 3 && context.ParticipantIds.Count >= this.Options.SubsetSize;

            var configuration = new EqualityIndexConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                WindowDuration = context.WindowDuration,
                ComputationInterval = TimeSpan.Zero,
                SubsetSize = withSubsets ? this.Options.SubsetSize : 0,
                ComputeOnDataReception = false,
                MinimumTotal = this.Options.MinimumTotal,
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new EqualityIndexComponent(context.Pipeline, configuration, context.ComponentName(this.Name), context.Store);
            source.Distribution.PipeTo(this.Component.In);

            // Computed from the source, so published exactly when the source publishes.
            context.FollowPulseOf(this.Options.Source!);
            context.PublishGroup(this.Name, this.Component.Out);
            context.PublishPair(this.Name, this.Component.PairOut, IndexUsage.Graph);

            // The undefined marker (-1 by default) must not reach the score as 1 - (-1) = 2:
            // it is passed as 0 and flagged unusable instead.
            double undefined = configuration.UndefinedValue;
            bool publishedAsEquality = configuration.PublishAsEquality;
            context.PublishScoreInput(this.Name, this.Component.Out.Select(value => value == undefined ? 0.0 : (publishedAsEquality ? value : 1.0 - value)));
            context.PublishValidity(this.Name, this.Component.Out.Select(value => value != undefined));

            if (source.DistributionUsable != null)
            {
                context.PublishValidity(this.Name, source.DistributionUsable);
            }
        }
    }
}
