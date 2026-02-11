// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.Numerics;
using System.Windows;
using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using OpenFaceInterop;
using SAAC.Helpers;

// From https://github.com/ihp-lab/OpenSense/tree/master/Components/OpenFace.Windows
namespace SAAC.OpenFace
{
    /// <summary>
    /// OpenFace component for face analysis.
    /// </summary>
    public class OpenFace : IConsumer<Shared<Image>>
    {
        private CLNF? landmarkDetector;
        private FaceDetector? faceDetector;
        private FaceAnalyser? faceAnalyser;
        private GazeAnalyser? gazeAnalyser;
        private FaceModelParameters? faceModelParameters;
        private bool isInit;
        private Thread? initThread;
        private string name;
        private OpenFaceConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenFace"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="name">The name of the component.</param>
        public OpenFace(Pipeline pipeline, OpenFaceConfiguration configuration, string name = nameof(OpenFace))
        {
            this.name = name;
            this.configuration = configuration;
            this.isInit = false;
            this.initThread = null;
            this.In = pipeline.CreateReceiver<Shared<Image>>(this, this.ReceiveImage, $"{name}-In");

            // BoundingBoxes data emitter.
            this.OutBoundingBoxes = pipeline.CreateEmitter<List<System.Drawing.Rectangle>>(this, $"{name}-OutBoundingBoxes");

            // Pose data emitter.
            this.OutPose = pipeline.CreateEmitter<Pose>(this, $"{name}-OutPose");

            // Gaze data emitter.
            this.OutEyes = pipeline.CreateEmitter<Eye>(this, $"{name}-OutEyes");

            // Face data emitter.
            this.OutFace = pipeline.CreateEmitter<Face>(this, $"{name}-OutFace");

            pipeline.PipelineRun += this.Initialize;
        }

        /// <summary>
        /// Gets the receiver that encapsulates the shared image input stream.
        /// </summary>
        public Receiver<Shared<Image>> In { get; }

        /// <summary>
        /// Gets the emitter that encapsulates the boundingBoxes data output stream.
        /// </summary>
        public Emitter<List<System.Drawing.Rectangle>> OutBoundingBoxes { get; private set; }

        /// <summary>
        /// Gets the emitter that encapsulates the pose data output stream.
        /// </summary>
        public Emitter<Pose> OutPose { get; private set; }

        /// <summary>
        /// Gets the emitter that encapsulates the gaze data output stream.
        /// </summary>
        public Emitter<Eye> OutEyes { get; private set; }

