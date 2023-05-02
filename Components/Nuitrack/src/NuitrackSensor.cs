using nuitrack;
using Microsoft.Psi;
using DepthImage = Microsoft.Psi.Imaging.DepthImage;
using Image = Microsoft.Psi.Imaging.Image;
using Microsoft.Psi.Components;

namespace Nuitrack
{
    public class NuitrackSensor : ISourceComponent
    {

        /* Begin in/out puts*/

        /// <summary>
        /// Gets the current image from the color camera.
        /// </summary>
        public Emitter<Shared<Image>> ColorImage { get; private set; }

        /// <summary>
        /// Gets the current depth image.
        /// </summary>
        public Emitter<Shared<DepthImage>> DepthImage { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked bodies.
        /// </summary>
        public Emitter<List<Skeleton>> Bodies { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked hands.
        /// </summary>
        public Emitter<List<UserHands>> Hands { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked users.
        /// </summary>
        public Emitter<List<User>> Users { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked gestures.
        /// </summary>
        public Emitter<List<UserGesturesState>> Gestures { get; private set; }

        /// <summary>
        /// Gets the current frames-per-second actually achieved.
        /// </summary>
        public Emitter<double> FrameRate { get; private set; }
        // TODO: Add more if needed and renaming properly the reciever & emitter

        /* End in/out puts */
        // Constructor
        private NuitrackSensorConfiguration Configuration;
        private NuitrackCore Core;
        protected Pipeline Pipeline;
        private long ColorTimestamp = 0;
        private long DepthTimestamp = 0;
        private long SkeletonTimestamp = 0;
        private long HandTimestamp = 0;
        private long UserTimestamp = 0;
        private long GestureTimestamp = 0;

        public NuitrackSensor(Pipeline pipeline, NuitrackSensorConfiguration config, DeliveryPolicy? defaultDeliveryPolicy = null, DeliveryPolicy? bodyTrackerDeliveryPolicy = null)
        {
            Configuration = config;
            Pipeline = pipeline;
            Core = NuitrackCore.GetNuitrackCore();
            Core.RegisterSensor(config, this);

            DepthImage = pipeline.CreateEmitter<Shared<DepthImage>>(this, nameof(DepthImage));
            ColorImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(ColorImage));
            Bodies = pipeline.CreateEmitter<List<Skeleton>>(this, nameof(Bodies));
            Hands = pipeline.CreateEmitter<List<UserHands>>(this, nameof(Hands));
            Users = pipeline.CreateEmitter<List<User>>(this, nameof(Users));
            Gestures = pipeline.CreateEmitter<List<UserGesturesState>>(this, nameof(Gestures));
            FrameRate = pipeline.CreateEmitter<double>(this, nameof(FrameRate));
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            if(Core.Start(notifyCompletionTime) == false)
                notifyCompletionTime(DateTime.MaxValue);
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
           if(Core.Stop(finalOriginatingTime, notifyCompleted) == false)
                notifyCompleted();
        }

        internal void onDepthSensorUpdate(DepthFrame depthFrame)
        {
            if (depthFrame != null && DepthTimestamp != (long)depthFrame.Timestamp)
            {
                Shared<DepthImage> image = Microsoft.Psi.Imaging.DepthImagePool.GetOrCreate(depthFrame.Cols, depthFrame.Rows);
                DepthTimestamp = (long)depthFrame.Timestamp;
                image.Resource.CopyFrom(depthFrame.Data);
                DepthImage.Post(image, Pipeline.GetCurrentTime());
                depthFrame.Dispose();
            }
        }

        internal void onColorSensorUpdate(ColorFrame colorFrame)
        {
            if (colorFrame != null && ColorTimestamp != (long)colorFrame.Timestamp)
            {
                Shared<Image> image = Microsoft.Psi.Imaging.ImagePool.GetOrCreate(colorFrame.Cols, colorFrame.Rows, Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp);
                ColorTimestamp = (long)colorFrame.Timestamp;
                image.Resource.CopyFrom(colorFrame.Data);
                ColorImage.Post(image, Pipeline.GetCurrentTime());
                colorFrame.Dispose();
            }
        }

        internal void onSkeletonUpdate(SkeletonData skeletonData)
        {
            if (skeletonData != null && skeletonData.NumUsers > 0 && SkeletonTimestamp != (long)skeletonData.Timestamp)
            {
                List<Skeleton> output = new List<Skeleton>();
                foreach (Skeleton body in skeletonData.Skeletons)
                    output.Add(body);
                SkeletonTimestamp = (long)skeletonData.Timestamp;
                Bodies.Post(output, Pipeline.GetCurrentTime());
                skeletonData.Dispose();
            }
        }

        internal void onHandUpdate(HandTrackerData handData)
        {
            if (handData != null && handData.NumUsers > 1 && HandTimestamp != (long)handData.Timestamp)
            {
                List<UserHands> output = new List<UserHands>();
                foreach (UserHands hand in handData.UsersHands)
                    output.Add(hand);
                HandTimestamp = (long)handData.Timestamp;
                Hands.Post(output, Pipeline.GetCurrentTime());
                handData.Dispose();
            }
        }

        internal void onUserUpdate(UserFrame userFrame)
        {
            if (userFrame != null && userFrame.NumUsers > 0 && UserTimestamp != (long)userFrame.Timestamp)
            {
                List<User> output = new List<User>();
                foreach (User user in userFrame.Users)
                    output.Add(user);
                UserTimestamp = (long)userFrame.Timestamp;
                Users.Post(output, Pipeline.GetCurrentTime());
                userFrame.Dispose();
            }
        }

        internal void onGestureUpdate(UserGesturesStateData gestureData)
        {
            if (gestureData != null && gestureData.NumUsersGesturesStates > 0 && GestureTimestamp != (long)gestureData.Timestamp)
            {
                List<UserGesturesState> output = new List<UserGesturesState>();
                foreach (UserGesturesState gesture in gestureData.UserGesturesStates)
                    output.Add(gesture);
                GestureTimestamp = (long)gestureData.Timestamp;
                Gestures.Post(output, Pipeline.GetCurrentTime());
                gestureData.Dispose();
            }
        }

        public MathNet.Spatial.Euclidean.Point2D getProjCoordinates(MathNet.Spatial.Euclidean.Vector3D position)
        {
            Vector3 point = new Vector3((float)position.X, (float)position.Y, (float)position.Z);
            Vector3 proj = Core.toProj(point, Configuration.DeviceSerialNumber);
            return new MathNet.Spatial.Euclidean.Point2D(proj.X, proj.Y);
        }
    }
}
