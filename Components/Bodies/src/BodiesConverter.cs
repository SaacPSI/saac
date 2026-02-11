// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using nuitrack;

    /// <summary>
    /// Component that converts body tracking data from different sources (Nuitrack or Azure Kinect) into a unified SimplifiedBody format.
    /// </summary>
    public class BodiesConverter : IProducer<List<SimplifiedBody>>
    {
        private readonly Dictionary<JointType, Microsoft.Azure.Kinect.BodyTracking.JointId> nuiToAzure;
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="BodiesConverter"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public BodiesConverter(Pipeline parent, string name = nameof(BodiesConverter))
        {
            this.name = name;
            this.InBodiesNuitrack = parent.CreateReceiver<List<Skeleton>>(this, this.Process, $"{name}-InBodiesNuitrack");
            this.InBodiesAzure = parent.CreateReceiver<List<AzureKinectBody>>(this, this.Process, $"{name}-InBodiesAzure");
            this.Out = parent.CreateEmitter<List<SimplifiedBody>>(this, $"{name}-Out");
            this.nuiToAzure = new Dictionary<JointType, Microsoft.Azure.Kinect.BodyTracking.JointId>
            {
                { JointType.Head, Microsoft.Azure.Kinect.BodyTracking.JointId.Head },
                { JointType.Neck, Microsoft.Azure.Kinect.BodyTracking.JointId.Neck },
                { JointType.Torso, Microsoft.Azure.Kinect.BodyTracking.JointId.SpineChest },
                { JointType.Waist, Microsoft.Azure.Kinect.BodyTracking.JointId.Pelvis },
                { JointType.LeftCollar, Microsoft.Azure.Kinect.BodyTracking.JointId.ClavicleLeft },
                { JointType.LeftShoulder, Microsoft.Azure.Kinect.BodyTracking.JointId.ShoulderLeft },
                { JointType.LeftElbow, Microsoft.Azure.Kinect.BodyTracking.JointId.ElbowLeft },
                { JointType.LeftWrist, Microsoft.Azure.Kinect.BodyTracking.JointId.WristLeft },
                { JointType.LeftHand, Microsoft.Azure.Kinect.BodyTracking.JointId.HandLeft },
                { JointType.LeftFingertip, Microsoft.Azure.Kinect.BodyTracking.JointId.HandTipLeft },
                { JointType.RightCollar, Microsoft.Azure.Kinect.BodyTracking.JointId.ClavicleRight },
                { JointType.RightShoulder, Microsoft.Azure.Kinect.BodyTracking.JointId.ShoulderRight },
                { JointType.RightElbow, Microsoft.Azure.Kinect.BodyTracking.JointId.ElbowRight },
                { JointType.RightWrist, Microsoft.Azure.Kinect.BodyTracking.JointId.WristRight },
                { JointType.RightHand, Microsoft.Azure.Kinect.BodyTracking.JointId.HandRight },
                { JointType.RightFingertip, Microsoft.Azure.Kinect.BodyTracking.JointId.HandTipRight },
                { JointType.LeftHip, Microsoft.Azure.Kinect.BodyTracking.JointId.HipLeft },
                { JointType.LeftKnee, Microsoft.Azure.Kinect.BodyTracking.JointId.KneeLeft },
                { JointType.LeftAnkle, Microsoft.Azure.Kinect.BodyTracking.JointId.AnkleLeft },
                { JointType.LeftFoot, Microsoft.Azure.Kinect.BodyTracking.JointId.FootLeft },
                { JointType.RightHip, Microsoft.Azure.Kinect.BodyTracking.JointId.HipRight },
                { JointType.RightKnee, Microsoft.Azure.Kinect.BodyTracking.JointId.KneeRight },
                { JointType.RightAnkle, Microsoft.Azure.Kinect.BodyTracking.JointId.AnkleRight },
                { JointType.RightFoot, Microsoft.Azure.Kinect.BodyTracking.JointId.FootRight }
            };
        }

        /// <summary>
        /// Gets the emitter of converted bodies.
        /// </summary>
        public Emitter<List<SimplifiedBody>> Out { get; private set; }

        /// <summary>
        /// Gets the Nuitrack receiver for lists of currently tracked bodies.
        /// </summary>
        public Receiver<List<Skeleton>> InBodiesNuitrack { get; private set; }

        /// <summary>
        /// Gets the Azure Kinect receiver for lists of currently tracked bodies.
        /// </summary>
        public Receiver<List<AzureKinectBody>> InBodiesAzure { get; private set; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Processes Nuitrack skeleton data and converts it to simplified bodies.
        /// </summary>
        /// <param name="bodies">The list of Nuitrack skeletons.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process(List<Skeleton> bodies, Envelope envelope)
        {
            List<SimplifiedBody> returnedBodies = new List<SimplifiedBody>();
            foreach (var skeleton in bodies)
            {
                SimplifiedBody body = new SimplifiedBody(SimplifiedBody.SensorOrigin.Azure, (uint)skeleton.ID);
                foreach (var joint in skeleton.Joints)
                {
                    if (this.nuiToAzure.ContainsKey(joint.Type))
                    {
                        body.Joints.Add(
                            this.nuiToAzure[joint.Type],
                            new Tuple<Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel, Vector3D>(
                                Helpers.Helpers.FloatToConfidence(joint.Confidence),
                                Helpers.Helpers.NuitrackToMathNet(joint.Real)));
                    }
                }

                this.CompleteBody(ref body);
                returnedBodies.Add(body);
            }

            this.Out.Post(returnedBodies, envelope.OriginatingTime);
        }

        /// <summary>
        /// Completes a body by adding missing joints based on existing joints.
        /// Creates fake positions for joints not provided by Nuitrack.
        /// </summary>
        /// <param name="body">The body to complete.</param>
        private void CompleteBody(ref SimplifiedBody body)
        {
            Vector3D fakePosition = (body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.SpineChest].Item2 + body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Pelvis].Item2) / 2.0;
            body.Joints.Add(Microsoft.Azure.Kinect.BodyTracking.JointId.SpineNavel, new Tuple<Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel, Vector3D>
                              (body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Pelvis].Item1, fakePosition));
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.ThumbLeft] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.WristLeft];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.ThumbRight] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.WristRight];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Nose] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Head];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.EyeLeft] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Head];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.EyeRight] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Head];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.EarRight] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Head];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.EarLeft] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Head];
        }

        /// <summary>
        /// Processes Azure Kinect body data and converts it to simplified bodies.
        /// </summary>
        /// <param name="bodies">The list of Azure Kinect bodies.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process(List<AzureKinectBody> bodies, Envelope envelope)
        {
            List<SimplifiedBody> returnedBodies = new List<SimplifiedBody>();
            foreach (var skeleton in bodies)
            {
                SimplifiedBody body = new SimplifiedBody(SimplifiedBody.SensorOrigin.Azure, skeleton.TrackingId);
                foreach (var joint in skeleton.Joints)
                {
                    body.Joints.Add(joint.Key, new Tuple<Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel, Vector3D>(joint.Value.Confidence, joint.Value.Pose.Origin.ToVector3D()));
                }

                returnedBodies.Add(body);
            }

            this.Out.Post(returnedBodies, envelope.OriginatingTime);
        }
    }
}
