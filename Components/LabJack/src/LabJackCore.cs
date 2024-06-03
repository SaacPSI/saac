using Microsoft.Psi;
using Microsoft.Psi.Components;
using LabJack.LabJackUD;

namespace SAAC.LabJackComponent
{
     /// <summary>
     /// Internal LabJack communicator component class.
     /// </summary>
    internal class LabJackCore : ISourceComponent, IDisposable
    {
        //private LJUD Device;
        private int deviceHandle;
        private LabJackCoreConfiguration configuration;
        private Thread? captureThread = null;
        private bool shutdown = false;
        private readonly object commandsLock = new object();
        private bool firstNexptOptionGetter = true;

        private Connector<Commands> inCommandsReceiverConnector;

        public Receiver<Commands> InCommandsReceiver => inCommandsReceiverConnector.In;

        public Emitter<bool> OutCommandsAck { get; private set; }

        public Emitter<double> OutDoubleValue { get; private set; }

        public LabJackCore(Pipeline pipeline, LabJackCoreConfiguration? config = null)
        {
            configuration = config ?? new LabJackCoreConfiguration();
            inCommandsReceiverConnector = pipeline.CreateConnector<Commands>(nameof(inCommandsReceiverConnector));
            OutCommandsAck = pipeline.CreateEmitter<bool>(this, nameof(OutCommandsAck));
            OutDoubleValue = pipeline.CreateEmitter<double>(this, nameof(OutDoubleValue));
            inCommandsReceiverConnector.Out.Process<Commands, bool>(ProcessCommands);
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            // Open
            switch (configuration.DeviceType)
            {
                case LabJackCoreConfiguration.LabJackType.U3:
                    {
                        U3 device = new U3(configuration.ConnnectionType, configuration.DeviceAdress, configuration.FirstDeviceFound);
                        //Device = device;
                        deviceHandle = device.ljhandle;
                    }
                    break;
                case LabJackCoreConfiguration.LabJackType.U6:
                    { 
                        U6 device = new U6(configuration.ConnnectionType, configuration.DeviceAdress, configuration.FirstDeviceFound);
                        //Device = device;
                        deviceHandle = device.ljhandle;
                    }
                    break;
                case LabJackCoreConfiguration.LabJackType.UE9:
                    {
                        UE9 device = new UE9(configuration.ConnnectionType, configuration.DeviceAdress, configuration.FirstDeviceFound);
                        //Device = device;
                        deviceHandle = device.ljhandle;
                    }
                    break;
            }
            if(ProcessCommands(configuration.Commands) == false)
                throw new Exception("LabJackCore: Failed to process configuration commands");

            captureThread = new Thread(new ThreadStart(CaptureThreadProcess));
            captureThread.Start();
        }
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            shutdown = true;
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (captureThread != null && captureThread.Join(waitTime) != true)
                captureThread.Abort();
            notifyCompleted();
        }
        public void Dispose()
        {
            //Device = null;
        }
        private void ProcessCommands(Commands commands, Envelope envelope, Emitter<bool> response)
        {
            configuration.Commands = commands;
            response.Post(ProcessCommands(commands), envelope.OriginatingTime);
        }
        public bool ProcessCommands(Commands commands)
        {
            if(commands.PutCommands.Count == 0 && commands.RequestCommands.Count == 0)
                return false;
            try
            {
                lock (commandsLock)
                {
                    foreach (PutCommand put in commands.PutCommands)
                        LJUD.ePut(deviceHandle, put.IoType, put.Channel, put.Val, put.X1);

                    foreach (RequestCommand req in commands.RequestCommands)
                        LJUD.AddRequest(deviceHandle, req.IoType, req.Channel, req.Val, req.X1, req.UserData);

                    // ToDo
                    //LJUD.eTCConfig
                    //LJUD.eAIN
                    //LJUD.eDAC
                    //LJUD.eDI
                    //LJUD.eDO
                    LJUD.GoOne(deviceHandle);
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
            while (!shutdown)
            {
                LJUD.IO ioType = 0;
                LJUD.CHANNEL channel = 0;
                double dblValue = 0;
                int dummyInt = 0;
                double dummyDouble = 0;
                lock (commandsLock)
                {
                    try
                    {
                        switch(configuration.Commands.ResponseCommand.GetterType)
                        {
                            case ResponseCommand.EGetterType.First_Next:
                                if (firstNexptOptionGetter)
                                {
                                    LJUD.GetFirstResult(deviceHandle, ref ioType, ref channel, ref dblValue, ref dummyInt, ref dummyDouble);
                                    firstNexptOptionGetter = false;
                                }
                                else
                                    LJUD.GetNextResult(deviceHandle, ref ioType, ref channel, ref dblValue, ref dummyInt, ref dummyDouble);
                                OutDoubleValue.Post(dblValue, DateTime.UtcNow);
                                break;
                            case ResponseCommand.EGetterType.E_Get:
                                //LJUD.eGet(deviceHandle, LJUD.IO.GET_TIMER, 0, ref dblValue, 0);
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
            //Type device = new Type(configuration.ConnnectionType, configuration.DeviceAdress, configuration.FirstDeviceFound);
            //Device = device;
            //deviceHandle = device.ljhandle;
        }
    }
}
