using Microsoft.Psi.Components;
using System.Numerics;
using System.Windows;
using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using OpenFaceInterop;
using SAAC.Helpers;

// Fromhttps://github.com/ihp-lab/OpenSense/tree/master/Components/OpenFace.Windows
namespace SAAC.OpenFace
{
    public class OpenFace : IConsumer<Shared<Image>>
    {
        /// <summary>
        /// Gets. Receiver that encapsulates the shared image input stream.
        /// </summary>
        public Connector<Shared<Image>> InConnector { get; private set; }

        /// <summary>
        /// Gets. Receiver that encapsulates the shared image input stream.
        /// </summary>
        public Receiver<Shared<Image>> In => InConnector.In;

        /// <summary>
        /// Gets. Emitter that encapsulates the boundingBoxes data output stream.
        /// </summary>
        public Emitter<List<System.Drawing.Rectangle>> OutBoundingBoxes { get; private set; }

        /// <summary>
        /// Gets. Emitter that encapsulates the pose data output stream.
        /// </summary>
        public Emitter<Pose> OutPose { get; private set; }

        /// <summary>
        /// Gets. Emitter that encapsulates the gaze data output stream.
        /// </summary>
        public Emitter<Eye> OutEyes { get; private set; }

        /// <summary>
        /// Gets. Emitter that encapsulates the face data output stream.
        /// </summary>
        public Emitter<Face> OutFace { get; private set; }

        private CLNF? LandmarkDetector;
        private FaceDetector? FaceDetector;
        private FaceAnalyser? FaceAnalyser;
        private GazeAnalyser? GazeAnalyser;
        private FaceModelParameters? FaceModelParameters;
        private bool isInit = false; 
        private Thread? initThread = null;

        private OpenFaceConfiguration Configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenFace"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        public OpenFace(Pipeline pipeline, OpenFaceConfiguration configuration) 
        {
            Configuration = configuration;
            // Image receiver.
            InConnector = pipeline.CreateConnector<Shared<Image>>(nameof(In));//pipeline.CreateReceiver<Shared<Image>>(this, ReceiveImage, nameof(In));
            InConnector.Out.Do(ReceiveImage);

            // BoundingBoxes data emitter.
            OutBoundingBoxes = pipeline.CreateEmitter<List<System.Drawing.Rectangle>>(this, nameof(OutBoundingBoxes));
            // Pose data emitter.
            OutPose = pipeline.CreateEmitter<Pose>(this, nameof(OutPose));
            // Gaze data emitter.
            OutEyes = pipeline.CreateEmitter<Eye>(this, nameof(OutEyes));
            // Face data emitter.
            OutFace = pipeline.CreateEmitter<Face>(this, nameof(OutFace));

            pipeline.PipelineRun += Initialize;
        }

        private void Initialize(object sender, PipelineRunEventArgs e)
        {
            FaceModelParameters = new FaceModelParameters(Configuration.ModelDirectory, true, false, false);
            FaceModelParameters.optimiseForVideo();

            FaceDetector = new FaceDetector(FaceModelParameters.GetHaarLocation(), FaceModelParameters.GetMTCNNLocation());
            isInit = true;
            if (!FaceDetector.IsMTCNNLoaded())
            {
                FaceModelParameters.SetFaceDetector(false, true, false);
            }

            LandmarkDetector = new CLNF(FaceModelParameters);
            if (Configuration.Face)
            {
                FaceAnalyser = new FaceAnalyser(Configuration.ModelDirectory, dynamic: true, output_width: 112, mask_aligned: true);
            }
            if (Configuration.Eyes)
            {
                GazeAnalyser = new GazeAnalyser();
            }

            LandmarkDetector.Reset();
            if (FaceAnalyser != null)
            {
                FaceAnalyser.Reset();
            }
        }

