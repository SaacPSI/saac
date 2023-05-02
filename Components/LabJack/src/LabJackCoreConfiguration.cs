using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LabJack.LabJackUD;

namespace LabJackComponent
{
    public struct PutCommand
    {
        public LJUD.IO      IoType;
        public LJUD.CHANNEL Channel;
        public double       Val;
        public byte[]       X1;
    }

    public struct RequestCommand
    {
        public LJUD.IO      IoType;
        public LJUD.CHANNEL Channel;
        public double       Val;
        public int          X1;
        public double       UserData;
    }

    public struct ResponseCommand 
    { 
        public enum EGetterType { First_Next, E_Get };
        public EGetterType GetterType;
    }

    public struct Commands
    {
        public List<PutCommand>     PutCommands;
        public List<RequestCommand> RequestCommands;
        public ResponseCommand      ResponseCommand;
    }

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
