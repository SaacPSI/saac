// <copyright file="TurnTakingIndicator.cs" company="SAAC">
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
    /// Options of <see cref="TurnTakingIndicator"/>.
    /// </summary>
    public class TurnTakingOptions : ComponentIndicatorOptions<TurnTakingConfiguration>
    {
        /// <summary>Gets or sets the event categories counted, each one with its own outputs.</summary>
        public List<string> Categories { get; set; } = new List<string>
        {
            IndexCategories.TurnTakingWithOverlap,
            IndexCategories.TurnTakingWithoutOverlap,
            IndexCategories.Overlap,
        };
    }

    /// <summary>
    /// Turn taking: number of turn takings with and without overlap, and of overlaps, over
    /// the window (<see cref="TurnTakingComponent"/>). Connect the events to
    /// <c>Component.EventsIn</c>, the new speaker in ParticipantId and the previous one in
    /// TargetId. Each category is an index of its own, named after the category.
    /// </summary>
    public class TurnTakingIndicator : ComponentIndicator<TurnTakingOptions, TurnTakingComponent>
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.TurnTaking;

        /// <inheritdoc/>
        public override IEnumerable<string> Validate(IndicatorBuildContext context)
        {
            foreach (string error in this.RequireParticipants(context, 2))
            {
                yield return error;
            }

            if (this.Options.Categories == null || this.Options.Categories.Count == 0)
            {
                yield return $"Indicator '{this.Name}' needs at least one category (Options.Categories).";
            }
        }

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            var configuration = new TurnTakingConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                WindowDuration = context.WindowDuration,
                ComputationInterval = TimeSpan.Zero,
                ComputeOnDataReception = false,
                Categories = new List<string>(this.Options.Categories),
                CategoryNormalizers = new Dictionary<string, IIndexNormalizer>
                {
                    { IndexCategories.TurnTakingWithOverlap, context.NormalizerFor(IndexNames.TurnTakingWithOverlap) },
                    { IndexCategories.TurnTakingWithoutOverlap, context.NormalizerFor(IndexNames.TurnTakingWithoutOverlap) },
                },
                PairNormalizers = new Dictionary<string, IIndexNormalizer>
                {
                    { IndexCategories.TurnTakingWithoutOverlap, context.NormalizerFor(IndexNames.TurnTakingWithoutOverlapPair) },
                },
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new TurnTakingComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("TurnTaking")));
            context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);

            foreach (string category in configuration.Categories)
            {
                string key = category;
                string indexName = this.IndexNameOf(key);

                context.PublishGroup(indexName, this.Component.GetGroupEmitter(key));
                context.PublishScoreInput(indexName, this.Component.GetGroupEmitter(key));
                context.PublishPair(
                    indexName,
                    this.Component.PairTotalsOut.Select(totals => new Dictionary<ParticipantPair, double>(totals[key])),
                    IndexUsage.Export);
            }

            // The Silence indicator, when it is declared, measures the silence on the speech
            // itself and publishes it under that name: the one cumulated from silence events
            // is then left out.
            if (!string.IsNullOrEmpty(configuration.SilenceCategory) && !context.Contains(this.IndexNameOf(configuration.SilenceCategory)))
            {
                context.PublishGroup(this.IndexNameOf(configuration.SilenceCategory), this.Component.SilenceOut);
            }
        }

        // A category is an index name as it is; a renamed indicator prefixes it, so that two
        // turn taking indicators can live in the same instance.
        private string IndexNameOf(string category)
            => string.IsNullOrEmpty(this.Options.Name) ? category : $"{this.Options.Name}_{category}";
    }
}
