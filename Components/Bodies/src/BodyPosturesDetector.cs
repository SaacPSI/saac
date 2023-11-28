using MathNet.Numerics.RootFinding;
using MathNet.Spatial.Euclidean;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace Bodies 
{
    public class BodyPosturesDetector : IConsumerProducer<List<SimplifiedBody>, Dictionary<uint, List<BodyPosturesDetector.Posture>>>
    {
        public enum Posture { Standing, Sitting, Pointing_Left, Pointing_Right, ArmCrossed };
        
        private const double DoubleFloatingPointTolerance = double.Epsilon * 2;

        public Receiver<List<SimplifiedBody>> In { get; }

        public Emitter<Dictionary<uint, List<Posture>>> Out { get; }

        private BodyPosturesDetectorConfiguration Configuration;

        public BodyPosturesDetector(Pipeline pipeline, BodyPosturesDetectorConfiguration? configuration = null) 
        {
            Configuration = configuration ?? new BodyPosturesDetectorConfiguration();
            In = pipeline.CreateReceiver<List<SimplifiedBody>>(this, Process, nameof(In));
            Out = pipeline.CreateEmitter<Dictionary<uint, List<BodyPosturesDetector.Posture>>>(this, nameof(Out));
        }

        public void Process(List<SimplifiedBody> bodies, Envelope envelope)
        {
            Dictionary<uint, List<Posture>> postures = new Dictionary<uint, List<Posture>>();
            foreach (var body in bodies) 
            {
               var listing = ProcessBodies(body);
                if(listing.Count > 0)
                    postures.Add(body.Id, listing);
            }
            if (postures.Count > 0)
                Out.Post(postures, envelope.OriginatingTime);
        }

        private List<Posture> ProcessBodies(SimplifiedBody body)
        {
            List<Posture> postures = new List<Posture>();

            if (CheckArmsCrossed(body))
                postures.Add(Posture.ArmCrossed);

            if (CheckPointingLeft(body))
                postures.Add(Posture.Pointing_Left);

            if (CheckPointingRight(body))
                postures.Add(Posture.Pointing_Right);

            var neck = body.Joints[JointId.Neck];
            var pelvis = body.Joints[JointId.Pelvis];
            if (!Helpers.Helpers.CheckConfidenceLevel(new[] { neck, pelvis }, Configuration.MinimumConfidenceLevel))
                return postures;

            Line3D reference = new Line3D(pelvis.Item2.ToPoint3D(), neck.Item2.ToPoint3D());

            if (CheckSittings(body, reference))
                postures.Add(Posture.Sitting);

            if (CheckStanding(body, reference))
                postures.Add(Posture.Standing);

            return postures;
        }

        private bool CheckArmsCrossed(in SimplifiedBody body)
        {
            var leftWrist = body.Joints[JointId.WristLeft];
            var leftElbow = body.Joints[JointId.ElbowLeft];
            var rightWrist = body.Joints[JointId.WristRight];
            var rightElbow = body.Joints[JointId.ElbowRight];

            if (!Helpers.Helpers.CheckConfidenceLevel(new[] { leftWrist, leftElbow, rightWrist, rightElbow }, Configuration.MinimumConfidenceLevel))
                return false;

            Line3D left = new Line3D(leftWrist.Item2.ToPoint3D(), leftElbow.Item2.ToPoint3D());
            Line3D right = new Line3D(rightWrist.Item2.ToPoint3D(), rightElbow.Item2.ToPoint3D());

            var (pOnLine1, pOnLine2) = left.ClosestPointsBetween(right, mustBeOnSegments: false);
            var (pOnSeg1, pOnSeg2) = left.ClosestPointsBetween(right, mustBeOnSegments: true);
            return pOnLine1.Equals(pOnSeg1, tolerance: Configuration.MinimumDistanceThreshold) && pOnLine2.Equals(pOnSeg2, tolerance: Configuration.MinimumDistanceThreshold);
        }

        private bool CheckSittings(in SimplifiedBody body, in Line3D reference)
        {
            var leftKnee = body.Joints[JointId.KneeLeft];
            var leftHip = body.Joints[JointId.HipLeft];
            var rightKnee = body.Joints[JointId.KneeRight];
            var rightHip = body.Joints[JointId.HipRight];

            if (!Helpers.Helpers.CheckConfidenceLevel(new[] { leftKnee, leftHip, rightKnee, rightHip }, Configuration.MinimumConfidenceLevel))
                return false;

            Line3D left = new Line3D(leftKnee.Item2.ToPoint3D(), leftHip.Item2.ToPoint3D());
            Line3D right = new Line3D(rightKnee.Item2.ToPoint3D(), rightHip.Item2.ToPoint3D());
            return AngleToDegrees(reference, left) > Configuration.MinimumSittingDegrees && AngleToDegrees(reference, right) > Configuration.MinimumSittingDegrees;
        }

        private bool CheckStanding(in SimplifiedBody body, in Line3D reference)
        {
            var leftAnkle = body.Joints[JointId.AnkleLeft];
            var leftHip = body.Joints[JointId.HipLeft];
            var rightAnkle = body.Joints[JointId.AnkleRight];
            var rightHip = body.Joints[JointId.HipRight];

            if (!Helpers.Helpers.CheckConfidenceLevel(new[] { leftAnkle, leftHip, rightAnkle, rightHip }, Configuration.MinimumConfidenceLevel))
                return false;
     
            Line3D left = new Line3D(leftAnkle.Item2.ToPoint3D(), leftHip.Item2.ToPoint3D());
            Line3D right = new Line3D(rightAnkle.Item2.ToPoint3D(), rightHip.Item2.ToPoint3D());

            return AngleToDegrees(reference, left) < Configuration.MaximumStandingDegrees && AngleToDegrees(reference, right) < Configuration.MaximumStandingDegrees;
        }

        private bool CheckPointingRight(in SimplifiedBody body)
        {
            return CheckPointing(body.Joints[JointId.WristRight], body.Joints[JointId.ElbowRight], body.Joints[JointId.ShoulderRight]);
        }

        private bool CheckPointingLeft(in SimplifiedBody body)
        {
            return CheckPointing(body.Joints[JointId.WristLeft], body.Joints[JointId.ElbowLeft], body.Joints[JointId.ShoulderLeft]);
        }

        private bool CheckPointing(in Tuple<JointConfidenceLevel,MathNet.Spatial.Euclidean.Vector3D> wrist, in Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D> elbow, in Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D> shoulder)
        {
            if (!Helpers.Helpers.CheckConfidenceLevel(new[] { wrist, elbow, shoulder }, Configuration.MinimumConfidenceLevel))
                return false;

            Line3D Forarm = new Line3D(wrist.Item2.ToPoint3D(), elbow.Item2.ToPoint3D());
            Line3D Arm = new Line3D(wrist.Item2.ToPoint3D(), shoulder.Item2.ToPoint3D());

            return AngleToDegrees(Arm, Forarm) < Configuration.MaximumPointingDegrees;
        }

        private double AngleToDegrees(in Line3D origin, in Line3D target) 
        {
            return origin.Direction.AngleTo(target.Direction).Degrees;
        }
    }
}
