using nuitrack;
using Microsoft.Psi;
using DepthImage = Microsoft.Psi.Imaging.DepthImage;
using Image = Microsoft.Psi.Imaging.Image;
using Microsoft.Psi.Components;

namespace SAAC.Nuitrack
{
    /// <summary>
    /// NuitrackSensor component class, handle camera with nuitrack API.
    /// See NuitrackSensorConfiguration class for details.
    /// </summary>
    public class NuitrackSensor : ISourceComponent
    {

        /* Begin in/out puts*/

        /// <summary>
        /// Gets the current image from the color camera.
        /// </summary>
        public Emitter<Shared<Image>> OutColorImage { get; private set; }

        /// <summary>
        /// Gets the current depth image.
        /// </summary>
        public Emitter<Shared<DepthImage>> OutDepthImage { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked bodies.
        /// </summary>
        public Emitter<List<Skeleton>> OutBodies { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked hands.
        /// </summary>
        public Emitter<List<UserHands>> OutHands { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked users.
        /// </summary>
        public Emitter<List<User>> OutUsers { get; private set; }

        /// <summary>
        /// Gets the emitter of lists of currently tracked gestures.
        /// </summary>
        public Emitter<List<UserGesturesState>> OutGestures { get; private set; }

        /// <summary>
        /// Gets the current frames-per-second actually achieved.
        /// </summary>
        public Emitter<double> OutFrameRate { get; private set; }
        // TODO: Add more if needed and renaming properly the reciever & emitter

        /* End in/out puts */
        // Constructor
        private NuitrackSensorConfiguration configuration;
        private NuitrackCore core;
        protected Pipeline pipeline;
        private long colorTimestamp = 0;
        private long depthTimestamp = 0;
        private long skeletonTimestamp = 0;
        private long handTimestamp = 0;
        private long userTimestamp = 0;
        private long gestureTimestamp = 0;
        private string name;

        public NuitrackSensor(Pipeline pipeline, NuitrackSensorConfiguration config, string name = nameof(NuitrackSensor))
        {
            this.name = name;
            configuration = config;
            this.pipeline = pipeline;
            core = NuitrackCore.GetNuitrackCore();
            core.RegisterSensor(config, this);

            OutDepthImage = pipeline.CreateEmitter<Shared<DepthImage>>(this, $"{name}-OutDepthImage");
            OutColorImage = pipeline.CreateEmitter<Shared<Image>>(this, $"{name}-OutColorImage");
            OutBodies = pipeline.CreateEmitter<List<Skeleton>>(this, $"{name}-OutBodies");
            OutHands = pipeline.CreateEmitter<List<UserHands>>(this, $"{name}-OutHands");
            OutUsers = pipeline.CreateEmitter<List<User>>(this, $"{name}-OutUsers");
            OutGestures = pipeline.CreateEmitter<List<UserGesturesState>>(this, $"{name}-OutGestures");
            OutFrameRate = pipeline.CreateEmitter<double>(this, $"{name}-OutFrameRate");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            if(core.Start(notifyCompletionTime) == false)
                notifyCompletionTime(DateTime.MaxValue);
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
           if(core.Stop(finalOriginatingTime, notifyCompleted) == false)
                notifyCompleted();
        }

        internal void onDepthSensorUpdate(DepthFrame depthFrame)
        {
            if (depthFrame != null && depthTimestamp != (long)depthFrame.Timestamp)
            {
                Shared<DepthImage> image = Microsoft.Psi.Imaging.DepthImagePool.GetOrCreate(depthFrame.Cols, depthFrame.Rows);
                depthTimestamp = (long)depthFrame.Timestamp;
                image.Resource.CopyFrom(depthFrame.Data);
                OutDepthImage.Post(image, pipeline.GetCurrentTime());
                depthFrame.Dispose();
            }
        }

        internal void onColorSensorUpdate(ColorFrame colorFrame)
        {
            if (colorFrame != null && colorTimestamp != (long)colorFrame.Timestamp)
            {
                Shared<Image> image = Microsoft.Psi.Imaging.ImagePool.GetOrCreate(colorFrame.Cols, colorFrame.Rows, Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp);
                colorTimestamp = (long)colorFrame.Timestamp;
                image.Resource.CopyFrom(colorFrame.Data);
                OutColorImage.Post(image, pipeline.GetCurrentTime());
                colorFrame.Dispose();
            }
        }

        internal void onSkeletonUpdate(SkeletonData skeletonData)
        {
            if (skeletonData != null && skeletonData.NumUsers > 0 && skeletonTimestamp != (long)skeletonData.Timestamp)
            {
                List<Skeleton> output = new List<Skeleton>();
                foreach (Skeleton body in skeletonData.Skeletons)
                    output.Add(body);
                skeletonTimestamp = (long)skeletonData.Timestamp;
                OutBodies.Post(output, pipeline.GetCurrentTime());
                skeletonData.Dispose();
            }
        }

        internal void onHandUpdate(HandTrackerData handData)
        {
            if (handData != null && handData.NumUsers > 1 && handTimestamp != (long)handData.Timestamp)
            {
                List<UserHands> output = new List<UserHands>();
                foreach (UserHands hand in handData.UsersHands)
                    output.Add(hand);
                handTimestamp = (long)handData.Timestamp;
                OutHands.Post(output, pipeline.GetCurrentTime());
                handData.Dispose();
            }
        }

        internal void onUserUpdate(UserFrame userFrame)
        {
            if (userFrame != null && userFrame.NumUsers > 0 && userTimestamp != (long)userFrame.Timestamp)
            {
                List<User> output = new List<User>();
                foreach (User user in userFrame.Users)
                    output.Add(user);
                userTimestamp = (long)userFrame.Timestamp;
                OutUsers.Post(output, pipeline.GetCurrentTime());
                userFrame.Dispose();
            }
        }

        internal void onGestureUpdate(UserGesturesStateData gestureData)
        {
            if (gestureData != null && gestureData.NumUsersGesturesStates > 0 && gestureTimestamp != (long)gestureData.Timestamp)
            {
                List<UserGesturesState> output = new List<UserGesturesState>();
                foreach (UserGesturesState gesture in gestureData.UserGesturesStates)
                    output.Add(gesture);
                gestureTimestamp = (long)gestureData.Timestamp;
                OutGestures.Post(output, pipeline.GetCurrentTime());
                gestureData.Dispose();
            }
        }

        public MathNet.Spatial.Euclidean.Point2D getProjCoordinates(MathNet.Spatial.Euclidean.Vector3D position)
        {
            Vector3 point = new Vector3((float)position.X, (float)position.Y, (float)position.Z);
            Vector3 proj = core.toProj(point, configuration.DeviceSerialNumber);
            return new MathNet.Spatial.Euclidean.Point2D(proj.X, proj.Y);
        }
    }
}
