// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    /// <summary>
    /// Internal helper class providing utility functions for group operations.
    /// </summary>
    internal class GroupsHelpers
    {
        /// <summary>
        /// Computes a unique identifier for a pair of numbers using Cantor pairing function.
        /// </summary>
        /// <param name="k1">The first number.</param>
        /// <param name="k2">The second number.</param>
        /// <returns>A unique identifier for the pair.</returns>
        public static uint CantorPairing(uint k1, uint k2)
        {
            return (uint)(0.5 * (k1 + k2) * (k1 + k2 + 1) + k2);
        }

        /// <summary>
        /// Computes a unique identifier for a sequence of numbers using successive Cantor pairing.
        /// </summary>
        /// <param name="set">The list of numbers.</param>
        /// <returns>A unique identifier for the sequence.</returns>
        public static uint CantorParingSequence(List<uint> set)
        {
            uint value = set.ElementAt(0);
            for (int iterator = 1; iterator < set.Count(); iterator++)
            {
                uint value2 = set[iterator];
                value = CantorPairing(value, value2);
            }

            return value;
        }

        /// <summary>
        /// Reduces and merges groups by combining groups that share members.
        /// </summary>
        /// <param name="groups">The dictionary of groups to reduce.</param>
        public static void ReduceGroups(ref Dictionary<uint, List<uint>> groups)
        {
            bool call = false;
            foreach (var group in groups)
            {
                foreach (var id in group.Value)
                {
                    if (groups.ContainsKey(id) && group.Key != id)
                    {
                        groups[group.Key].AddRange(groups[id]);
                        groups.Remove(id);
                        call = true;
                        break;
                    }
                }

                if (call)
                {
                    break;
                }

                group.Value.Sort();
                groups[group.Key] = group.Value.Distinct().ToList();
            }

            if (call)
            {
                ReduceGroups(ref groups);
            }
        }

        /// <summary>
        /// Generates unique group identifiers using Cantor pairing and reduces overlapping groups.
        /// </summary>
        /// <param name="groups">The dictionary of groups to process.</param>
        /// <returns>A dictionary with unique Cantor-paired identifiers for each reduced group.</returns>
        public static Dictionary<uint, List<uint>> GenerateGroups(ref Dictionary<uint, List<uint>> groups)
        {
            Dictionary<uint, List<uint>> cantorGroups = new Dictionary<uint, List<uint>>();
            ReduceGroups(ref groups);
            foreach (var group in groups)
            {
                uint uid = CantorParingSequence(group.Value);
                cantorGroups.Add(uid, group.Value);
            }

            return cantorGroups;
        }
    }
}
