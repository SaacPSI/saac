using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Azure.Kinect.BodyTracking;

namespace SAAC.Bodies
{
    // TODO : remove Subpipeline to consumer producer.
    public class BodiesSelection : Subpipeline
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<List<SimplifiedBody>> OutBodiesCalibrated{ get; private set; }

        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<List<uint>> OutBodiesRemoved { get; private set; }

        /// <summary>
        /// Gets the connector of the calibration matrix.
        /// </summary>
        private Connector<Matrix<double>> InCalibrationMatrixConnector;

        /// <summary>
        /// Receiver that encapsulates the calibration matrix input.
        /// </summary>
        public Receiver<Matrix<double>> InCalibrationMatrix => InCalibrationMatrixConnector.In;

        /// <summary>
        /// Gets the connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCamera1BodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of simplified skeletons from first camera.
        /// </summary>
        public Receiver<List<SimplifiedBody>> InCamera1Bodies => InCamera1BodiesConnector.In;

        /// <summary>
        /// Gets the connector of lists of currently tracked bodies of the second camera.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCamera2BodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list  of simplified skeletons from second camera
        /// </summary>
        public Receiver<List<SimplifiedBody>> InCamera2Bodies => InCamera2BodiesConnector.In;

        /// <summary>
        /// Gets the connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<LearnedBody>> InCamera1LearnedBodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of learned skeletons from first camera.
        /// </summary>
        public Receiver<List<LearnedBody>> InCamera1LearnedBodies => InCamera1LearnedBodiesConnector.In;

        /// <summary>
        /// Gets the connector of new learned bodies from second camera..
        /// </summary>
        private Connector<List<LearnedBody>> InCamera2LearnedBodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of learned skeletons from second camera.
        /// </summary>
        public Receiver<List<LearnedBody>> InCamera2LearnedBodies => InCamera2LearnedBodiesConnector.In;

        /// <summary>
        /// Gets the connector of lists of removed skeletons from first camera.
        /// </summary>
        private Connector<List<uint>> InCamera1RemovedBodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of removed skeletons
        /// </summary>
        public Receiver<List<uint>> InCamera1RemovedBodies => InCamera1RemovedBodiesConnector.In;

        /// <summary>
        /// Gets the nuitrack connector of lists of removed bodies.
        /// </summary>
        private Connector<List<uint>> InCamera2RemovedBodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of removed skeletons
        /// </summary>
        public Receiver<List<uint>> InCamera2RemovedBodies => InCamera2RemovedBodiesConnector.In;

        private BodiesSelectionConfiguration configuration;

        private Dictionary<(uint, uint), uint> generatedIdsMap = new Dictionary<(uint, uint), uint>();
        private Dictionary<(uint, uint), List<uint>> notPairable = new Dictionary<(uint, uint), List<uint>>();

        private Dictionary<uint, LearnedBody> camera1LearnedBodies = new Dictionary<uint, LearnedBody>();
        private Dictionary<uint, LearnedBody> camera2LearnedBodies = new Dictionary<uint, LearnedBody>();
        private uint idCount = 1;
        private enum TupleState { AlreadyExist, KeyAlreadyInserted, GoodToInsert, Replace  };

