// <copyright file="ICollaborationIndicator.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// One collaboration indicator, as seen by <see cref="CollaborationIndicesBuilder"/>.
    ///
    /// An indicator does not compute anything itself: it creates the \psi component(s) that
    /// do, and tells the builder what they need (a clock, other indicators) and what they
    /// offer (named outputs). Everything else, the phase gate, the score, the graph and the
    /// export, is wired by the builder from those declarations, which is why a new indicator
    /// needs no change in the core of the library.
    /// </summary>
    public interface ICollaborationIndicator
    {
        /// <summary>Gets the name of the indicator, unique inside an instance.</summary>
        string Name { get; }

        /// <summary>Gets the options of the indicator.</summary>
        IndicatorOptions Options { get; }

        /// <summary>
        /// Gets the names of the indicators this one reads from. They are built first, and a
        /// missing one is reported when the configuration is validated.
        /// </summary>
        IEnumerable<string> Dependencies { get; }

        /// <summary>
        /// Checks the options against the context. Called before anything is created.
        /// </summary>
        /// <param name="context">Participants, window and the other declared indicators.</param>
        /// <returns>One message per problem, empty when the options are valid.</returns>
        IEnumerable<string> Validate(IndicatorBuildContext context);

        /// <summary>
        /// Creates the component(s) and declares their clock and their outputs.
        /// </summary>
        /// <param name="context">Everything the indicator needs to wire itself.</param>
        void Build(IndicatorBuildContext context);
    }

    /// <summary>
    /// Implemented by an indicator whose main result is one value per participant, so that
    /// an equality or a dominance indicator can be derived from it.
    /// </summary>
    public interface IDistributionSource
    {
        /// <summary>Gets the raw value of every participant.</summary>
        IProducer<Dictionary<uint, double>> Distribution { get; }

        /// <summary>
        /// Gets a flag telling whether the distribution is dense enough for an equality index
        /// to be meaningful, or null when it always is.
        /// </summary>
        IProducer<bool>? DistributionUsable { get; }
    }
}
