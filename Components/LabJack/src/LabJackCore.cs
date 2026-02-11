// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.LabJackComponent
{
    using LabJack.LabJackUD;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Internal LabJack communicator component class.
    /// Handles communication with LabJack devices (U3, U6, UE9) via USB or Ethernet.
    /// </summary>
    internal class LabJackCore : ISourceComponent, IDisposable
    {
        private readonly object commandsLock = new object();
        private readonly LabJackCoreConfiguration configuration;
        private readonly Connector<Commands> inCommandsReceiverConnector;
        private int deviceHandle;
        private Thread? captureThread = null;
        private bool shutdown = false;
        private bool firstNexptOptionGetter = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="LabJackCore"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="config">Optional configuration for the LabJack device.</param>
        public LabJackCore(Pipeline pipeline, LabJackCoreConfiguration? config = null)
        {
            this.configuration = config ?? new LabJackCoreConfiguration();
            this.inCommandsReceiverConnector = pipeline.CreateConnector<Commands>(nameof(this.inCommandsReceiverConnector));
            this.OutCommandsAck = pipeline.CreateEmitter<bool>(this, nameof(this.OutCommandsAck));
            this.OutDoubleValue = pipeline.CreateEmitter<double>(this, nameof(this.OutDoubleValue));
            this.inCommandsReceiverConnector.Out.Process<Commands, bool>(this.ProcessCommands);
        }

        /// <summary>
        /// Gets the receiver for incoming commands.
        /// </summary>
        public Receiver<Commands> InCommandsReceiver => this.inCommandsReceiverConnector.In;

        /// <summary>
        /// Gets the emitter for command acknowledgements.
        /// </summary>
        public Emitter<bool> OutCommandsAck { get; private set; }

        /// <summary>
        /// Gets the emitter for double value outputs.
        /// </summary>
        public Emitter<double> OutDoubleValue { get; private set; }

        /// <summary>
        /// Starts the LabJack device communication.
        /// </summary>
        /// <param name="notifyCompletionTime">Delegate to notify completion time.</param>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            // Open
            switch (this.configuration.DeviceType)
            {
                case LabJackCoreConfiguration.LabJackType.U3:
                    {
                        U3 device = new U3(this.configuration.ConnnectionType, this.configuration.DeviceAdress, this.configuration.FirstDeviceFound);

                        // Device = device;
                        this.deviceHandle = device.ljhandle;
                    }

                    break;
                case LabJackCoreConfiguration.LabJackType.U6:
                    {
                        U6 device = new U6(this.configuration.ConnnectionType, this.configuration.DeviceAdress, this.configuration.FirstDeviceFound);

                        // Device = device;
                        this.deviceHandle = device.ljhandle;
                    }

                    break;
                case LabJackCoreConfiguration.LabJackType.UE9:
                    {
                        UE9 device = new UE9(this.configuration.ConnnectionType, this.configuration.DeviceAdress, this.configuration.FirstDeviceFound);

                        // Device = device;
                        this.deviceHandle = device.ljhandle;
                    }

                    break;
            }

            if (this.ProcessCommands(this.configuration.Commands) == false)
            {
                throw new Exception("LabJackCore: Failed to process configuration commands");
            }

            this.captureThread = new Thread(new ThreadStart(this.CaptureThreadProcess));
            this.captureThread.Start();
        }

        /// <summary>
        /// Stops the LabJack device communication.
        /// </summary>
        /// <param name="finalOriginatingTime">The final originating time.</param>
        /// <param name="notifyCompleted">Delegate to notify completion.</param>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.shutdown = true;
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (this.captureThread != null && this.captureThread.Join(waitTime) != true)
            {
                this.captureThread.Abort();
            }

            notifyCompleted();
        }

        /// <summary>
        /// Disposes the LabJack device and releases resources.
        /// </summary>
        public void Dispose()
        {
            // Device = null;
        }

        /// <summary>
        /// Processes commands received via the pipeline.
        /// </summary>
        /// <param name="commands">The commands to process.</param>
        /// <param name="envelope">The message envelope.</param>
        /// <param name="response">The emitter for the response.</param>
        private void ProcessCommands(Commands commands, Envelope envelope, Emitter<bool> response)
        {
            this.configuration.Commands = commands;
            response.Post(this.ProcessCommands(commands), envelope.OriginatingTime);
        }

        /// <summary>
        /// Processes a set of commands by executing PUT and REQUEST operations on the LabJack device.
        /// </summary>
        /// <param name="commands">The commands to process.</param>
        /// <returns>True if all commands were processed successfully; otherwise false.</returns>
        public bool ProcessCommands(Commands commands)
        {
            if (commands.PutCommands.Count == 0 && commands.RequestCommands.Count == 0)
            {
                return false;
            }

            try
            {
                lock (this.commandsLock)
                {
                    foreach (PutCommand put in commands.PutCommands)
                    {
                        LJUD.ePut(this.deviceHandle, put.IoType, put.Channel, put.Val, put.X1);
                    }

                    foreach (RequestCommand req in commands.RequestCommands)
                    {
                        LJUD.AddRequest(this.deviceHandle, req.IoType, req.Channel, req.Val, req.X1, req.UserData);
                    }

                    // ToDo
                    // LJUD.eTCConfig
                    // LJUD.eAIN
                    // LJUD.eDAC
                    // LJUD.eDI
                    // LJUD.eDO
                    LJUD.GoOne(this.deviceHandle);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Main capture thread process that continuously retrieves data from the LabJack device.
        /// </summary>
        private void CaptureThreadProcess()
        {
            while (!this.shutdown)
            {
                LJUD.IO ioType = 0;
                LJUD.CHANNEL channel = 0;
                double dblValue = 0;
                int dummyInt = 0;
                double dummyDouble = 0;
                lock (this.commandsLock)
                {
                    try
                    {
                        switch (this.configuration.Commands.ResponseCommand.GetterType)
                        {
                            case ResponseCommand.EGetterType.First_Next:
                                if (this.firstNexptOptionGetter)
                                {
                                    LJUD.GetFirstResult(this.deviceHandle, ref ioType, ref channel, ref dblValue, ref dummyInt, ref dummyDouble);
                                    this.firstNexptOptionGetter = false;
                                }
                                else
                                {
                                    LJUD.GetNextResult(this.deviceHandle, ref ioType, ref channel, ref dblValue, ref dummyInt, ref dummyDouble);
                                }

                                this.OutDoubleValue.Post(dblValue, DateTime.UtcNow);
                                break;
                            case ResponseCommand.EGetterType.E_Get:
                                // LJUD.eGet(deviceHandle, LJUD.IO.GET_TIMER, 0, ref dblValue, 0);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Write(ex.Message);
                        this.OutCommandsAck.Post(false, DateTime.UtcNow);
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Generic method for creating a LabJack device instance (currently not used).
        /// </summary>
        /// <typeparam name="Type">The type of LabJack device to create.</typeparam>
        private void CreateDevice<Type>()
            where Type : new()
        {
            // Type device = new Type(configuration.ConnnectionType, configuration.DeviceAdress, configuration.FirstDeviceFound);
            // Device = device;
            // deviceHandle = device.ljhandle;
        }
    }
}
