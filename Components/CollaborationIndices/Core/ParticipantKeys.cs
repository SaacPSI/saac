using System;
using System.Collections.Generic;
using System.Linq;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Standard body part identifiers. Using constants instead of hard coded strings
    /// avoids typos and keeps configurations comparable between components.
    /// Any other string is accepted by the components, this is only a convention.
    /// </summary>
    public static class BodyPartNames
    {
        public const string Head = "Head";
        public const string Neck = "Neck";
        public const string SpineChest = "SpineChest";
        public const string Pelvis = "Pelvis";
        public const string LeftHand = "LeftHand";
        public const string RightHand = "RightHand";
        public const string LeftFoot = "LeftFoot";
        public const string RightFoot = "RightFoot";
    }

    /// <summary>
    /// Unordered pair of participants. (0,1) and (1,0) are the same key.
    /// Used as dictionary key for every pairwise indicator.
    /// </summary>
    public readonly struct ParticipantPair : IEquatable<ParticipantPair>, IComparable<ParticipantPair>
    {
        public uint A { get; }

        public uint B { get; }

        public ParticipantPair(uint a, uint b)
        {
            if (a <= b)
            {
                this.A = a;
                this.B = b;
            }
            else
            {
                this.A = b;
                this.B = a;
            }
        }

        public bool Contains(uint participantId) => this.A == participantId || this.B == participantId;

        public bool Equals(ParticipantPair other) => this.A == other.A && this.B == other.B;

        public override bool Equals(object obj) => obj is ParticipantPair other && this.Equals(other);

        // Manual combination rather than HashCode.Combine: System.HashCode only exists
        // from netstandard2.1 / net6.0, and this library must also build on .NET Framework 4.8.
        public override int GetHashCode() => (int)(((long)this.A * 397) ^ this.B);

        public int CompareTo(ParticipantPair other) => this.A != other.A ? this.A.CompareTo(other.A) : this.B.CompareTo(other.B);

        public override string ToString() => $"{this.A}-{this.B}";
    }

    /// <summary>
    /// Ordered set of participants of any size (triad, quartet, whole group...).
    /// Used for indicators that are only defined above the dyadic level.
    /// </summary>
    public readonly struct ParticipantSubset : IEquatable<ParticipantSubset>
    {
        public uint[] Members { get; }

        public ParticipantSubset(IEnumerable<uint> members)
        {
            this.Members = members.Distinct().OrderBy(m => m).ToArray();
        }

        public int Size => this.Members == null ? 0 : this.Members.Length;

        public IEnumerable<ParticipantPair> Pairs() => Combinatorics.Pairs(this.Members);

        public bool Equals(ParticipantSubset other)
        {
            if (this.Size != other.Size)
            {
                return false;
            }

            for (int i = 0; i < this.Size; i++)
            {
                if (this.Members[i] != other.Members[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj) => obj is ParticipantSubset other && this.Equals(other);

        public override int GetHashCode()
        {
            int hash = 17;
            for (int i = 0; i < this.Size; i++)
            {
                hash = (hash * 31) + (int)this.Members[i];
            }

            return hash;
        }

        public override string ToString() => this.Members == null ? string.Empty : string.Join("-", this.Members);
    }

    /// <summary>
    /// Generation of the sub-groups on which an indicator has to be computed.
    /// </summary>
    public static class Combinatorics
    {
        /// <summary>
        /// All unordered pairs of a participant list. n*(n-1)/2 elements.
        /// </summary>
        public static IEnumerable<ParticipantPair> Pairs(IReadOnlyList<uint> participantIds)
        {
            for (int i = 0; i < participantIds.Count; i++)
            {
                for (int j = i + 1; j < participantIds.Count; j++)
                {
                    yield return new ParticipantPair(participantIds[i], participantIds[j]);
                }
            }
        }

        /// <summary>
        /// All subsets of the given size (3 for triads, 4 for quartets...).
        /// </summary>
        public static IEnumerable<ParticipantSubset> Subsets(IReadOnlyList<uint> participantIds, int size)
        {
            if (size <= 0 || size > participantIds.Count)
            {
                yield break;
            }

            int[] indices = Enumerable.Range(0, size).ToArray();
            while (true)
            {
                yield return new ParticipantSubset(indices.Select(i => participantIds[i]));

                int k = size - 1;
                while (k >= 0 && indices[k] == participantIds.Count - size + k)
                {
                    k--;
                }

                if (k < 0)
                {
                    yield break;
                }

                indices[k]++;
                for (int l = k + 1; l < size; l++)
                {
                    indices[l] = indices[l - 1] + 1;
                }
            }
        }
    }
}
