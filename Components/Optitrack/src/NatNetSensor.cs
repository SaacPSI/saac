using Microsoft.Psi;

namespace NatNetComponent
{
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
        //public Emitter<Shared<Image>> ColorImage { get; private set; }

        /// <summary>
        /// Gets the current depth image.
        /// </summary>
      //  public Emitter<Shared<DepthImage>> DepthImage { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked bodies.
        /// </summary>
        public Emitter<List<RigidBody>> OutRigidBodies { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked hands.
        /// </summary>
        //public Emitter<List<UserHands>> Hands { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked users.
        /// </summary>
        //public Emitter<List<User>> Users { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked users.
        /// </summary>
        //public Emitter<List<UserGesturesState>> Gestures { get; private set; }

        /// <summary>
        /// Gets the current frames-per-second actually achieved.
        /// </summary>
        //public Emitter<double> FrameRate { get; private set; }
        // TODO: Add more if needed and renaming properly the reciever & emitter

        /* End in/out puts */
        // Constructor
        public NatNetSensor(Pipeline pipeline, NatNetCoreConfiguration? config = null, DeliveryPolicy? defaultDeliveryPolicy = null, DeliveryPolicy? bodyTrackerDeliveryPolicy = null)
         : base(pipeline, nameof(NatNetSensor), defaultDeliveryPolicy ?? DeliveryPolicy.LatestMessage)
        {

            this.Configuration = config ?? new NatNetCoreConfiguration();

            var NatNetCore = new NatNetCore(this, this.Configuration);

            //this.ColorImage = NatNetCore.ColorImage.BridgeTo(pipeline, nameof(this.ColorImage)).Out;
            //this.DepthImage = NatNetCore.DepthImage.BridgeTo(pipeline, nameof(this.DepthImage)).Out;
            //this.Bodies = NatNetCore.Bodies.BridgeTo(pipeline, nameof(this.Bodies)).Out;
            this.OutRigidBodies = NatNetCore.OutRigidBodies.BridgeTo(pipeline, nameof(this.OutRigidBodies)).Out;
            //this.Users = NatNetCore.Users.BridgeTo(pipeline, nameof(this.Users)).Out;
            //this.Gestures = NatNetCore.Gestures.BridgeTo(pipeline, nameof(this.Gestures)).Out;
            //this.FrameRate = NatNetCore.FrameRate.BridgeTo(pipeline, nameof(this.FrameRate)).Out;
        }

        public static IEnumerable<string> ConnectionTypes
        {
            get
            {
                if (connectionTypes == null)
                {
                    connectionTypes = new List<string>();
                    connectionTypes.Add(NatNetML.ConnectionType.Multicast.ToString());
                    connectionTypes.Add(NatNetML.ConnectionType.Unicast.ToString());
                }
                return connectionTypes;
            }
        }
    }
}