        /// <summary>
        /// The receive method for the ImageIn receiver.
        /// This executes every time a message arrives on ImageIn.
        /// </summary>
        private void ReceiveImage(Shared<Image> input, Envelope envelope) 
        {
            if (isInit == false)
                return;
            try 
            {
                var width = input.Resource.Width;
                var height = input.Resource.Height;
                static Vector2 pointToVector2(Point p) {
                    return new Vector2((float)p.X, (float)p.Y);
                }
                static Vector2 tupleToVector2(Tuple<float, float> tuple) {
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
                        float Cx = input.Resource.Width / 2.0f, Cy = input.Resource.Height / 2.0f;
                        float Fx = input.Resource.Width / 4.0f, Fy = input.Resource.Height / 4.0f;
                        if (LandmarkDetector != null && LandmarkDetector.DetectLandmarksInVideo(colorRawImage, FaceModelParameters, grayRawImage))
                        {
                            var bboxes = LandmarkDetector.GetBoundingBoxes(grayRawImage, 0.5f);
                            List<System.Drawing.Rectangle> boundingBoxes = new List<System.Drawing.Rectangle>();
                            foreach (var bbox in bboxes)
                            {
                                System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)bbox.Item1.X, (int)bbox.Item1.Y, (int)bbox.Item2.X, (int)bbox.Item2.Y);
                                boundingBoxes.Add(rect);
                            }
                            OutBoundingBoxes.Post(boundingBoxes, envelope.OriginatingTime);
                        
                            if((Configuration.Pose || Configuration.Eyes || Configuration.Face) )
                            {
                                var rawAllLandmarks = LandmarkDetector.CalculateAllLandmarks();

                                // BoundingBoxes.
                                var allLandmarks = rawAllLandmarks.Select(tupleToVector2);
                                var visiableLandmarks = LandmarkDetector
                                    .CalculateVisibleLandmarks()
                                    .Select(tupleToVector2);

                                // Pose.
                                if (Configuration.Pose)
                                {
                                    var landmarks3D = LandmarkDetector
                                      .Calculate3DLandmarks(Fx, Fy, Cx, Cy)
                                      .Select(m => new Vector3(m.Item1, m.Item2, m.Item3));
                                    var poseData = new List<float>();
                                    LandmarkDetector.GetPose(poseData, Fx, Fy, Cx, Cy);
                                    var box = LandmarkDetector.CalculateBox(Fx, Fy, Cx, Cy);
                                    var boxConverted = box.Select(line =>
                                    {
                                        var a = pointToVector2(line.Item1);
                                        var b = pointToVector2(line.Item2);
                                        return (a, b);
                                    });
                                    var headPose = new Pose(poseData, allLandmarks, visiableLandmarks, landmarks3D, boxConverted);
                                    OutPose.Post(headPose, envelope.OriginatingTime);
                                }

                                // Gaze.
                                if (GazeAnalyser != null)
                                {
                                    GazeAnalyser.AddNextFrame(LandmarkDetector, success: true, Fx, Fy, Cx, Cy);
                                    var eyeLandmarks = LandmarkDetector
                                        .CalculateAllEyeLandmarks()
                                        .Select(tupleToVector2);
                                    var visiableEyeLandmarks = LandmarkDetector
                                        .CalculateVisibleEyeLandmarks()
                                        .Select(tupleToVector2);
                                    var eyeLandmarks3D = LandmarkDetector
                                        .CalculateAllEyeLandmarks3D(Fx, Fy,  Cx, Cy)
                                        .Select(m => new Vector3(m.Item1, m.Item2, m.Item3));
                                    var (leftPupil, rightPupil) = GazeAnalyser.GetGazeCamera();
                                    var (angleX, angleY) = GazeAnalyser.GetGazeAngle();//Not accurate
                                    var gazeLines = GazeAnalyser.CalculateGazeLines(Fx, Fy,  Cx, Cy);
                                    var gazeLinesConverted = gazeLines.Select(line =>
                                    {
                                        var a = pointToVector2(line.Item1);
                                        var b = pointToVector2(line.Item2);
                                        return (a, b);
                                    });
                                    var gaze = new Eye(
                                            new GazeVector(
                                                    new Vector3(leftPupil.Item1, leftPupil.Item2, leftPupil.Item3),
                                                    new Vector3(rightPupil.Item1, rightPupil.Item2, rightPupil.Item3)
                                                ),
                                            new Vector2(angleX, angleY),
                                            eyeLandmarks,
                                            visiableEyeLandmarks,
                                            eyeLandmarks3D,
                                            gazeLinesConverted
                                        );
                                    OutEyes.Post(gaze, envelope.OriginatingTime);
                                }

                                //Face
                                if (FaceAnalyser != null)
                                {
                                    var (actionUnitIntensities, actionUnitOccurences) = FaceAnalyser.PredictStaticAUsAndComputeFeatures(colorRawImage, rawAllLandmarks);//image mode, so not using FaceAnalyser.AddNextFrame()
                                    var actionUnits = actionUnitIntensities.ToDictionary(
                                        kv => kv.Key.Substring(2)/*remove prefix "AU"*/.TrimStart('0'),
                                        kv => new ActionUnit(intensity: kv.Value, presence: actionUnitOccurences[kv.Key])
                                    );
                                    var face = new Face(actionUnits);
                                    OutFace.Post(face, envelope.OriginatingTime);
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
