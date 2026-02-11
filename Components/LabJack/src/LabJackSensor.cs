// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.LabJackComponent
{
    using Microsoft.Psi;

    /// <summary>
    /// LabJack sensor component for controlling and reading data from LabJack devices.
    /// Provides command input, acknowledgement output, and data value output.
    /// See LabJackCoreConfiguration class for configuration details.
    /// </summary>
    public class LabJackSensor : Subpipeline
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabJackSensor"/> class.
        /// </summary>
        /// <param name="pipeline">The parent pipeline.</param>
        /// <param name="config">Optional configuration for the LabJack device.</param>
        /// <param name="name">The name of the component.</param>
        /// <param name="defaultDeliveryPolicy">Optional default delivery policy.</param>
        public LabJackSensor(Pipeline pipeline, LabJackCoreConfiguration? config = null, string name = nameof(LabJackSensor), DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(pipeline, name, defaultDeliveryPolicy ?? DeliveryPolicy.LatestMessage)
        {
            this.Configuration = config ?? new LabJackCoreConfiguration();

            var labJackCore = new LabJackCore(this, this.Configuration);

            this.OutCommandsAck = labJackCore.OutCommandsAck.BridgeTo(pipeline, $"{name}-OutCommandsAck").Out;
            this.OutDoubleValue = labJackCore.OutDoubleValue.BridgeTo(pipeline, $"{name}-OutDoubleValue").Out;
            this.InCommandsReceiver = labJackCore.InCommandsReceiver;
        }

        /// <summary>
        /// Gets the configuration for the LabJack device.
        /// </summary>
        public LabJackCoreConfiguration? Configuration { get; }

        /// <summary>
        /// Gets the receiver for incoming commands.
        /// </summary>
        public Receiver<Commands> InCommandsReceiver { get; private set; }

        /// <summary>
        /// Gets the emitter for command acknowledgements.
        /// </summary>
        public Emitter<bool> OutCommandsAck { get; private set; }

        /// <summary>
        /// Gets the emitter for double value outputs.
        /// </summary>
        public Emitter<double> OutDoubleValue { get; private set; }
    }
}
