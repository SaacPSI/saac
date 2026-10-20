// <copyright file="IndicatorRegistry.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// The kinds of indicators that can be declared by name, from a configuration file or
    /// from <c>builder.AddIndicator("Synchrony")</c>. The indicators of the library are
    /// registered by <see cref="Indicators"/>; register yours once at start-up.
    /// </summary>
    public static class IndicatorRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, IndicatorType> Types = new Dictionary<string, IndicatorType>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Gets the registered names, sorted.</summary>
        public static IReadOnlyList<string> Names
        {
            get
            {
                EnsureBuiltInTypes();
                lock (Sync)
                {
                    return Types.Values.Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal).ToList();
                }
            }
        }

        /// <summary>Registers a kind of indicator.</summary>
        /// <typeparam name="TIndicator">Class of the indicator.</typeparam>
        /// <typeparam name="TOptions">Options of the indicator.</typeparam>
        /// <param name="name">Name to refer to it by.</param>
        /// <param name="factory">Creates an indicator with its default options.</param>
        /// <returns>The registered kind, to keep in a variable and pass to the builder.</returns>
        public static IndicatorType<TIndicator, TOptions> Register<TIndicator, TOptions>(string name, Func<TIndicator> factory)
            where TIndicator : CollaborationIndicator<TOptions>
            where TOptions : IndicatorOptions, new()
        {
            var type = new IndicatorType<TIndicator, TOptions>(name, factory);
            Register(type);
            return type;
        }

        /// <summary>Registers a kind of indicator whose class has a parameterless constructor.</summary>
        /// <typeparam name="TIndicator">Class of the indicator.</typeparam>
        /// <typeparam name="TOptions">Options of the indicator.</typeparam>
        /// <param name="name">Name to refer to it by.</param>
        /// <returns>The registered kind, to keep in a variable and pass to the builder.</returns>
        public static IndicatorType<TIndicator, TOptions> Register<TIndicator, TOptions>(string name)
            where TIndicator : CollaborationIndicator<TOptions>, new()
            where TOptions : IndicatorOptions, new()
            => Register<TIndicator, TOptions>(name, () => new TIndicator());

        /// <summary>Registers a kind of indicator.</summary>
        /// <param name="type">The kind of indicator.</param>
        public static void Register(IndicatorType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            lock (Sync)
            {
                if (Types.TryGetValue(type.Name, out IndicatorType? existing) && !ReferenceEquals(existing, type))
                {
                    throw new ArgumentException($"An indicator type named '{type.Name}' is already registered.", nameof(type));
                }

                Types[type.Name] = type;
            }
        }

        /// <summary>
        /// Finds a kind of indicator by its registered name or, failing that, by the assembly
        /// qualified name of a class implementing <see cref="ICollaborationIndicator"/>.
        /// </summary>
        /// <param name="name">Registered name or assembly qualified class name.</param>
        /// <param name="type">The kind of indicator.</param>
        /// <returns>True when it was found.</returns>
        public static bool TryResolve(string name, out IndicatorType? type)
        {
            type = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            EnsureBuiltInTypes();
            lock (Sync)
            {
                if (Types.TryGetValue(name, out type))
                {
                    return true;
                }
            }

            Type? indicatorClass = Type.GetType(name, false);
            if (indicatorClass != null && typeof(ICollaborationIndicator).IsAssignableFrom(indicatorClass) && indicatorClass.GetConstructor(Type.EmptyTypes) != null)
            {
                type = new ClassIndicatorType(name, indicatorClass);
                return true;
            }

            return false;
        }

        // The built-in kinds are registered by the static fields of Indicators, which may not
        // have run yet when a configuration is loaded before any of them is touched.
        private static void EnsureBuiltInTypes() => RuntimeHelpers.RunClassConstructor(typeof(Indicators).TypeHandle);

        private sealed class ClassIndicatorType : IndicatorType
        {
            private readonly Type indicatorClass;

            public ClassIndicatorType(string name, Type indicatorClass)
                : base(name, typeof(IndicatorOptions))
            {
                this.indicatorClass = indicatorClass;
            }

            public override ICollaborationIndicator Create() => (ICollaborationIndicator)Activator.CreateInstance(this.indicatorClass)!;
        }
    }
}
