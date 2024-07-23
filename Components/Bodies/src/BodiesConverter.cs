using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.AzureKinect;
using MathNet.Spatial.Euclidean;
using nuitrack;

namespace SAAC.Bodies
{
    public class BodiesConverter : IProducer<List<SimplifiedBody>>
    {
        /// <summary>
        /// Gets the emitter of bodies converted.
        /// </summary>
        public Emitter<List<SimplifiedBody>> Out { get; private set; }

        /// <summary>
        /// Gets the nuitrack receiver of lists of currently tracked bodies.
        /// </summary>
        public Receiver<List<Skeleton>> InBodiesNuitrack;

        /// <summary>
        /// Gets the azure reciever of lists of currently tracked bodies.
        /// </summary>
        public Receiver<List<AzureKinectBody>> InBodiesAzure;

        private Dictionary<JointType, Microsoft.Azure.Kinect.BodyTracking.JointId> nuiToAzure;
        private string name;
        public BodiesConverter(Pipeline parent, string name = nameof(BodiesConverter))
        {
            this.name = name;
            InBodiesNuitrack = parent.CreateReceiver<List<Skeleton>>(this, Process, $"{name}-InBodiesNuitrack");
            InBodiesAzure= parent.CreateReceiver<List<AzureKinectBody>>(this, Process, $"{name}-InBodiesAzure");
            Out = parent.CreateEmitter<List<SimplifiedBody>>(this, $"{name}-Out");
            nuiToAzure = new Dictionary<JointType, Microsoft.Azure.Kinect.BodyTracking.JointId> {
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
                { JointType.RightFoot, Microsoft.Azure.Kinect.BodyTracking.JointId.FootRight }};
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(List<Skeleton> bodies, Envelope envelope)
        {
            List<SimplifiedBody> returnedBodies = new List<SimplifiedBody>();
            foreach (var skeleton in bodies)
            {
                SimplifiedBody body = new SimplifiedBody(SimplifiedBody.SensorOrigin.Azure, (uint)skeleton.ID);
                foreach (var joint in skeleton.Joints)
                    if (nuiToAzure.ContainsKey(joint.Type))
                        body.Joints.Add(nuiToAzure[joint.Type], new Tuple<Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel, Vector3D>
                            (Helpers.Helpers.FloatToConfidence(joint.Confidence), Helpers.Helpers.NuitrackToMathNet(joint.Real)));
                CompleteBody(ref body);
                returnedBodies.Add(body);
            }
            Out.Post(returnedBodies, envelope.OriginatingTime);
        }

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

        private void Process(List<AzureKinectBody> bodies, Envelope envelope)
        {
            List<SimplifiedBody> returnedBodies = new List<SimplifiedBody>();
            foreach (var skeleton in bodies)
            {
                SimplifiedBody body = new SimplifiedBody(SimplifiedBody.SensorOrigin.Azure, skeleton.TrackingId);
                foreach (var joint in skeleton.Joints)
                    body.Joints.Add(joint.Key, new Tuple<Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel, Vector3D>
                        (joint.Value.Confidence, joint.Value.Pose.Origin.ToVector3D()));
                returnedBodies.Add(body);
            }
            Out.Post(returnedBodies, envelope.OriginatingTime);
        }
    }
}
