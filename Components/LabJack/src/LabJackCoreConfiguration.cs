using LabJack.LabJackUD;

namespace SAAC.LabJackComponent
{

    public class LabJackCoreConfiguration
    {
        public enum LabJackType { U3, U6, UE9};
        public LabJackType DeviceType { get; set; } = LabJackType.U6;

        public LJUD.CONNECTION ConnnectionType { get; set; } = LJUD.CONNECTION.USB;

        public string DeviceAdress { get; set; } = "0";

        public bool FirstDeviceFound { get; set; } = true;

        public Commands Commands { get; set; }
    }
}
