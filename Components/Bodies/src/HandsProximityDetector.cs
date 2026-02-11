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
    /// Simple component that extract the position of a joint from given bodies .
    /// See SimpleBodiesPositionExtractionConfiguration for parameters details.
    /// </summary>
    public class HandsProximityDetector : IConsumerProducer<List<SimplifiedBody>, Dictionary<(uint, uint), List<HandsProximityDetector.HandsProximity>>>
    {
        /// <summary>
        /// Enumeration describing which hands are close.
        /// </summary>
        public enum HandsProximity
        {
            /// <summary>Left-Left hands proximity.</summary>
            LeftLeft,

            /// <summary>Left-Right hands proximity.</summary>
            LeftRight,

            /// <summary>Right-Left hands proximity.</summary>
            RightLeft,

            /// <summary>Right-Right hands proximity.</summary>
            RightRight,
        }

        /// <summary>
        /// Connector that specifies which pair of bodies to check.
        /// </summary>
        private Connector<List<(uint, uint)>> InPairConnector;

        /// <summary>
        /// Gets the receiver for pair specification.
        /// </summary>
        public Receiver<List<(uint, uint)>> InPair => InPairConnector.In;

        /// <summary>
        /// Connector for bodies receiver.
        /// </summary>
        private Connector<List<SimplifiedBody>> InConnector;

        /// <summary>
        /// Gets the receiver of bodies.
        /// </summary>
        public Receiver<List<SimplifiedBody>> In => InConnector.In;

        /// <summary>
        /// Gets the emitter that outputs which hands are close.
        /// </summary>
        public Emitter<Dictionary<(uint, uint), List<HandsProximity>>> Out { get; }

        private HandsProximityDetectorConfiguration configuration;
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandsProximityDetector"/> class.
        /// </summary>
        /// <param name="pipeline">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration for hands proximity detection.</param>
        /// <param name="name">Optional name for the component.</param>
        public HandsProximityDetector(Pipeline pipeline, HandsProximityDetectorConfiguration configuration = null, string name = nameof(HandsProximityDetector))
        {
            this.name = name;
            this.configuration = configuration ?? new HandsProximityDetectorConfiguration();
            this.InConnector = pipeline.CreateConnector<List<SimplifiedBody>>($"{name}-In");
            this.InPairConnector = pipeline.CreateConnector<List<(uint, uint)>>($"{name}-InPair");
            this.Out = pipeline.CreateEmitter<Dictionary<(uint, uint), List<HandsProximity>>>(this, $"{name}-Out");
            if (this.configuration.IsPairToCheckGiven)
            {
                this.InConnector.Out.Pair(this.InPairConnector, DeliveryPolicy.LatestMessage, DeliveryPolicy.LatestMessage).Do(this.Process);
            }
            else
            {
                this.InConnector.Out.Do(this.Process);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(List<SimplifiedBody> message, Envelope envelope)
        {
            if (message.Count == 0)
            {
                return;
            }

            Dictionary<(uint, uint), List<HandsProximity>> post = new Dictionary<(uint, uint), List<HandsProximity>>();
            foreach (var body1 in message)
            {
                foreach (var body2 in message)
                {
                    Tuple<(uint, uint), List<HandsProximity>> detected;
                    if (this.ProcessBodies(body1, body2, out detected))
                    {
                        post.Add(detected.Item1, detected.Item2);
                    }
                }
            }

            if (post.Count > 0)
            {
                this.Out.Post(post, envelope.OriginatingTime);
            }
        }

        private void Process((List<SimplifiedBody>, List<(uint, uint)>) message, Envelope envelope)
        {
            var (bodies, list) = message;
            if (bodies.Count == 0 || list.Count == 0)
            {
                return;
            }

            Dictionary<uint, SimplifiedBody> bodiesDics = new Dictionary<uint, SimplifiedBody>();
            foreach (var body in bodies)
            {
                bodiesDics.Add(body.Id, body);
            }

            Dictionary<(uint, uint), List<HandsProximity>> post = new Dictionary<(uint, uint), List<HandsProximity>>();
            foreach (var ids in list)
            {
                if (bodiesDics.ContainsKey(ids.Item1) && bodiesDics.ContainsKey(ids.Item2))
                {
                    Tuple<(uint, uint), List<HandsProximity>> detected;
                    if (this.ProcessBodies(bodiesDics[ids.Item1], bodiesDics[ids.Item2], out detected))
                    {
                        post.Add(detected.Item1, detected.Item2);
                    }
                }
            }

            if (post.Count > 0)
            {
                this.Out.Post(post, envelope.OriginatingTime);
            }
        }

        private bool ProcessBodies(in SimplifiedBody bodie1, in SimplifiedBody bodie2, out Tuple<(uint, uint), List<HandsProximity>> detected)
        {
            (Point3D, Point3D) handsB1, handsB2;
            if (!(this.ProcessHands(bodie1, out handsB1) && this.ProcessHands(bodie2, out handsB2)))
            {
                detected = new Tuple<(uint, uint), List<HandsProximity>>((bodie1.Id, bodie2.Id), new List<HandsProximity>());
                return false;
            }

            List<HandsProximity> list = new List<HandsProximity>();
            if (this.ProcessPoints(handsB1.Item1, handsB2.Item2))
            {
                list.Add(HandsProximity.LeftRight);
            }

            if (bodie1.Id != bodie2.Id)
            {
                if (this.ProcessPoints(handsB1.Item1, handsB2.Item1))
                {
                    list.Add(HandsProximity.LeftLeft);
                }

                if (this.ProcessPoints(handsB1.Item2, handsB2.Item1))
                {
                    list.Add(HandsProximity.RightLeft);
                }

                if (this.ProcessPoints(handsB1.Item2, handsB2.Item2))
                {
                    list.Add(HandsProximity.RightRight);
                }
            }

            detected = new Tuple<(uint, uint), List<HandsProximity>>((bodie1.Id, bodie2.Id), list);
            return true;
        }

        private bool ProcessHands(in SimplifiedBody bodie, out (Point3D, Point3D) left_right)
        {
            var handLeft = bodie.Joints[JointId.HandLeft];
            var handRight = bodie.Joints[JointId.HandRight];
            if (new[] { handLeft, handRight }.Select(j => j.Item1).Any(c => (int)c <= (int)this.configuration.MinimumConfidenceLevel))
            {
                left_right = (new Point3D(), new Point3D());
                return false;
            }

            left_right.Item1 = handLeft.Item2.ToPoint3D();
            left_right.Item2 = handRight.Item2.ToPoint3D();
            return true;
        }

        private bool ProcessPoints(in Point3D origin, in Point3D target)
        {
            return origin.DistanceTo(target) <= this.configuration.MinimumDistanceThreshold;
        }
    }
}
