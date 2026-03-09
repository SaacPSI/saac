namespace SAAC.PsiFormats
{
    /// <summary>
    /// Structure representing battery information from Unity.
    /// Matches the Unity sendData signature: (int id, int tension, int places, bool regulated, int[] modules, string state, float dist)
    /// </summary>
    public struct PsiBatterie
    {
        public int Id { get; set; }
        public int Tension { get; set; }
        public int Places { get; set; }
        public bool Regulated { get; set; }
        public int[] Modules { get; set; }
        public string State { get; set; }
        public float Dist { get; set; }

        public PsiBatterie(int id, int tension, int places, bool regulated, int[] modules, string state, float dist)
        {
            Id = id;
            Tension = tension;
            Places = places;
            Regulated = regulated;
            Modules = modules ?? new int[0];
            State = state ?? string.Empty;
            Dist = dist;
        }
    }
}

