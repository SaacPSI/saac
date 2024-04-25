
namespace SAAC.Groups
{
    internal class GroupsHelpers
    {
        static public uint CantorPairing(uint k1, uint k2)
        {
            return (uint)(0.5 * (k1 + k2) * (k1 + k2 + 1) + k2);
        }

        static public uint CantorParingSequence(List<uint> set)
        {
            uint value = set.ElementAt(0);
            for (int iterator = 1; iterator < set.Count(); iterator++)
            {
                uint value2 = set[iterator];
                value = CantorPairing(value, value2);
            }
            return value;
        }

        static public void ReduceGroups(ref Dictionary<uint, List<uint>> groups)
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
                    break;
                group.Value.Sort();
                groups[group.Key] = group.Value.Distinct().ToList();
            }
            if (call)
                ReduceGroups(ref groups);
        }

        static public Dictionary<uint, List<uint>> GenerateGroups(ref Dictionary<uint, List<uint>> groups)
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
