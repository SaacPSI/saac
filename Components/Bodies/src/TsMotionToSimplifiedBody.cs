// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Components;
    using TsAPI.Types;

    /// <summary>
    /// Component that converts Tesla Suit motion data to simplified body format.
    /// </summary>
    public class TsMotionToSimplifiedBody : IConsumerProducer<Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4>, List<SimplifiedBody>>
    {
        private readonly Dictionary<TsHumanBoneIndex, Microsoft.Azure.Kinect.BodyTracking.JointId> tsToAzure;
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="TsMotionToSimplifiedBody"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public TsMotionToSimplifiedBody(Pipeline parent, string name = nameof(TsMotionToSimplifiedBody))
        {
            this.name = name;
            this.In = parent.CreateReceiver<Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4>>(this, this.Process, $"{name}-In");
            this.Out = parent.CreateEmitter<List<SimplifiedBody>>(this, nameof(this.Out));
            this.tsToAzure = new Dictionary<TsHumanBoneIndex, Microsoft.Azure.Kinect.BodyTracking.JointId>
            {
                { TsHumanBoneIndex.Head, Microsoft.Azure.Kinect.BodyTracking.JointId.Head },
                { TsHumanBoneIndex.Neck, Microsoft.Azure.Kinect.BodyTracking.JointId.Neck },
                { TsHumanBoneIndex.UpperSpine, Microsoft.Azure.Kinect.BodyTracking.JointId.SpineChest },
                { TsHumanBoneIndex.Hips, Microsoft.Azure.Kinect.BodyTracking.JointId.Pelvis },
                { TsHumanBoneIndex.LeftUpperLeg, Microsoft.Azure.Kinect.BodyTracking.JointId.HipLeft },
                { TsHumanBoneIndex.LeftLowerLeg, Microsoft.Azure.Kinect.BodyTracking.JointId.KneeLeft },
                { TsHumanBoneIndex.RightUpperLeg, Microsoft.Azure.Kinect.BodyTracking.JointId.HipRight },
                { TsHumanBoneIndex.RightLowerLeg, Microsoft.Azure.Kinect.BodyTracking.JointId.KneeRight },
                { TsHumanBoneIndex.RightFoot, Microsoft.Azure.Kinect.BodyTracking.JointId.FootRight },
                { TsHumanBoneIndex.LeftShoulder, Microsoft.Azure.Kinect.BodyTracking.JointId.ShoulderLeft },
                { TsHumanBoneIndex.LeftUpperArm, Microsoft.Azure.Kinect.BodyTracking.JointId.ElbowLeft },
                { TsHumanBoneIndex.LeftLowerArm, Microsoft.Azure.Kinect.BodyTracking.JointId.WristLeft },
                { TsHumanBoneIndex.LeftHand, Microsoft.Azure.Kinect.BodyTracking.JointId.HandLeft },
                { TsHumanBoneIndex.RightShoulder, Microsoft.Azure.Kinect.BodyTracking.JointId.ShoulderRight },
                { TsHumanBoneIndex.RightUpperArm, Microsoft.Azure.Kinect.BodyTracking.JointId.ElbowRight },
                { TsHumanBoneIndex.RightLowerArm, Microsoft.Azure.Kinect.BodyTracking.JointId.WristRight },
                { TsHumanBoneIndex.RightHand, Microsoft.Azure.Kinect.BodyTracking.JointId.HandRight },
                { TsHumanBoneIndex.LeftFoot, Microsoft.Azure.Kinect.BodyTracking.JointId.FootLeft }
            };
        }

        /// <summary>
        /// Gets the emitter of converted bodies.
        /// </summary>
        public Emitter<List<SimplifiedBody>> Out { get; private set; }

        /// <summary>
        /// Gets the receiver for Tesla Suit motion data.
        /// </summary>
        public Receiver<Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4>> In { get; private set; }

        /// <summary>
        /// Processes Tesla Suit motion data and converts it to simplified body format.
        /// </summary>
        /// <param name="body">The Tesla Suit bone data.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process(Dictionary<TsHumanBoneIndex, System.Numerics.Matrix4x4> body, Envelope envelope)
        {
            if (body.Count == 0)
            {
                return;
            }

            SimplifiedBody sBody = new SimplifiedBody(SimplifiedBody.SensorOrigin.TeslaSuit, 0);
            foreach (var joint in body)
            {
                if (this.tsToAzure.ContainsKey(joint.Key))
                {
                    sBody.Joints.Add(
                        this.tsToAzure[joint.Key],
                        new Tuple<Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel, Vector3D>(
                            JointConfidenceLevel.High,
                            Helpers.Helpers.NumericToMathNet(joint.Value.Translation)));
                }
            }

            this.CompleteBody(ref sBody);
            this.Out.Post([sBody], envelope.OriginatingTime);
        }

        /// <summary>
        /// Completes a Tesla Suit body by adding missing joints based on existing joints.
        /// </summary>
        /// <param name="body">The body to complete.</param>
        private void CompleteBody(ref SimplifiedBody body)
        {
            Vector3D fakePosition = (body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.SpineChest].Item2 + body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Pelvis].Item2) / 2.0;
            body.Joints.Add(Microsoft.Azure.Kinect.BodyTracking.JointId.SpineNavel, new Tuple<Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel, Vector3D>(body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Pelvis].Item1, fakePosition));
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.ThumbLeft] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.WristLeft];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.ThumbRight] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.WristRight];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Nose] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Head];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.EyeLeft] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Head];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.EyeRight] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Head];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.EarRight] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Head];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.EarLeft] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.Head];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.ClavicleLeft] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.ShoulderLeft];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.ClavicleRight] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.ShoulderRight];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.HandLeft] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.HandTipLeft];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.HandRight] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.HandTipRight];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.ClavicleLeft] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.ShoulderLeft];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.ClavicleRight] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.ShoulderRight];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.AnkleRight] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.FootRight];
            body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.AnkleLeft] = body.Joints[Microsoft.Azure.Kinect.BodyTracking.JointId.FootLeft];
        }

        /// <summary>
        /// Processes Azure Kinect body data (alternative overload).
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
                    body.Joints.Add(
                        joint.Key,
                        new Tuple<Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel, Vector3D>(
                            joint.Value.Confidence,
                            joint.Value.Pose.Origin.ToVector3D()));
                }

                returnedBodies.Add(body);
            }

            this.Out.Post(returnedBodies, envelope.OriginatingTime);
        }
    }
}
