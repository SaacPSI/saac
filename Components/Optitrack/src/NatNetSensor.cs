// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;

namespace SAAC.NatNetComponent
{
    /// <summary>
    /// Optitrack component class using NatNet API.
    /// See NatNetCoreConfiguration class for details.
    /// </summary>
    public class NatNetSensor : Subpipeline
    {
        /// <summary>
        /// Gets the sensor configuration.
        /// </summary>
        public NatNetCoreConfiguration? Configuration { get; } = null;

        private static List<string>? connectionTypes = null;

        /* Begin in/out puts*/

        /// <summary>
        /// Gets the current image from the color camera.
        /// </summary>
        // public Emitter<Shared<Image>> ColorImage { get; private set; }

        /// <summary>
        /// Gets the current depth image.
        /// </summary>
      // public Emitter<Shared<DepthImage>> DepthImage { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked bodies.
        /// </summary>
        public Emitter<List<RigidBody>> OutRigidBodies { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NatNetSensor"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="config">The configuration to use for the sensor.</param>
        /// <param name="name">The name of the component.</param>
        /// <param name="defaultDeliveryPolicy">The default delivery policy.</param>
        /// <param name="bodyTrackerDeliveryPolicy">The body tracker delivery policy.</param>
        public NatNetSensor(Pipeline pipeline, NatNetCoreConfiguration? config = null, string name = nameof(NatNetSensor), DeliveryPolicy? defaultDeliveryPolicy = null, DeliveryPolicy? bodyTrackerDeliveryPolicy = null)
         : base(pipeline, name, defaultDeliveryPolicy ?? DeliveryPolicy.LatestMessage)
        {
            this.Configuration = config ?? new NatNetCoreConfiguration();

            var natNetCore = new NatNetCore(this, this.Configuration);

            // this.ColorImage = NatNetCore.ColorImage.BridgeTo(pipeline, nameof(this.ColorImage)).Out;
            // this.DepthImage = NatNetCore.DepthImage.BridgeTo(pipeline, nameof(this.DepthImage)).Out;
            // this.Bodies = NatNetCore.Bodies.BridgeTo(pipeline, nameof(this.Bodies)).Out;
            this.OutRigidBodies = natNetCore.OutRigidBodies.BridgeTo(pipeline, $"{name}-OutRigidBodies").Out;

            // this.Users = NatNetCore.Users.BridgeTo(pipeline, nameof(this.Users)).Out;
            // this.Gestures = NatNetCore.Gestures.BridgeTo(pipeline, nameof(this.Gestures)).Out;
            // this.FrameRate = NatNetCore.FrameRate.BridgeTo(pipeline, nameof(this.FrameRate)).Out;
        }

        /// <summary>
        /// Gets the available NatNet connection types.
        /// </summary>
        public static IEnumerable<string> ConnectionTypes
        {
            get
            {
                if (connectionTypes == null)
                {
                    connectionTypes = new List<string>
                    {
                        NatNetML.ConnectionType.Multicast.ToString(),
                        NatNetML.ConnectionType.Unicast.ToString()
                    };
                }

                return connectionTypes;
            }
        }
    }
}
