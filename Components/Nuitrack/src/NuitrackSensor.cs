// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Nuitrack
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using nuitrack;
    using DepthImage = Microsoft.Psi.Imaging.DepthImage;
    using Image = Microsoft.Psi.Imaging.Image;

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

        private readonly NuitrackSensorConfiguration configuration;
        private readonly NuitrackCore core;
        private readonly string name;
        private readonly Pipeline pipeline;
        private long colorTimestamp = 0;
        private long depthTimestamp = 0;
        private long skeletonTimestamp = 0;
        private long handTimestamp = 0;
        private long userTimestamp = 0;
        private long gestureTimestamp = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="NuitrackSensor"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="config">The configuration for the Nuitrack sensor.</param>
        /// <param name="name">The name of the component.</param>
        public NuitrackSensor(Pipeline pipeline, NuitrackSensorConfiguration config, string name = nameof(NuitrackSensor))
        {
            this.name = name;
            this.configuration = config;
            this.pipeline = pipeline;
            this.core = NuitrackCore.GetNuitrackCore();
            this.core.RegisterSensor(config, this);

            this.OutDepthImage = pipeline.CreateEmitter<Shared<DepthImage>>(this, $"{name}-OutDepthImage");
            this.OutColorImage = pipeline.CreateEmitter<Shared<Image>>(this, $"{name}-OutColorImage");
            this.OutBodies = pipeline.CreateEmitter<List<Skeleton>>(this, $"{name}-OutBodies");
            this.OutHands = pipeline.CreateEmitter<List<UserHands>>(this, $"{name}-OutHands");
            this.OutUsers = pipeline.CreateEmitter<List<User>>(this, $"{name}-OutUsers");
            this.OutGestures = pipeline.CreateEmitter<List<UserGesturesState>>(this, $"{name}-OutGestures");
            this.OutFrameRate = pipeline.CreateEmitter<double>(this, $"{name}-OutFrameRate");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Starts the Nuitrack sensor capture.
        /// </summary>
        /// <param name="notifyCompletionTime">Delegate to notify completion time.</param>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            if (this.core.Start(notifyCompletionTime) == false)
            {
                notifyCompletionTime(DateTime.MaxValue);
            }
        }

        /// <summary>
        /// Stops the Nuitrack sensor capture.
        /// </summary>
        /// <param name="finalOriginatingTime">The final originating time.</param>
        /// <param name="notifyCompleted">Delegate to notify completion.</param>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            if (this.core.Stop(finalOriginatingTime, notifyCompleted) == false)
            {
                notifyCompleted();
            }
        }

        /// <summary>
        /// Handles depth sensor frame updates.
        /// </summary>
        /// <param name="depthFrame">The depth frame data.</param>
        internal void OnDepthSensorUpdate(DepthFrame depthFrame)
        {
            if (depthFrame != null && this.depthTimestamp != (long)depthFrame.Timestamp)
            {
                Shared<DepthImage> image = Microsoft.Psi.Imaging.DepthImagePool.GetOrCreate(depthFrame.Cols, depthFrame.Rows);
                this.depthTimestamp = (long)depthFrame.Timestamp;
                image.Resource.CopyFrom(depthFrame.Data);
                this.OutDepthImage.Post(image, this.pipeline.GetCurrentTime());
                depthFrame.Dispose();
            }
        }

        /// <summary>
        /// Handles color sensor frame updates.
        /// </summary>
        /// <param name="colorFrame">The color frame data.</param>
        internal void OnColorSensorUpdate(ColorFrame colorFrame)
        {
            if (colorFrame != null && this.colorTimestamp != (long)colorFrame.Timestamp)
            {
                Shared<Image> image = Microsoft.Psi.Imaging.ImagePool.GetOrCreate(colorFrame.Cols, colorFrame.Rows, Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp);
                this.colorTimestamp = (long)colorFrame.Timestamp;
                image.Resource.CopyFrom(colorFrame.Data);
                this.OutColorImage.Post(image, this.pipeline.GetCurrentTime());
                colorFrame.Dispose();
            }
        }

        /// <summary>
        /// Handles skeleton tracking updates.
        /// </summary>
        /// <param name="skeletonData">The skeleton tracking data.</param>
        internal void OnSkeletonUpdate(SkeletonData skeletonData)
        {
            if (skeletonData != null && skeletonData.NumUsers > 0 && this.skeletonTimestamp != (long)skeletonData.Timestamp)
            {
                List<Skeleton> output = new List<Skeleton>();
                foreach (Skeleton body in skeletonData.Skeletons)
                {
                    output.Add(body);
                }

                this.skeletonTimestamp = (long)skeletonData.Timestamp;
                this.OutBodies.Post(output, this.pipeline.GetCurrentTime());
                skeletonData.Dispose();
            }
        }

        /// <summary>
        /// Handles hand tracking updates.
        /// </summary>
        /// <param name="handData">The hand tracking data.</param>
        internal void OnHandUpdate(HandTrackerData handData)
        {
            if (handData != null && handData.NumUsers > 1 && this.handTimestamp != (long)handData.Timestamp)
            {
                List<UserHands> output = new List<UserHands>();
                foreach (UserHands hand in handData.UsersHands)
                {
                    output.Add(hand);
                }

                this.handTimestamp = (long)handData.Timestamp;
                this.OutHands.Post(output, this.pipeline.GetCurrentTime());
                handData.Dispose();
            }
        }

        /// <summary>
        /// Handles user tracking updates.
        /// </summary>
        /// <param name="userFrame">The user frame data.</param>
        internal void OnUserUpdate(UserFrame userFrame)
        {
            if (userFrame != null && userFrame.NumUsers > 0 && this.userTimestamp != (long)userFrame.Timestamp)
            {
                List<User> output = new List<User>();
                foreach (User user in userFrame.Users)
                {
                    output.Add(user);
                }

                this.userTimestamp = (long)userFrame.Timestamp;
                this.OutUsers.Post(output, this.pipeline.GetCurrentTime());
                userFrame.Dispose();
            }
        }

        /// <summary>
        /// Handles gesture recognition updates.
        /// </summary>
        /// <param name="gestureData">The gesture recognition data.</param>
        internal void OnGestureUpdate(UserGesturesStateData gestureData)
        {
            if (gestureData != null && gestureData.NumUsersGesturesStates > 0 && this.gestureTimestamp != (long)gestureData.Timestamp)
            {
                List<UserGesturesState> output = new List<UserGesturesState>();
                foreach (UserGesturesState gesture in gestureData.UserGesturesStates)
                {
                    output.Add(gesture);
                }

                this.gestureTimestamp = (long)gestureData.Timestamp;
                this.OutGestures.Post(output, this.pipeline.GetCurrentTime());
                gestureData.Dispose();
            }
        }

        /// <summary>
        /// Converts 3D world coordinates to 2D projection coordinates.
        /// </summary>
        /// <param name="position">The 3D position in world coordinates.</param>
        /// <returns>The 2D projection coordinates.</returns>
        public MathNet.Spatial.Euclidean.Point2D GetProjCoordinates(MathNet.Spatial.Euclidean.Vector3D position)
        {
            Vector3 point = new Vector3((float)position.X, (float)position.Y, (float)position.Z);
            Vector3 proj = this.core.ToProj(point, this.configuration.DeviceSerialNumber);
            return new MathNet.Spatial.Euclidean.Point2D(proj.X, proj.Y);
        }
    }
}
