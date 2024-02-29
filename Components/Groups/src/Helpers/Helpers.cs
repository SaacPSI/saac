
namespace SAAC.Groups.Helpers
{
    public class Helpers
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
    }
}
