// <copyright file="IndexSnapshot.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Every index of one tick, in a single message.
    ///
    /// The indices are computed by independent components, so their values for a given tick
    /// reach a consumer as separate messages, in no guaranteed order. A snapshot is built once
    /// all of them are known (see <see cref="IndexSnapshotAssembler"/>): whoever receives it
    /// reads values that all describe the same instant.
    ///
    /// An index that was not published at this tick keeps its last known value.
    /// </summary>
    public class IndexSnapshot
    {
        /// <summary>Gets or sets the tick the snapshot describes.</summary>
        public DateTime OriginatingTime { get; set; }

        /// <summary>Gets or sets the group level values, by index name.</summary>
        public Dictionary<string, double> Group { get; set; } = new Dictionary<string, double>();

        /// <summary>Gets or sets the values per participant, by index name.</summary>
        public Dictionary<string, Dictionary<uint, double>> Individual { get; set; } = new Dictionary<string, Dictionary<uint, double>>();

        /// <summary>Gets or sets the values per pair, by index name.</summary>
        public Dictionary<string, Dictionary<ParticipantPair, double>> Pair { get; set; } = new Dictionary<string, Dictionary<ParticipantPair, double>>();

        /// <summary>Gets or sets the values per ordered pair, by index name.</summary>
        public Dictionary<string, Dictionary<DirectedParticipantPair, double>> DirectedPair { get; set; } = new Dictionary<string, Dictionary<DirectedParticipantPair, double>>();

        /// <summary>Gets or sets the normalized indices that feed the collaboration score.</summary>
        public Dictionary<string, double> ScoreInputs { get; set; } = new Dictionary<string, double>();

        /// <summary>Gets or sets whether each score input is usable. An absent name means usable.</summary>
        public Dictionary<string, bool> Validity { get; set; } = new Dictionary<string, bool>();

        /// <summary>Independent copy, safe to keep or to post.</summary>
        /// <returns>The copy.</returns>
        public IndexSnapshot Clone()
        {
            var copy = new IndexSnapshot
            {
                OriginatingTime = this.OriginatingTime,
                Group = new Dictionary<string, double>(this.Group),
                ScoreInputs = new Dictionary<string, double>(this.ScoreInputs),
                Validity = new Dictionary<string, bool>(this.Validity),
            };

            foreach (var entry in this.Individual)
            {
                copy.Individual[entry.Key] = new Dictionary<uint, double>(entry.Value);
            }

            foreach (var entry in this.Pair)
            {
                copy.Pair[entry.Key] = new Dictionary<ParticipantPair, double>(entry.Value);
            }

            foreach (var entry in this.DirectedPair)
            {
                copy.DirectedPair[entry.Key] = new Dictionary<DirectedParticipantPair, double>(entry.Value);
            }

            return copy;
        }
    }
}
