namespace SAAC.PsiFormats
{
    /// <summary>
    /// Simple battery event matching Unity SendEvent signature:
    /// (int batteryId,
    ///  int power,
    ///  int places,
    ///  bool regulated)
    /// </summary>
    public struct PsiBatteryEvent
    {
        public int BatteryId { get; set; }
        public int Power { get; set; }
        public int Places { get; set; }
        public bool Regulated { get; set; }

        public PsiBatteryEvent(int batteryId, int power, int places, bool regulated)
        {
            BatteryId = batteryId;
            Power = power;
            Places = places;
            Regulated = regulated;
        }
    }
}

