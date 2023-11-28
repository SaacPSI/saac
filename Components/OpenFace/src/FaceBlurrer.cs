using SAAC.Helpers;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using System.Drawing;
using System.Numerics;

namespace SAAC.OpenFace
{
    public class FaceBlurrer : Subpipeline
    {
        /// <summary>
        /// Gets. Connector that encapsulates the shared image input stream.
        /// </summary>
        public Connector<Shared<Microsoft.Psi.Imaging.Image>> InImageConnector { get; private set; }

        /// <summary>
        /// Gets. Receiver that encapsulates the shared image input stream.
        /// </summary>
        public Receiver<Shared<Microsoft.Psi.Imaging.Image>> InImage => InImageConnector.In;

        /// <summary>
        /// Gets. Connector that encapsulates the pose data input stream.
        /// </summary>
        public Connector<Pose> InPoseConnector { get; private set; }

        /// <summary>
        /// Gets. Receiver that encapsulates the pose data input stream.
        /// </summary>
        public Receiver<Pose> InPose => InPoseConnector.In;

        /// <summary>
        /// Gets. Connector that encapsulates the pose data input stream.
        /// </summary>
        public Connector<List<Rectangle>> InBBoxesConnector { get; private set; }

        /// <summary>
        /// Gets. Receiver that encapsulates the pose data input stream.
        /// </summary>
        public Receiver<List<Rectangle>> InBBoxes => InBBoxesConnector.In;


        /// <summary>
        /// Gets. Emitter that encapsulates the image data output stream.
        /// </summary>
        public Emitter<Shared<Microsoft.Psi.Imaging.Image>> Out { get; private set; }

        public FaceBlurrer(Pipeline parent, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null) : base(parent, name, defaultDeliveryPolicy)
        {
            InImageConnector = parent.CreateConnector<Shared<Microsoft.Psi.Imaging.Image>>(nameof(InImage));
            InPoseConnector = parent.CreateConnector<Pose>(nameof(InPose));
            InBBoxesConnector = parent.CreateConnector<List<Rectangle>>(nameof(InBBoxesConnector));

            Out = parent.CreateEmitter<Shared<Microsoft.Psi.Imaging.Image>>(parent, nameof(Out));

            InPoseConnector.Pair(InImageConnector, DeliveryPolicy.LatestMessage).Do(Process);
            InImageConnector.Fuse(InBBoxesConnector, Reproducible.Exact<List<Rectangle>>(), DeliveryPolicy.Throttle).Do(Process);
        }

        private void Process((Pose, Shared<Microsoft.Psi.Imaging.Image>) data, Envelope envelope)
        {
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            SearchForMinMax(data.Item1.Landmarks, out min, out max);
            Rectangle rect = new Rectangle();
            var s = (max - min)*1.2f;
            rect.Size = new Size((int)s.X, (int)s.Y);
            rect.X = (int)min.X;
            rect.Y = (int)min.Y;

            data.Item2.Resource.FillRectangle(rect, Color.Black);
            Shared<Microsoft.Psi.Imaging.Image> image = ImagePool.GetOrCreate(data.Item2.Resource.Width, data.Item2.Resource.Height, data.Item2.Resource.PixelFormat);
            image.Resource.FillRectangle(rect, Color.Black);
            Out.Post(image, envelope.OriginatingTime);
        }

        private void Process((Shared<Microsoft.Psi.Imaging.Image>, List<Rectangle>) data, Envelope envelope)
        {
            Shared<Microsoft.Psi.Imaging.Image> src = data.Item1;
            List<Rectangle> boxes = data.Item2;
            Shared<Microsoft.Psi.Imaging.Image> image = ImagePool.GetOrCreate(src.Resource.Width, src.Resource.Height, src.Resource.PixelFormat);
            image.Resource.CopyFrom(src.Resource); 
            foreach(Rectangle rectangle in boxes)
            {
                rectangle.Inflate(new Size((int)(rectangle.Size.Width * 0.25f), (int)(rectangle.Size.Height * 0.25f)));
                image.Resource.FillRectangle(rectangle, Color.Black);
            }
            Out.Post(image, envelope.OriginatingTime);
        }

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
                else if(maxX < landmark.X)
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
