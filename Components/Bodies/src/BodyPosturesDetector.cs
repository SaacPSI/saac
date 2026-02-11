// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that detects body postures from simplified body data.
    /// </summary>
    public class BodyPosturesDetector : IConsumerProducer<List<SimplifiedBody>, Dictionary<uint, List<BodyPosturesDetector.Posture>>>
    {
        /// <summary>
        /// Enumeration of detected postures.
        /// </summary>
        public enum Posture
        {
            /// <summary>Standing posture.</summary>
            Standing,

            /// <summary>Sitting posture.</summary>
            Sitting,

            /// <summary>Pointing left posture.</summary>
            Pointing_Left,

            /// <summary>Pointing right posture.</summary>
            Pointing_Right,

            /// <summary>Arms crossed posture.</summary>
            ArmCrossed,
        }

        private const double DoubleFloatingPointTolerance = double.Epsilon * 2;

        /// <summary>
        /// Gets the receiver for input simplified bodies.
        /// </summary>
        public Receiver<List<SimplifiedBody>> In { get; }

        /// <summary>
        /// Gets the emitter for detected postures.
        /// </summary>
        public Emitter<Dictionary<uint, List<Posture>>> Out { get; }

        private BodyPosturesDetectorConfiguration configuration;
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyPosturesDetector"/> class.
        /// </summary>
        /// <param name="pipeline">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration for posture detection.</param>
        /// <param name="name">Optional name for the component.</param>
        public BodyPosturesDetector(Pipeline pipeline, BodyPosturesDetectorConfiguration? configuration = null, string name = nameof(BodyPosturesDetector))
        {
            this.name = name;
            this.configuration = configuration ?? new BodyPosturesDetectorConfiguration();
            this.In = pipeline.CreateReceiver<List<SimplifiedBody>>(this, this.Process, $"{name}-In");
            this.Out = pipeline.CreateEmitter<Dictionary<uint, List<BodyPosturesDetector.Posture>>>(this, $"{name}-Out");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Processes a list of simplified bodies and detects postures.
        /// </summary>
        /// <param name="bodies">The list of simplified bodies.</param>
        /// <param name="envelope">The message envelope.</param>
        public void Process(List<SimplifiedBody> bodies, Envelope envelope)
        {
            Dictionary<uint, List<Posture>> postures = new Dictionary<uint, List<Posture>>();
            foreach (var body in bodies)
            {
                var listing = this.ProcessBodies(body);
                if (listing.Count > 0)
                {
                    postures.Add(body.Id, listing);
                }
            }

            if (postures.Count > 0)
            {
                this.Out.Post(postures, envelope.OriginatingTime);
            }
        }

        private List<Posture> ProcessBodies(SimplifiedBody body)
        {
            List<Posture> postures = new List<Posture>();

            if (this.CheckArmsCrossed(body))
            {
                postures.Add(Posture.ArmCrossed);
            }

            if (this.CheckPointingLeft(body))
            {
                postures.Add(Posture.Pointing_Left);
            }

            if (this.CheckPointingRight(body))
            {
                postures.Add(Posture.Pointing_Right);
            }

            var neck = body.Joints[JointId.Neck];
            var pelvis = body.Joints[JointId.Pelvis];
            if (!Helpers.Helpers.CheckConfidenceLevel(new[] { neck, pelvis }, this.configuration.MinimumConfidenceLevel))
            {
                return postures;
            }

            Line3D reference = new Line3D(pelvis.Item2.ToPoint3D(), neck.Item2.ToPoint3D());

            if (this.CheckSittings(body, reference))
            {
                postures.Add(Posture.Sitting);
            }

            if (this.CheckStanding(body, reference))
            {
                postures.Add(Posture.Standing);
            }

            return postures;
        }

        private bool CheckArmsCrossed(in SimplifiedBody body)
        {
            var leftWrist = body.Joints[JointId.WristLeft];
            var leftElbow = body.Joints[JointId.ElbowLeft];
            var rightWrist = body.Joints[JointId.WristRight];
            var rightElbow = body.Joints[JointId.ElbowRight];

            if (!Helpers.Helpers.CheckConfidenceLevel(new[] { leftWrist, leftElbow, rightWrist, rightElbow }, this.configuration.MinimumConfidenceLevel))
            {
                return false;
            }

            Line3D left = new Line3D(leftWrist.Item2.ToPoint3D(), leftElbow.Item2.ToPoint3D());
            Line3D right = new Line3D(rightWrist.Item2.ToPoint3D(), rightElbow.Item2.ToPoint3D());

            var (pOnLine1, pOnLine2) = left.ClosestPointsBetween(right, mustBeOnSegments: false);
            var (pOnSeg1, pOnSeg2) = left.ClosestPointsBetween(right, mustBeOnSegments: true);
            return pOnLine1.Equals(pOnSeg1, tolerance: this.configuration.MinimumDistanceThreshold) && pOnLine2.Equals(pOnSeg2, tolerance: this.configuration.MinimumDistanceThreshold);
        }

        private bool CheckSittings(in SimplifiedBody body, in Line3D reference)
        {
            var leftKnee = body.Joints[JointId.KneeLeft];
            var leftHip = body.Joints[JointId.HipLeft];
            var rightKnee = body.Joints[JointId.KneeRight];
            var rightHip = body.Joints[JointId.HipRight];

            if (!Helpers.Helpers.CheckConfidenceLevel(new[] { leftKnee, leftHip, rightKnee, rightHip }, this.configuration.MinimumConfidenceLevel))
            {
                return false;
            }

            Line3D left = new Line3D(leftKnee.Item2.ToPoint3D(), leftHip.Item2.ToPoint3D());
            Line3D right = new Line3D(rightKnee.Item2.ToPoint3D(), rightHip.Item2.ToPoint3D());
            return this.AngleToDegrees(reference, left) > this.configuration.MinimumSittingDegrees && this.AngleToDegrees(reference, right) > this.configuration.MinimumSittingDegrees;
        }

        private bool CheckStanding(in SimplifiedBody body, in Line3D reference)
        {
            var leftAnkle = body.Joints[JointId.AnkleLeft];
            var leftHip = body.Joints[JointId.HipLeft];
            var rightAnkle = body.Joints[JointId.AnkleRight];
            var rightHip = body.Joints[JointId.HipRight];

            if (!Helpers.Helpers.CheckConfidenceLevel(new[] { leftAnkle, leftHip, rightAnkle, rightHip }, this.configuration.MinimumConfidenceLevel))
            {
                return false;
            }

            Line3D left = new Line3D(leftAnkle.Item2.ToPoint3D(), leftHip.Item2.ToPoint3D());
            Line3D right = new Line3D(rightAnkle.Item2.ToPoint3D(), rightHip.Item2.ToPoint3D());

            return this.AngleToDegrees(reference, left) < this.configuration.MaximumStandingDegrees && this.AngleToDegrees(reference, right) < this.configuration.MaximumStandingDegrees;
        }

        private bool CheckPointingRight(in SimplifiedBody body)
        {
            return this.CheckPointing(body.Joints[JointId.WristRight], body.Joints[JointId.ElbowRight], body.Joints[JointId.ShoulderRight]);
        }

        private bool CheckPointingLeft(in SimplifiedBody body)
        {
            return this.CheckPointing(body.Joints[JointId.WristLeft], body.Joints[JointId.ElbowLeft], body.Joints[JointId.ShoulderLeft]);
        }

        private bool CheckPointing(in Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D> wrist, in Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D> elbow, in Tuple<JointConfidenceLevel, MathNet.Spatial.Euclidean.Vector3D> shoulder)
        {
            if (!Helpers.Helpers.CheckConfidenceLevel(new[] { wrist, elbow, shoulder }, this.configuration.MinimumConfidenceLevel))
            {
                return false;
            }

            Line3D forarm = new Line3D(wrist.Item2.ToPoint3D(), elbow.Item2.ToPoint3D());
            Line3D arm = new Line3D(wrist.Item2.ToPoint3D(), shoulder.Item2.ToPoint3D());

            return this.AngleToDegrees(arm, forarm) < this.configuration.MaximumPointingDegrees;
        }

        private double AngleToDegrees(in Line3D origin, in Line3D target)
        {
            return origin.Direction.AngleTo(target.Direction).Degrees;
        }
    }
}
