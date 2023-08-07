using LabJack.LabJackUD;

namespace LabJackComponent
{
    public struct PutCommand
    {
        public LJUD.IO IoType;
        public LJUD.CHANNEL Channel;
        public double Val;
        public byte[] X1;
    }

    public struct RequestCommand
    {
        public LJUD.IO IoType;
        public LJUD.CHANNEL Channel;
        public double Val;
        public int X1;
        public double UserData;
    }

    public struct ResponseCommand
    {
        public enum EGetterType { First_Next, E_Get };
        public EGetterType GetterType;
    }

    public struct Commands
    {
        public List<PutCommand> PutCommands;
        public List<RequestCommand> RequestCommands;
        public ResponseCommand ResponseCommand;
    }
}
