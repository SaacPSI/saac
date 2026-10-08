// <copyright file="IndicatorType.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// A kind of indicator known by name: what a configuration file refers to in "Type".
    /// </summary>
    public abstract class IndicatorType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndicatorType"/> class.
        /// </summary>
        /// <param name="name">Name of the kind of indicator.</param>
        /// <param name="optionsType">Type of its options.</param>
        protected IndicatorType(string name, Type optionsType)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("An indicator type needs a name.", nameof(name));
            }

            this.Name = name;
            this.OptionsType = optionsType;
        }

        /// <summary>Gets the name of the kind of indicator, which is also the default name of its instances.</summary>
        public string Name { get; }

        /// <summary>Gets the type of the options.</summary>
        public Type OptionsType { get; }

        /// <summary>Creates an indicator of this kind, with its default options.</summary>
        /// <returns>The indicator.</returns>
        public abstract ICollaborationIndicator Create();

        /// <inheritdoc/>
        public override string ToString() => this.Name;
    }

    /// <summary>
    /// A kind of indicator, with the types needed to configure it and to read it back
    /// without naming its class:
    /// <c>builder.AddIndicator(Indicators.Synchrony, o => o.SubsetSize = 3)</c>, then
    /// <c>indices.Get(Indicators.Synchrony).Component</c>.
    /// </summary>
    /// <typeparam name="TIndicator">Class of the indicator.</typeparam>
    /// <typeparam name="TOptions">Options of the indicator.</typeparam>
    public sealed class IndicatorType<TIndicator, TOptions> : IndicatorType
        where TIndicator : CollaborationIndicator<TOptions>
        where TOptions : IndicatorOptions, new()
    {
        private readonly Func<TIndicator> factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndicatorType{TIndicator, TOptions}"/> class.
        /// </summary>
        /// <param name="name">Name of the kind of indicator.</param>
        /// <param name="factory">Creates an indicator with its default options.</param>
        public IndicatorType(string name, Func<TIndicator> factory)
            : base(name, typeof(TOptions))
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc/>
        public override ICollaborationIndicator Create() => this.CreateTyped();

        /// <summary>Creates an indicator of this kind, with its default options.</summary>
        /// <returns>The indicator.</returns>
        public TIndicator CreateTyped() => this.factory();
    }
}
