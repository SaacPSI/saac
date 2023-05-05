using Microsoft.Psi;
using Microsoft.Psi.Components;
using LabJack.LabJackUD;

namespace LabJackComponent
{
     /// <summary>
     /// Internal LabJack communicator component class.
     /// </summary>
    internal class LabJackCore : ISourceComponent, IDisposable
    {
        //private LJUD Device;
        private int DeviceHandle;
        private LabJackCoreConfiguration Configuration;
        private Thread? CaptureThread = null;
        private bool Shutdown = false;
        private readonly object CommandsLock = new object();
        private bool FirstNexptOptionGetter = true;


        private Connector<Commands> InCommandsReceiverConnector;

        public Receiver<Commands> InCommandsReceiver => InCommandsReceiverConnector.In;

        public Emitter<bool> OutCommandsAck { get; private set; }

        public Emitter<double> OutDoubleValue { get; private set; }
        public LabJackCore(Pipeline pipeline, LabJackCoreConfiguration? config = null)
        {
            Configuration = config ?? new LabJackCoreConfiguration();
            InCommandsReceiverConnector = pipeline.CreateConnector<Commands>(nameof(InCommandsReceiverConnector));
            OutCommandsAck = pipeline.CreateEmitter<bool>(this, nameof(OutCommandsAck));
            OutDoubleValue = pipeline.CreateEmitter<double>(this, nameof(OutDoubleValue));
            InCommandsReceiverConnector.Out.Process<Commands, bool>(ProcessCommands);
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            // Open
            switch (Configuration.DeviceType)
            {
                case LabJackCoreConfiguration.LabJackType.U3:
                    {
                        U3 device = new U3(Configuration.ConnnectionType, Configuration.DeviceAdress, Configuration.FirstDeviceFound);
                        //Device = device;
                        DeviceHandle = device.ljhandle;
                    }
                    break;
                case LabJackCoreConfiguration.LabJackType.U6:
                    { 
                        U6 device = new U6(Configuration.ConnnectionType, Configuration.DeviceAdress, Configuration.FirstDeviceFound);
                        //Device = device;
                        DeviceHandle = device.ljhandle;
                    }
                    break;
                case LabJackCoreConfiguration.LabJackType.UE9:
                    {
                        UE9 device = new UE9(Configuration.ConnnectionType, Configuration.DeviceAdress, Configuration.FirstDeviceFound);
                        //Device = device;
                        DeviceHandle = device.ljhandle;
                    }
                    break;
            }
            if(ProcessCommands(Configuration.Commands) == false)
                throw new Exception("LabJackCore: Failed to process configuration commands");

            CaptureThread = new Thread(new ThreadStart(CaptureThreadProcess));
            CaptureThread.Start();
        }
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            Shutdown = true;
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (CaptureThread != null && CaptureThread.Join(waitTime) != true)
                CaptureThread.Abort();
            notifyCompleted();
        }
        public void Dispose()
        {
            //Device = null;
        }
        private void ProcessCommands(Commands commands, Envelope envelope, Emitter<bool> response)
        {
            Configuration.Commands = commands;
            response.Post(ProcessCommands(commands), envelope.OriginatingTime);
        }
        public bool ProcessCommands(Commands commands)
        {
            if(commands.PutCommands.Count == 0 && commands.RequestCommands.Count == 0)
                return false;
            try
            {
                lock (CommandsLock)
                {
                    foreach (PutCommand put in commands.PutCommands)
                        LJUD.ePut(DeviceHandle, put.IoType, put.Channel, put.Val, put.X1);

                    foreach (RequestCommand req in commands.RequestCommands)
                        LJUD.AddRequest(DeviceHandle, req.IoType, req.Channel, req.Val, req.X1, req.UserData);

                    // ToDo
                    //LJUD.eTCConfig
                    //LJUD.eAIN
                    //LJUD.eDAC
                    //LJUD.eDI
                    //LJUD.eDO
                    LJUD.GoOne(DeviceHandle);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message);
                return false;
            }
            return true;
        }

        private void CaptureThreadProcess()
        {
            while (!Shutdown)
            {
                LJUD.IO ioType = 0;
                LJUD.CHANNEL channel = 0;
                double dblValue = 0;
                int dummyInt = 0;
                double dummyDouble = 0;
                lock (CommandsLock)
                {
                    try
                    {
                        switch(Configuration.Commands.ResponseCommand.GetterType)
                        {
                            case ResponseCommand.EGetterType.First_Next:
                                if (FirstNexptOptionGetter)
                                {
                                    LJUD.GetFirstResult(DeviceHandle, ref ioType, ref channel, ref dblValue, ref dummyInt, ref dummyDouble);
                                    FirstNexptOptionGetter = false;
                                }
                                else
                                    LJUD.GetNextResult(DeviceHandle, ref ioType, ref channel, ref dblValue, ref dummyInt, ref dummyDouble);
                                OutDoubleValue.Post(dblValue, DateTime.UtcNow);
                                break;
                            case ResponseCommand.EGetterType.E_Get:
                                //LJUD.eGet(DeviceHandle, LJUD.IO.GET_TIMER, 0, ref dblValue, 0);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Write(ex.Message);
                        OutCommandsAck.Post(false, DateTime.UtcNow);
                        continue;
                    }
                }
            
            }
        }
        private void CreateDevice<Type>() where Type : new()
        {
            //Type device = new Type(Configuration.ConnnectionType, Configuration.DeviceAdress, Configuration.FirstDeviceFound);
            //Device = device;
            //DeviceHandle = device.ljhandle;
        }
    }
}
