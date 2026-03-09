namespace SAAC.PsiFormats
{
    /// <summary>
    /// Battery module event matching Unity SendEvent signature:
    /// (int batteryId,
    ///  int power,
    ///  int places,
    ///  bool regulated,
    ///  int moduleId,
    ///  string moduleType,
    ///  int modulePower,
    ///  int placeIndex,
    ///  string moduleStatus)
    /// </summary>
    public struct PsiBatteryModuleEvent
    {
        public int BatteryId { get; set; }
        public int Power { get; set; }
        public int Places { get; set; }
        public bool Regulated { get; set; }

        public int ModuleId { get; set; }
        public string ModuleType { get; set; }
        public int ModulePower { get; set; }
        public int PlaceIndex { get; set; }
        public string ModuleStatus { get; set; }

        public PsiBatteryModuleEvent(
            int batteryId,
            int power,
            int places,
            bool regulated,
            int moduleId,
            string moduleType,
            int modulePower,
            int placeIndex,
            string moduleStatus)
        {
            BatteryId = batteryId;
            Power = power;
            Places = places;
            Regulated = regulated;
            ModuleId = moduleId;
            ModuleType = moduleType;
            ModulePower = modulePower;
            PlaceIndex = placeIndex;
            ModuleStatus = moduleStatus;
        }
    }
}

