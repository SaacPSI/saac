// <copyright file="CollaborationIndicesConfigurationException.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Thrown by <see cref="CollaborationIndicesBuilder.Build"/> when the declared indicators
    /// cannot be built. Every problem found is reported at once, in <see cref="Errors"/> and
    /// in the message, rather than one per attempt.
    /// </summary>
    public class CollaborationIndicesConfigurationException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollaborationIndicesConfigurationException"/> class.
        /// </summary>
        /// <param name="errors">One message per problem.</param>
        public CollaborationIndicesConfigurationException(IEnumerable<string> errors)
            : this((errors ?? Enumerable.Empty<string>()).ToList())
        {
        }

        private CollaborationIndicesConfigurationException(List<string> errors)
            : base(Describe(errors))
        {
            this.Errors = errors;
        }

        /// <summary>Gets one message per problem.</summary>
        public IReadOnlyList<string> Errors { get; }

        private static string Describe(List<string> errors)
        {
            if (errors.Count == 1)
            {
                return "Invalid collaboration indices configuration: " + errors[0];
            }

            return $"Invalid collaboration indices configuration ({errors.Count} problems):" + Environment.NewLine
                + string.Join(Environment.NewLine, errors.Select(error => " - " + error));
        }
    }
}
