using Microsoft.Psi;

namespace LabJackComponent
{
    public class LabJackSensor : Subpipeline
    {
        public LabJackCoreConfiguration? Configuration { get; } = null;

        /// <summary>
        /// Gets the emitter of lists of currently tracked bodies.
        /// </summary>
        public Receiver<Commands> InCommandsReceiver { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked bodies.
        /// </summary>
        public Emitter<bool> OutCommandsAck { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked bodies.
        /// </summary>
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
