using Microsoft.Psi;

namespace LabJackComponent
{    
     /// <summary>
     /// LabJackComponent communicator component class, in input the command, one emitter for acknowledge the command and an output for values.
     /// See LabJackCoreConfiguration class for details.
     /// </summary>
    public class LabJackSensor : Subpipeline
    {
        public LabJackCoreConfiguration? Configuration { get; } = null;
        public Receiver<Commands> InCommandsReceiver { get; private set; }

        public Emitter<bool> OutCommandsAck { get; private set; }
        public Emitter<double> OutDoubleValue { get; private set; }

        public LabJackSensor(Pipeline pipeline, LabJackCoreConfiguration? config = null, DeliveryPolicy? defaultDeliveryPolicy = null, DeliveryPolicy? bodyTrackerDeliveryPolicy = null)
     : base(pipeline, nameof(LabJackSensor), defaultDeliveryPolicy ?? DeliveryPolicy.LatestMessage)
        {

            this.Configuration = config ?? new LabJackCoreConfiguration();

            var LabJackCore = new LabJackCore(this, this.Configuration);

            OutCommandsAck = LabJackCore.OutCommandsAck.BridgeTo(pipeline, nameof(OutCommandsAck)).Out;
            OutDoubleValue = LabJackCore.OutDoubleValue.BridgeTo(pipeline, nameof(OutDoubleValue)).Out;
            InCommandsReceiver = LabJackCore.InCommandsReceiver;
        }
    }
}
