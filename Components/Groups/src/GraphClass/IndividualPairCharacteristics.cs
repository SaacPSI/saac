// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// State of a group at one instant, for a sociogram: a node per participant, an edge per
    /// pair of participants and the values of the group.
    ///
    /// It holds any number of participants and any set of indices. The metrics are kept by
    /// name (<see cref="GraphMetricNames"/>), under the names the collaboration indices give
    /// them: an index that is not computed is absent, and what draws the graph leaves it out.
    /// </summary>
    public class IndividualPairCharacteristics
    {
        /// <summary>Gets or sets the participants.</summary>
        public List<PersonNode> PersonNodes { get; set; } = new List<PersonNode>();

        /// <summary>Gets or sets the pairs of participants.</summary>
        public List<PersonEdge> PersonEdges { get; set; } = new List<PersonEdge>();

        /// <summary>Gets or sets the values of the group.</summary>
        public PersonGroup PersonGroup { get; set; } = new PersonGroup();

        /// <summary>
        /// Gets or sets the length of the sliding window the metrics are computed on, in
        /// seconds; 0 when it is not known.
        /// </summary>
        public double WindowSeconds { get; set; }

        /// <summary>
        /// A graph without metrics for a number of participants, named "1", "2", and so on.
        /// </summary>
        /// <param name="participantCount">Number of participants.</param>
        /// <returns>The graph: a node per participant and an edge per pair.</returns>
        public static IndividualPairCharacteristics ForParticipants(int participantCount)
            => ForParticipants(Enumerable.Range(1, System.Math.Max(0, participantCount)).Select(number => number.ToString(CultureInfo.InvariantCulture)));

        /// <summary>A graph without metrics for the participants given.</summary>
        /// <param name="ids">Identifier of each participant.</param>
        /// <returns>The graph: a node per participant and an edge per pair.</returns>
        public static IndividualPairCharacteristics ForParticipants(IEnumerable<string> ids)
        {
            var graph = new IndividualPairCharacteristics();
            List<string> participants = ids.Distinct().ToList();
            for (int i = 0; i < participants.Count; i++)
            {
                graph.PersonNodes.Add(new PersonNode { Id = participants[i] });

                // The pairs in the order 1-2, 1-3, 2-3, 1-4...
                for (int j = 0; j < i; j++)
                {
                    graph.PersonEdges.Add(PersonEdge.Between(participants[j], participants[i]));
                }
            }

            return graph;
        }

        /// <summary>The node of a participant.</summary>
        /// <param name="id">Identifier of the participant.</param>
        /// <returns>The node, or null when the graph has none for it.</returns>
        public PersonNode? FindNode(string id) => this.PersonNodes?.FirstOrDefault(node => node != null && node.Id == id);

        /// <summary>The edge of two participants.</summary>
        /// <param name="a">One participant.</param>
        /// <param name="b">The other one.</param>
        /// <returns>The edge, or null when the graph has none for them.</returns>
        public PersonEdge? FindEdge(string a, string b) => this.PersonEdges?.FirstOrDefault(edge => edge != null && edge.Links(a, b));
    }
}
