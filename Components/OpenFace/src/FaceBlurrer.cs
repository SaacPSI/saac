// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.Drawing;
using System.Numerics;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using SAAC.Helpers;

namespace SAAC.OpenFace
{
    /// <summary>
    /// Component that blurs faces in images based on pose data or bounding boxes.
    /// </summary>
    public class FaceBlurrer : IProducer<Shared<Microsoft.Psi.Imaging.Image>>
    {
        /// <summary>
        /// Gets. Connector that encapsulates the shared image input stream.
        /// </summary>
        public Connector<Shared<Microsoft.Psi.Imaging.Image>> InImageConnector { get; private set; }

        /// <summary>
        /// Gets. Receiver that encapsulates the shared image input stream.
        /// </summary>
        public Receiver<Shared<Microsoft.Psi.Imaging.Image>> InImage => this.InImageConnector.In;

        /// <summary>
        /// Gets. Connector that encapsulates the pose data input stream.
        /// </summary>
        public Connector<Pose> InPoseConnector { get; private set; }

        /// <summary>
        /// Gets. Receiver that encapsulates the pose data input stream.
        /// </summary>
        public Receiver<Pose> InPose => this.InPoseConnector.In;

        /// <summary>
        /// Gets. Connector that encapsulates the pose data input stream.
        /// </summary>
        public Connector<List<Rectangle>> InBBoxesConnector { get; private set; }

        /// <summary>
        /// Gets. Receiver that encapsulates the pose data input stream.
        /// </summary>
        public Receiver<List<Rectangle>> InBBoxes => this.InBBoxesConnector.In;

        /// <summary>
        /// Gets. Emitter that encapsulates the image data output stream.
        /// </summary>
        public Emitter<Shared<Microsoft.Psi.Imaging.Image>> Out { get; private set; }

        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="FaceBlurrer"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public FaceBlurrer(Pipeline parent, string name = nameof(FaceBlurrer))
        {
            this.name = name;
            this.InImageConnector = parent.CreateConnector<Shared<Microsoft.Psi.Imaging.Image>>($"{name}-InImage");
            this.InPoseConnector = parent.CreateConnector<Pose>($"{name}-InPose");
            this.InBBoxesConnector = parent.CreateConnector<List<Rectangle>>($"{name}-InBBoxe");

            this.Out = parent.CreateEmitter<Shared<Microsoft.Psi.Imaging.Image>>(this, $"{name}-Out");

            this.InPoseConnector.Pair(this.InImageConnector, DeliveryPolicy.LatestMessage).Do(this.Process);
            this.InImageConnector.Fuse(this.InBBoxesConnector, Reproducible.Exact<List<Rectangle>>(), DeliveryPolicy.Throttle).Do(this.Process);
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Processes an image with pose data to blur the face region.
        /// </summary>
        /// <param name="data">Tuple containing pose data and the image to process.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process((Pose, Shared<Microsoft.Psi.Imaging.Image>) data, Envelope envelope)
        {
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            this.SearchForMinMax(data.Item1.Landmarks, out min, out max);
            Rectangle rect = default(Rectangle);
            var s = (max - min) * 1.2f;
            rect.Size = new Size((int)s.X, (int)s.Y);
            rect.X = (int)min.X;
            rect.Y = (int)min.Y;

            data.Item2.Resource.FillRectangle(rect, Color.Black);
            Shared<Microsoft.Psi.Imaging.Image> image = ImagePool.GetOrCreate(data.Item2.Resource.Width, data.Item2.Resource.Height, data.Item2.Resource.PixelFormat);
            image.Resource.FillRectangle(rect, Color.Black);
            this.Out.Post(image, envelope.OriginatingTime);
        }

        /// <summary>
        /// Processes an image with bounding boxes to blur face regions.
        /// </summary>
        /// <param name="data">Tuple containing the image and list of bounding boxes.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process((Shared<Microsoft.Psi.Imaging.Image>, List<Rectangle>) data, Envelope envelope)
        {
            Shared<Microsoft.Psi.Imaging.Image> src = data.Item1;
            List<Rectangle> boxes = data.Item2;
            Shared<Microsoft.Psi.Imaging.Image> image = ImagePool.GetOrCreate(src.Resource.Width, src.Resource.Height, src.Resource.PixelFormat);
            image.Resource.CopyFrom(src.Resource);
            foreach (Rectangle rectangle in boxes)
            {
                rectangle.Inflate(new Size((int)(rectangle.Size.Width * 0.25f), (int)(rectangle.Size.Height * 0.25f)));
                image.Resource.FillRectangle(rectangle, Color.Black);
            }

            this.Out.Post(image, envelope.OriginatingTime);
        }

        /// <summary>
        /// Searches for the minimum and maximum coordinates in a collection of landmarks.
        /// </summary>
        /// <param name="landmarks">The collection of landmark points.</param>
        /// <param name="min">The minimum coordinates found.</param>
        /// <param name="max">The maximum coordinates found.</param>
        private void SearchForMinMax(IReadOnlyCollection<Vector2> landmarks, out Vector2 min, out Vector2 max)
        {
            float minX, minY, maxX, maxY;
            minX = minY = float.MaxValue;
            maxX = maxY = float.MinValue;
            foreach (Vector2 landmark in landmarks)
            {
                if (minX > landmark.X)
                {
                    minX = landmark.X;
                }
                else if (maxX < landmark.X)
                {
                    maxX = landmark.X;
                }

                if (minY > landmark.Y)
                {
                    minY = landmark.Y;
                }
                else if (maxY < landmark.Y)
                {
                    maxY = landmark.Y;
                }
            }

            min = new Vector2(minX, minY);
            max = new Vector2(maxX, maxY);
        }
    }
}