        public BodiesSelection(Pipeline parent, BodiesSelectionConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
          : base(parent, name, defaultDeliveryPolicy)
        {
            this.configuration = configuration ?? new BodiesSelectionConfiguration();

            InCamera1BodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCamera1BodiesConnector));
            InCamera2BodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCamera2BodiesConnector));
            InCamera1LearnedBodiesConnector = CreateInputConnectorFrom<List<LearnedBody>>(parent, nameof(InCamera1LearnedBodiesConnector));
            InCamera2LearnedBodiesConnector = CreateInputConnectorFrom<List<LearnedBody>>(parent, nameof(InCamera2LearnedBodiesConnector));
            InCamera1RemovedBodiesConnector = CreateInputConnectorFrom<List<uint>>(parent, nameof(InCamera1RemovedBodiesConnector));
            InCamera2RemovedBodiesConnector = CreateInputConnectorFrom<List<uint>>(parent, nameof(InCamera2RemovedBodiesConnector));
            InCalibrationMatrixConnector = CreateInputConnectorFrom<Matrix<double>>(parent, nameof(InCalibrationMatrixConnector));
            OutBodiesCalibrated = parent.CreateEmitter<List<SimplifiedBody>>(this, nameof(OutBodiesCalibrated));
            OutBodiesRemoved = parent.CreateEmitter<List<uint>>(this, nameof(OutBodiesRemoved));

            if (this.configuration.Camera2ToCamera1Transformation == null)
                InCamera1BodiesConnector.Pair(InCamera2BodiesConnector).Out.Fuse(InCalibrationMatrixConnector.Out, Available.Nearest<Matrix<double>>()).Do(Process);
            else
                InCamera1BodiesConnector.Pair(InCamera2BodiesConnector).Do(Process);

            InCamera1LearnedBodiesConnector.Do(LearnedBodyProcessing1);
            InCamera2LearnedBodiesConnector.Do(LearnedBodyProcessing2);

            InCamera1RemovedBodiesConnector.Do(RemovedBodyProcessing1);
            InCamera2RemovedBodiesConnector.Do(RemovedBodyProcessing2);
        }
        private void Process((List<SimplifiedBody>, List<SimplifiedBody>, Matrix<double>) bodies, Envelope envelope)
        {
            configuration.Camera2ToCamera1Transformation = bodies.Item3;
            Process((bodies.Item1, bodies.Item2), envelope);
        }

        private void Process((List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            Dictionary<uint, SimplifiedBody> dicsC1 = new Dictionary<uint, SimplifiedBody>(), dicsC2 = new Dictionary<uint, SimplifiedBody>();
            UpdateCorrespondanceMap(bodies.Item1, bodies.Item2, ref dicsC1, ref dicsC2, envelope.OriginatingTime);
            var bbody = SelectBestBody(dicsC1, dicsC2);
            OutBodiesCalibrated.Post(bbody, envelope.OriginatingTime);
            //OutBodiesCalibrated.Post(SelectBestBody(dicsC1, dicsC2), envelope.OriginatingTime);
        }

        private void LearnedBodyProcessing1(List<LearnedBody> list, Envelope envelope)
        {
            LearnedBodyProcessing(list, ref camera1LearnedBodies);
        }
        private void LearnedBodyProcessing2(List<LearnedBody> list, Envelope envelope)
        {
            LearnedBodyProcessing(list, ref camera2LearnedBodies);
        }
        private void LearnedBodyProcessing(List<LearnedBody> list, ref Dictionary<uint, LearnedBody> dic)
        {
            lock (this)
            {
                foreach (var item in list)
                {
                    if (dic.ContainsKey(item.Id))
                        continue;
                    dic.Add(item.Id, item);
                }
            }
        }

        private void RemovedBodyProcessing1(List<uint> list, Envelope envelope)
        {
            RemovedBodyProcessing(list, true, ref camera1LearnedBodies, envelope);
        }

        private void RemovedBodyProcessing2(List<uint> list, Envelope envelope)
        {
            RemovedBodyProcessing(list, false, ref camera2LearnedBodies, envelope);
        }

        private void RemovedBodyProcessing(List<uint> list, bool isMaster, ref Dictionary<uint, LearnedBody> dic, Envelope envelope)
        {
            lock(this)
            {
                List<uint> removedId = new List<uint>();
                foreach (uint id in list)
                {
                    if (dic.ContainsKey(id))
                        dic.Remove(id);
                    (uint, uint) tuple, ouTuple;
                    if (isMaster)
                        tuple = (id, 0);
                    else
                        tuple = (0, id);
                    if (KeyOrValueExistInList(tuple, out ouTuple) != 0)
                    {
                        removedId.Add(generatedIdsMap[(ouTuple.Item1, ouTuple.Item2)]);
                        generatedIdsMap.Remove((ouTuple.Item1, ouTuple.Item2));
                    }
                }
                OutBodiesRemoved.Post(removedId, envelope.OriginatingTime);
            }
        }

        private void UpdateCorrespondanceMap(List<SimplifiedBody> camera1, List<SimplifiedBody> camera2, ref Dictionary<uint, SimplifiedBody> d1, ref Dictionary<uint, SimplifiedBody> d2, DateTime time)
        {
            var newMapping = ComputeCorrespondenceMap(camera1, camera2, ref d1, ref d2);

            //checking consistancy with old mapping
            foreach (var iterator in newMapping)
            {
                (uint, uint) tuple;
                switch (KeyOrValueExistInList(iterator, out tuple))
                {
                    case TupleState.AlreadyExist:
                        break;
                    case TupleState.KeyAlreadyInserted:
                        FindCorrectPairFromBones(ref d1, ref d2, iterator, tuple, time);
                        break;
                    case TupleState.GoodToInsert:
                        generatedIdsMap[(iterator.Item1, iterator.Item2)] = idCount++;
                        break;
                    case TupleState.Replace:
                        IntegrateInDicsAndList(tuple, iterator, time);
                        break;
                }
            }
        }

        private void IntegrateInDicsAndList((uint, uint) old, (uint, uint) newItem, DateTime time)
        {
            if (old == newItem)
                return;

            if (generatedIdsMap.ContainsKey((old.Item1, old.Item2)))
            {
                generatedIdsMap[(newItem.Item1, newItem.Item2)] = generatedIdsMap[(old.Item1, old.Item2)];
                generatedIdsMap.Remove((old.Item1, old.Item2));
                if (generatedIdsMap.ContainsKey((newItem.Item1, 0)))
                {
                    List<uint> removedId = new List<uint>();
                    removedId.Add(generatedIdsMap[(newItem.Item1, 0)]);
                    OutBodiesRemoved.Post(removedId, time);
                    generatedIdsMap.Remove((newItem.Item1, 0));
                }
                if (generatedIdsMap.ContainsKey((0, newItem.Item2)))
                {
                    List<uint> removedId = new List<uint>();
                    removedId.Add(generatedIdsMap[(0, newItem.Item2)]);
                    OutBodiesRemoved.Post(removedId, time);
                    generatedIdsMap.Remove((0, newItem.Item2));
                }
            }
            else if(!generatedIdsMap.ContainsKey((newItem.Item1, newItem.Item2)))
                generatedIdsMap[(newItem.Item1, newItem.Item2)] = idCount++;
        }

        private void FindCorrectPairFromBones(ref Dictionary<uint, SimplifiedBody> d1, ref Dictionary<uint, SimplifiedBody> d2, (uint, uint) iterator, (uint, uint) tuple, DateTime time)
        {
            LearnedBody unique, p1, p2;
            if (iterator.Item1 == tuple.Item1)
            {
                p1 = camera2LearnedBodies[iterator.Item2];
                p2 = camera2LearnedBodies[tuple.Item2];
                unique = camera1LearnedBodies[iterator.Item1];
            }
            else if (iterator.Item2 == tuple.Item2)
            {
                p1 = camera1LearnedBodies[iterator.Item1];
                p2 = camera1LearnedBodies[tuple.Item1];
                unique = camera2LearnedBodies[iterator.Item2];
            }
            else
            {
                throw new Exception("oups");
            }

            List<double> dist1 = new List<double>(), dist2 = new List<double>();
            foreach (var bones in unique.LearnedBones)
            {
                if (bones.Value == 0.0)
                    continue;
                if (p1.LearnedBones[bones.Key] > 0.0)
                    dist1.Add(Math.Abs(p1.LearnedBones[bones.Key] - bones.Value));
                if (p2.LearnedBones[bones.Key] > 0.0)
                    dist2.Add(Math.Abs(p2.LearnedBones[bones.Key] - bones.Value));
            }

            var statistics1 = MathNet.Numerics.Statistics.Statistics.MeanStandardDeviation(dist1);
            var statistics2 = MathNet.Numerics.Statistics.Statistics.MeanStandardDeviation(dist2);
            
            if (statistics1.Item2 < statistics2.Item2)
                IntegrateInDicsAndList(tuple, iterator, time);
            else
                IntegrateInDicsAndList(iterator, tuple, time);
        }

        private List<(uint, uint)> ComputeCorrespondenceMap(List<SimplifiedBody> camera1, List<SimplifiedBody> camera2, ref Dictionary<uint, SimplifiedBody> d1, ref Dictionary<uint, SimplifiedBody> d2) 
        {
            if ((camera1.Count == 0 && camera2.Count == 0) || configuration.Camera2ToCamera1Transformation == null) 
                return new List<(uint, uint)>();

            // Bruteforce ftm, might simplify to check directly the max allowed distance.
            Dictionary<uint, List<Tuple<double, uint>>> distances = new Dictionary<uint, List<Tuple<double, uint>>>();

            foreach (SimplifiedBody bodyC1 in camera1)
            {
                d1[bodyC1.Id] = bodyC1;
                distances[bodyC1.Id] = new List<Tuple<double, uint>>();
                foreach (SimplifiedBody bodyC2 in camera2)
                    if(!notPairable.ContainsKey((bodyC1.Id,0)) || !notPairable[(bodyC1.Id, 0)].Contains(bodyC2.Id))
                        distances[bodyC1.Id].Add(new Tuple<double, uint>(MathNet.Numerics.Distance.Euclidean(bodyC1.Joints[configuration.JointUsedForCorrespondence].Item2.ToVector(), Helpers.Helpers.CalculateTransform(bodyC2.Joints[configuration.JointUsedForCorrespondence].Item2, configuration.Camera2ToCamera1Transformation).ToVector()), bodyC2.Id));
            }

            List<(uint, uint)> correspondanceMap = new List<(uint, uint)>();
            List<uint> notMissingC2 = new List<uint>();
            foreach(var iterator in distances)
            { 
                iterator.Value.Sort(new TupleDoubleUintComparer());
                //to check if sort is good
                if (iterator.Value.Count > 1)
                {
                    if (iterator.Value.First().Item1 < configuration.MaxDistance)
                    {
                        correspondanceMap.Add((iterator.Key, iterator.Value.First().Item2));
                        notMissingC2.Add(iterator.Value.First().Item2);
                    }
                    foreach (var pair in iterator.Value)
                    {
                        if (iterator.Value.First().Item1 > configuration.NotPairableDistanceThreshold)
                        {
                            if (!notPairable.ContainsKey((iterator.Key, 0)))
                                notPairable.Add((iterator.Key, 0), new List<uint>());
                            notPairable[(iterator.Key, 0)].Add(pair.Item2);
                            if (!notPairable.ContainsKey((0, pair.Item2)))
                                notPairable.Add((0, pair.Item2), new List<uint>());
                            notPairable[(0, pair.Item2)].Add(iterator.Key);
                        }
                    }
                }
                else
                    correspondanceMap.Add((iterator.Key, 0));
            }
            foreach(SimplifiedBody bodyC2 in camera2)
            {
                d2[bodyC2.Id] = bodyC2;
                if (!notMissingC2.Contains(bodyC2.Id))
                    correspondanceMap.Add((0, bodyC2.Id));
            }
            return correspondanceMap;
        }

        private void SelectByConfidence(Dictionary<uint, SimplifiedBody> camera1, Dictionary<uint, SimplifiedBody> camera2, (uint,uint) ids, ref List<SimplifiedBody> bestBodies)
        {
            if (AccumulatedConfidence(camera1[ids.Item1]) < AccumulatedConfidence(camera2[ids.Item2]))
            {
                SimplifiedBody body = camera1[ids.Item1];
                body.Id = generatedIdsMap[(ids.Item1, ids.Item2)];
                bestBodies.Add(body);
            }
            else
            {
                SimplifiedBody body = camera2[ids.Item2];
                body.Id = generatedIdsMap[(ids.Item1, ids.Item2)];
                bestBodies.Add(TransformBody(body));
            }
        }
        private void SelectByLearnedBodies(Dictionary<uint, SimplifiedBody> camera1, Dictionary<uint, SimplifiedBody> camera2, (uint, uint) ids, ref List<SimplifiedBody> bestBodies)
        {
            SimplifiedBody b1 = camera1[ids.Item1], b2 = camera2[ids.Item2];
            LearnedBody l1 = camera1LearnedBodies[ids.Item1], l2 = camera2LearnedBodies[ids.Item2];

            List<double> dist1 = new List<double>(), dist2 = new List<double>();
            lock (this)
            {
                foreach (var bones in l1.LearnedBones)
                {
                    if (!l2.LearnedBones.ContainsKey(bones.Key))
                        continue;
                    if (bones.Value > 0.0)
                        dist1.Add(Math.Abs(MathNet.Numerics.Distance.Euclidean(b1.Joints[bones.Key.ParentJoint].Item2.ToVector(), b1.Joints[bones.Key.ChildJoint].Item2.ToVector()) - bones.Value));
                    if (l2.LearnedBones[bones.Key] > 0.0)
                        dist2.Add(Math.Abs(MathNet.Numerics.Distance.Euclidean(b2.Joints[bones.Key.ParentJoint].Item2.ToVector(), b2.Joints[bones.Key.ChildJoint].Item2.ToVector()) - l2.LearnedBones[bones.Key]));
                }
            }
            var statistics1 = MathNet.Numerics.Statistics.Statistics.MeanStandardDeviation(dist1);
            var statistics2 = MathNet.Numerics.Statistics.Statistics.MeanStandardDeviation(dist2);
            if (statistics1.Item2 < statistics2.Item2)
            {
                SimplifiedBody body = camera1[ids.Item1];
                body.Id = generatedIdsMap[(ids.Item1, ids.Item2)];
                bestBodies.Add(body);
            }
            else
            {
                SimplifiedBody body = camera2[ids.Item2];
                body.Id = generatedIdsMap[(ids.Item1, ids.Item2)];
                bestBodies.Add(TransformBody(body));
            }
        }

        private List<SimplifiedBody> SelectBestBody(Dictionary<uint, SimplifiedBody> camera1, Dictionary<uint, SimplifiedBody> camera2)
        {
            List<SimplifiedBody> bestBodies = new List<SimplifiedBody>();
            foreach(var pair in generatedIdsMap)
            {
                if (camera1.ContainsKey(pair.Key.Item1) && camera2.ContainsKey(pair.Key.Item2))
                {
                    if(camera1LearnedBodies.ContainsKey(pair.Key.Item1) && camera1LearnedBodies.ContainsKey(pair.Key.Item2))
                        SelectByLearnedBodies(camera1, camera2, pair.Key, ref bestBodies);
                    else
                        SelectByConfidence(camera1, camera2, pair.Key, ref bestBodies);
                }
                else if (pair.Key.Item1 == 0 || !camera1.ContainsKey(pair.Key.Item1))
                {
                    if (!camera2.ContainsKey(pair.Key.Item2))
                        continue;
                    (uint, uint) tuple;
                    var enumT = KeyOrValueExistInList(pair.Key, out tuple);
                    switch (enumT)
                    {
                        case TupleState.KeyAlreadyInserted:
                        case TupleState.GoodToInsert:
                            throw new Exception("oups");
                        case TupleState.AlreadyExist:
                        case TupleState.Replace:
                            break;
                    }
                    SimplifiedBody simplifiedBody = TransformBody(camera2[pair.Key.Item2]);
                    simplifiedBody.Id = generatedIdsMap[(tuple.Item1, tuple.Item2)];
                    bestBodies.Add(simplifiedBody);
                }
                else if (pair.Key.Item2 == 0 || !camera2.ContainsKey(pair.Key.Item2))
                {
                    if (!camera1.ContainsKey(pair.Key.Item1))
                        continue;
                    (uint, uint) tuple;
                    var enumT = KeyOrValueExistInList(pair.Key, out tuple);
                    switch (enumT)
                    {
                        case TupleState.KeyAlreadyInserted:
                        case TupleState.GoodToInsert:
                            throw new Exception("oups");
                        case TupleState.AlreadyExist:
                        case TupleState.Replace:
                            break;
                    }
                    SimplifiedBody simplifiedBody = camera1[pair.Key.Item1];
                    simplifiedBody.Id = generatedIdsMap[(tuple.Item1, tuple.Item2)];
                    bestBodies.Add(simplifiedBody);
                }
            }
            return bestBodies;
        } 
        
        private TupleState KeyOrValueExistInList((uint, uint) tuple, out (uint, uint) value)
        {
            bool checkTupleItem1 = tuple.Item1 != 0;
            bool checkTupleItem2 = tuple.Item2 != 0;
            foreach (var iterator in generatedIdsMap)
            {
                bool checkIteratorItem1 = iterator.Key.Item1 != 0;
                bool checkIteratorItem2 = iterator.Key.Item2 != 0;
                bool checkSameItem1 = iterator.Key.Item1 == tuple.Item1;
                bool checkSameItem2 = iterator.Key.Item2 == tuple.Item2;
                if (generatedIdsMap.ContainsKey(tuple) || (!checkTupleItem1 && checkSameItem2) || (checkSameItem1 && !checkTupleItem2))
                {
                    value = tuple;
                    return TupleState.AlreadyExist;
                }
                else if((!checkIteratorItem1 && checkSameItem2) || (checkSameItem1 && !checkIteratorItem2))
                {
                    value = iterator.Key;
                    return TupleState.Replace;
                }
                else if (checkTupleItem1 && checkTupleItem2 && checkIteratorItem1 && checkIteratorItem2 && (checkSameItem2 || checkSameItem1))
                {
                    value = iterator.Key;
                    return TupleState.KeyAlreadyInserted;
                }
            }
            value = tuple;
            return TupleState.GoodToInsert;
        }

        private double AccumulatedConfidence(SimplifiedBody body)
        {
            //might use coef for usefull joints.
            int accumulator = 0;
            foreach(var joint in body.Joints)
                accumulator+=(int)joint.Value.Item1;
            return accumulator;
        }

        private SimplifiedBody TransformBody(SimplifiedBody body)
        {
            if(configuration.Camera2ToCamera1Transformation == null)
                return body;
            SimplifiedBody transformed = body.DeepClone();
            foreach (var joint in body.Joints)
                transformed.Joints[joint.Key] = new Tuple<JointConfidenceLevel, Vector3D>(joint.Value.Item1, Helpers.Helpers.CalculateTransform(joint.Value.Item2, configuration.Camera2ToCamera1Transformation));
            return transformed;
        }
    }

    internal class TupleDoubleUintComparer : Comparer<Tuple<double, uint>>
    {
        public override int Compare(Tuple<double, uint> a, Tuple<double, uint> b)
        {
            if(a.Item1 == b.Item1)
                return 0;
            return a.Item1 > b.Item1 ? 1 : -1;
        }
    }
}
