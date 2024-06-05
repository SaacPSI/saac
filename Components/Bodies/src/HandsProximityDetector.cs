using MathNet.Spatial.Euclidean;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace SAAC.Bodies 
{
    /// <summary>
    /// Simple component that extract the position of a joint from given bodies .
    /// See SimpleBodiesPositionExtractionConfiguration for parameters details.
    /// </summary>
    public class HandsProximityDetector : IConsumerProducer<List<SimplifiedBody>, Dictionary<(uint, uint), List<HandsProximityDetector.HandsProximity>>>
    {
        /// <summary>
        /// Enumerator describing wich hands are close.
        /// </summary>
        public enum HandsProximity { LeftLeft, LeftRight, RightLeft, RightRight };

        /// <summary>
        /// Optionnal reciever that give which pair of bodies to check.
        /// </summary>
        private Connector<List<(uint, uint)>> InPairConnector;
        public Receiver<List<(uint, uint)>> InPair => InPairConnector.In;

        /// <summary>
        /// Reciever of bodies.
        /// </summary>
        private Connector<List<SimplifiedBody>> InConnector;
        public Receiver<List<SimplifiedBody>> In => InConnector.In;

        /// <summary>
        /// Emit if hands are close and send which ones.
        /// </summary>
        public Emitter<Dictionary<(uint, uint), List<HandsProximity>>> Out { get; }

        private HandsProximityDetectorConfiguration configuration;

        public HandsProximityDetector(Pipeline pipeline, HandsProximityDetectorConfiguration configuration = null)
        {
            this.configuration = configuration ?? new HandsProximityDetectorConfiguration();
            InConnector = pipeline.CreateConnector<List<SimplifiedBody>>(nameof(In));
            InPairConnector = pipeline.CreateConnector<List<(uint, uint)>>(nameof(InPair));
            Out = pipeline.CreateEmitter<Dictionary<(uint, uint), List<HandsProximity>>>(this, nameof(Out));
            if (this.configuration.IsPairToCheckGiven)
                InConnector.Out.Pair(InPairConnector, DeliveryPolicy.LatestMessage, DeliveryPolicy.LatestMessage).Do(Process);
            else
                InConnector.Out.Do(Process);
        }

        private void Process(List<SimplifiedBody> message, Envelope envelope)
        {
            if (message.Count == 0)
                return;

            Dictionary<(uint, uint), List<HandsProximity>> post = new Dictionary<(uint, uint), List<HandsProximity>>();
            foreach (var body1 in message)
            {
                foreach (var body2 in message)
                {
                    Tuple<(uint, uint), List<HandsProximity>> detected;
                    if (ProcessBodies(body1, body2, out detected))
                        post.Add(detected.Item1, detected.Item2); 
                }
            }
            if (post.Count > 0)
                Out.Post(post, envelope.OriginatingTime);
        }

        private void Process((List<SimplifiedBody>, List<(uint, uint)>) message, Envelope envelope)
        {
            var (bodies, list) = message;
            if (bodies.Count == 0 || list.Count == 0)
                return;
            Dictionary<uint, SimplifiedBody> bodiesDics = new Dictionary<uint,SimplifiedBody>();
            foreach (var body in bodies) 
                bodiesDics.Add(body.Id, body);

            Dictionary<(uint, uint), List<HandsProximity>> post = new Dictionary<(uint, uint), List<HandsProximity>>();
            foreach (var ids in list)
            {
                if(bodiesDics.ContainsKey(ids.Item1) && bodiesDics.ContainsKey(ids.Item2))
                {
                    Tuple<(uint, uint), List<HandsProximity>> detected;
                    if (ProcessBodies(bodiesDics[ids.Item1], bodiesDics[ids.Item2], out detected))
                        post.Add(detected.Item1, detected.Item2);
                }
            }
            if(post.Count > 0)
                Out.Post(post, envelope.OriginatingTime);
        }

        private bool ProcessBodies(in SimplifiedBody bodie1, in SimplifiedBody bodie2, out Tuple<(uint, uint), List<HandsProximity>> detected)
        {
            (Point3D, Point3D) handsB1, handsB2;
            if (!(ProcessHands(bodie1, out handsB1) && ProcessHands(bodie2, out handsB2)))
            {
                detected = new Tuple<(uint, uint), List<HandsProximity>>((bodie1.Id, bodie2.Id), new List<HandsProximity>());
                return false;
            }

            List<HandsProximity> list = new List<HandsProximity>();
            if (ProcessPoints(handsB1.Item1, handsB2.Item2))
                list.Add(HandsProximity.LeftRight);

            if (bodie1.Id != bodie2.Id)
            {
                if (ProcessPoints(handsB1.Item1, handsB2.Item1))
                    list.Add(HandsProximity.LeftLeft);
                if (ProcessPoints(handsB1.Item2, handsB2.Item1))
                    list.Add(HandsProximity.RightLeft);
                if (ProcessPoints(handsB1.Item2, handsB2.Item2))
                    list.Add(HandsProximity.RightRight);
            }
            detected = new Tuple<(uint, uint), List<HandsProximity>>((bodie1.Id, bodie2.Id), list);
            return true;
        }

        private bool ProcessHands(in SimplifiedBody bodie, out (Point3D, Point3D) left_right)
        {    
            var handLeft = bodie.Joints[JointId.HandLeft];
            var handRight = bodie.Joints[JointId.HandRight];
            if (new[] { handLeft, handRight}.Select(j => j.Item1).Any(c => (int)c <= (int)configuration.MinimumConfidenceLevel))
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
            return origin.DistanceTo(target) <= configuration.MinimumDistanceThreshold;
        }
    }
}
