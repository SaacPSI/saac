// <copyright file="CollaborationIndicator.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System.Collections.Generic;
using System.Linq;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Base class of the indicators: it owns the typed options and the naming, a derived
    /// class only writes <see cref="Build"/>.
    /// </summary>
    /// <typeparam name="TOptions">Options type of the indicator.</typeparam>
    public abstract class CollaborationIndicator<TOptions> : ICollaborationIndicator
        where TOptions : IndicatorOptions, new()
    {
        /// <summary>Gets the options of the indicator.</summary>
        public TOptions Options { get; } = new TOptions();

        /// <inheritdoc/>
        IndicatorOptions ICollaborationIndicator.Options => this.Options;

        /// <summary>Gets the name used when the options do not give one.</summary>
        public abstract string DefaultName { get; }

        /// <inheritdoc/>
        public string Name => string.IsNullOrEmpty(this.Options.Name) ? this.DefaultName : this.Options.Name!;

        /// <inheritdoc/>
        public virtual IEnumerable<string> Dependencies => Enumerable.Empty<string>();

        /// <inheritdoc/>
        public virtual IEnumerable<string> Validate(IndicatorBuildContext context) => Enumerable.Empty<string>();

        /// <inheritdoc/>
        public abstract void Build(IndicatorBuildContext context);

        /// <summary>
        /// Suffix of the component name: the historical one as long as the indicator keeps
        /// its default name, the indicator name once it has been renamed.
        /// </summary>
        /// <param name="historicalSuffix">Suffix the component had before the builder existed.</param>
        /// <returns>The suffix to use.</returns>
        protected string SuffixOrName(string historicalSuffix)
            => string.IsNullOrEmpty(this.Options.Name) ? historicalSuffix : this.Options.Name!;

        /// <summary>Error message when the group is too small for the indicator.</summary>
        /// <param name="context">The build context.</param>
        /// <param name="minimum">Minimum number of participants.</param>
        /// <returns>The message, or nothing when the group is large enough.</returns>
        protected IEnumerable<string> RequireParticipants(IndicatorBuildContext context, int minimum)
        {
            if (context.ParticipantIds.Count < minimum)
            {
                yield return $"Indicator '{this.Name}' needs at least {minimum} participants, {context.ParticipantIds.Count} declared.";
            }
        }
    }

    /// <summary>
    /// Base class of the indicators built on a single component, which is exposed so that
    /// the raw streams can be connected to its inputs.
    /// </summary>
    /// <typeparam name="TOptions">Options type of the indicator.</typeparam>
    /// <typeparam name="TComponent">Type of the component.</typeparam>
    public abstract class ComponentIndicator<TOptions, TComponent> : CollaborationIndicator<TOptions>
        where TOptions : IndicatorOptions, new()
        where TComponent : class
    {
        private TComponent? component;

        /// <summary>
        /// Gets the component computing the indicator. Available once the builder has built
        /// the instance; connect the source streams to its inputs.
        /// </summary>
        public TComponent Component
        {
            get => this.component ?? throw new System.InvalidOperationException($"Indicator '{this.Name}' has not been built yet: its component is created by CollaborationIndicesBuilder.Build().");
            protected set => this.component = value;
        }
    }
}