        /// <summary>
        /// Gets the emitter that encapsulates the face data output stream.
        /// </summary>
        public Emitter<Face> OutFace { get; private set; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Initialize(object sender, PipelineRunEventArgs e)
        {
            this.faceModelParameters = new FaceModelParameters(this.configuration.ModelDirectory, true, false, false);
            this.faceModelParameters.optimiseForVideo();

            this.faceDetector = new FaceDetector(this.faceModelParameters.GetHaarLocation(), this.faceModelParameters.GetMTCNNLocation());
            this.isInit = true;
            if (!this.faceDetector.IsMTCNNLoaded())
            {
                this.faceModelParameters.SetFaceDetector(false, true, false);
            }

            this.landmarkDetector = new CLNF(this.faceModelParameters);
            if (this.configuration.Face)
            {
                this.faceAnalyser = new FaceAnalyser(this.configuration.ModelDirectory, dynamic: true, output_width: 112, mask_aligned: true);
            }

            if (this.configuration.Eyes)
            {
                this.gazeAnalyser = new GazeAnalyser();
            }

            this.landmarkDetector.Reset();
            if (this.faceAnalyser != null)
            {
                this.faceAnalyser.Reset();
            }
        }

        /// <summary>
        /// The receive method for the ImageIn receiver.
        /// This executes every time a message arrives on ImageIn.
        /// </summary>
        private void ReceiveImage(Shared<Image> input, Envelope envelope)
        {
            if (this.isInit == false)
            {
                return;
            }

            try
            {
                var width = input.Resource.Width;
                var height = input.Resource.Height;
                static Vector2 PointToVector2(Point p)
                {
                    return new Vector2((float)p.X, (float)p.Y);
                }

                static Vector2 TupleToVector2(Tuple<float, float> tuple)
                {
                    return new Vector2(tuple.Item1, tuple.Item2);
                }

                using (var colorSharedImage = ImagePool.GetOrCreate(width, height, PixelFormat.RGB_24bpp))
                using (var graySharedImage = ImagePool.GetOrCreate(width, height, PixelFormat.Gray_8bpp))
                {
                    colorSharedImage.Resource.CopyFrom(input.Resource.Convert(PixelFormat.RGB_24bpp));
                    var colorImageBuffer = new ImageBuffer(width, height, colorSharedImage.Resource.ImageData, colorSharedImage.Resource.Stride);
                    var grayImageBuffer = new ImageBuffer(width, height, graySharedImage.Resource.ImageData, graySharedImage.Resource.Stride);
                    Methods.ToGray(colorImageBuffer, grayImageBuffer);
                    using (var colorRawImage = Methods.ToRaw(colorImageBuffer))
                    using (var grayRawImage = Methods.ToRaw(grayImageBuffer))
                    {
                        float cx = input.Resource.Width / 2.0f, cy = input.Resource.Height / 2.0f;
                        float fx = input.Resource.Width / 4.0f, fy = input.Resource.Height / 4.0f;
                        if (this.landmarkDetector != null && this.landmarkDetector.DetectLandmarksInVideo(colorRawImage, this.faceModelParameters, grayRawImage))
                        {
                            var bboxes = this.landmarkDetector.GetBoundingBoxes(grayRawImage, 0.5f);
                            List<System.Drawing.Rectangle> boundingBoxes = new List<System.Drawing.Rectangle>();
                            foreach (var bbox in bboxes)
                            {
                                System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)bbox.Item1.X, (int)bbox.Item1.Y, (int)bbox.Item2.X, (int)bbox.Item2.Y);
                                boundingBoxes.Add(rect);
                            }

                            this.OutBoundingBoxes.Post(boundingBoxes, envelope.OriginatingTime);

                            if ((this.configuration.Pose || this.configuration.Eyes || this.configuration.Face))
                            {
                                var rawAllLandmarks = this.landmarkDetector.CalculateAllLandmarks();

                                // BoundingBoxes.
                                var allLandmarks = rawAllLandmarks.Select(TupleToVector2);
                                var visiableLandmarks = this.landmarkDetector
                                    .CalculateVisibleLandmarks()
                                    .Select(TupleToVector2);

                                // Pose.
                                if (this.configuration.Pose)
                                {
                                    var landmarks3D = this.landmarkDetector
                                      .Calculate3DLandmarks(fx, fy, cx, cy)
                                      .Select(m => new Vector3(m.Item1, m.Item2, m.Item3));
                                    var poseData = new List<float>();
                                    this.landmarkDetector.GetPose(poseData, fx, fy, cx, cy);
                                    var box = this.landmarkDetector.CalculateBox(fx, fy, cx, cy);
                                    var boxConverted = box.Select(line =>
                                    {
                                        var a = PointToVector2(line.Item1);
                                        var b = PointToVector2(line.Item2);
                                        return (a, b);
                                    });
                                    var headPose = new Pose(poseData, allLandmarks, visiableLandmarks, landmarks3D, boxConverted);
                                    this.OutPose.Post(headPose, envelope.OriginatingTime);
                                }

                                // Gaze.
                                if (this.gazeAnalyser != null)
                                {
                                    this.gazeAnalyser.AddNextFrame(this.landmarkDetector, success: true, fx, fy, cx, cy);
                                    var eyeLandmarks = this.landmarkDetector
                                        .CalculateAllEyeLandmarks()
                                        .Select(TupleToVector2);
                                    var visiableEyeLandmarks = this.landmarkDetector
                                        .CalculateVisibleEyeLandmarks()
                                        .Select(TupleToVector2);
                                    var eyeLandmarks3D = this.landmarkDetector
                                        .CalculateAllEyeLandmarks3D(fx, fy, cx, cy)
                                        .Select(m => new Vector3(m.Item1, m.Item2, m.Item3));
                                    var (leftPupil, rightPupil) = this.gazeAnalyser.GetGazeCamera();
                                    var (angleX, angleY) = this.gazeAnalyser.GetGazeAngle(); // Not accurate
                                    var gazeLines = this.gazeAnalyser.CalculateGazeLines(fx, fy, cx, cy);
                                    var gazeLinesConverted = gazeLines.Select(line =>
                                    {
                                        var a = PointToVector2(line.Item1);
                                        var b = PointToVector2(line.Item2);
                                        return (a, b);
                                    });
                                    var gaze = new Eye(
                                            new GazeVector(
                                                    new Vector3(leftPupil.Item1, leftPupil.Item2, leftPupil.Item3),
                                                    new Vector3(rightPupil.Item1, rightPupil.Item2, rightPupil.Item3)),
                                            new Vector2(angleX, angleY),
                                            eyeLandmarks,
                                            visiableEyeLandmarks,
                                            eyeLandmarks3D,
                                            gazeLinesConverted);
                                    this.OutEyes.Post(gaze, envelope.OriginatingTime);
                                }

                                // Face
                                if (this.faceAnalyser != null)
                                {
                                    var (actionUnitIntensities, actionUnitOccurences) = this.faceAnalyser.PredictStaticAUsAndComputeFeatures(colorRawImage, rawAllLandmarks); // image mode, so not using FaceAnalyser.AddNextFrame()
                                    var actionUnits = actionUnitIntensities.ToDictionary(
                                        kv => kv.Key.Substring(2)/*remove prefix "AU"*/.TrimStart('0'),
                                        kv => new ActionUnit(intensity: kv.Value, presence: actionUnitOccurences[kv.Key]));
                                    var face = new Face(actionUnits);
                                    this.OutFace.Post(face, envelope.OriginatingTime);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
