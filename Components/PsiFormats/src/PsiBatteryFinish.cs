using System;

namespace SAAC.PsiFormats
{
    /// <summary>
    /// Structure representing the summary of a battery assembly at the end of the task.
    /// Matches the Unity sendData signature:
    /// (bool negativeBorn,
    ///  bool positiveBorn,
    ///  bool frontBorn,
    ///  bool backBorn,
    ///  bool onlyTwoBorns,
    ///  int completedSpaces,
    ///  int totalSpaces,
    ///  int[] givenVoltages,
    ///  int[] voltagesRequired,
    ///  int matchVoltages,
    ///  bool regulated)
    /// </summary>
    public struct PsiBatteryFinish
    {
        public bool NegativeBorn { get; set; }
        public bool PositiveBorn { get; set; }
        public bool FrontBorn { get; set; }
        public bool BackBorn { get; set; }
        public bool OnlyTwoBorns { get; set; }

        public int CompletedSpaces { get; set; }
        public int TotalSpaces { get; set; }

        public int[] GivenVoltages { get; set; }
        public int[] VoltagesRequired { get; set; }

        public int MatchVoltages { get; set; }
        public bool Regulated { get; set; }

        public PsiBatteryFinish(
            bool negativeBorn,
            bool positiveBorn,
            bool frontBorn,
            bool backBorn,
            bool onlyTwoBorns,
            int completedSpaces,
            int totalSpaces,
            int[] givenVoltages,
            int[] voltagesRequired,
            int matchVoltages,
            bool regulated)
        {
            NegativeBorn = negativeBorn;
            PositiveBorn = positiveBorn;
            FrontBorn = frontBorn;
            BackBorn = backBorn;
            OnlyTwoBorns = onlyTwoBorns;

            CompletedSpaces = completedSpaces;
            TotalSpaces = totalSpaces;

            GivenVoltages = givenVoltages ?? Array.Empty<int>();
            VoltagesRequired = voltagesRequired ?? Array.Empty<int>();

            MatchVoltages = matchVoltages;
            Regulated = regulated;
        }
    }
}


