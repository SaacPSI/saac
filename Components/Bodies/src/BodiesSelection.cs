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
    /// Component that selects and merges body detections from two cameras into a unified view.
    /// </summary>
    public class BodiesSelection
    {
        /// <summary>
        /// Gets the emitter of calibrated bodies.
        /// </summary>
        public Emitter<List<SimplifiedBody>> OutBodiesCalibrated { get; private set; }

        /// <summary>
        /// Gets the emitter of removed body IDs.
        /// </summary>
        public Emitter<List<uint>> OutBodiesRemoved { get; private set; }

        private Connector<CoordinateSystem> inCalibrationMatrixConnector;

        /// <summary>
        /// Gets the receiver that encapsulates the calibration matrix input.
        /// </summary>
        public Receiver<CoordinateSystem> InCalibrationMatrix => this.inCalibrationMatrixConnector.In;

        private Connector<List<SimplifiedBody>> inCamera1BodiesConnector;

        /// <summary>
        /// Gets the receiver that encapsulates the input list of simplified skeletons from first camera.
        /// </summary>
        public Receiver<List<SimplifiedBody>> InCamera1Bodies => this.inCamera1BodiesConnector.In;

        private Connector<List<SimplifiedBody>> inCamera2BodiesConnector;

        /// <summary>
        /// Gets the receiver that encapsulates the input list of simplified skeletons from second camera.
        /// </summary>
        public Receiver<List<SimplifiedBody>> InCamera2Bodies => this.inCamera2BodiesConnector.In;

        private Connector<List<LearnedBody>> inCamera1LearnedBodiesConnector;

        /// <summary>
        /// Gets the receiver that encapsulates the input list of learned skeletons from first camera.
        /// </summary>
        public Receiver<List<LearnedBody>> InCamera1LearnedBodies => this.inCamera1LearnedBodiesConnector.In;

        private Connector<List<LearnedBody>> inCamera2LearnedBodiesConnector;

        /// <summary>
        /// Gets the receiver that encapsulates the input list of learned skeletons from second camera.
        /// </summary>
        public Receiver<List<LearnedBody>> InCamera2LearnedBodies => this.inCamera2LearnedBodiesConnector.In;

        private Connector<List<uint>> inCamera1RemovedBodiesConnector;

        /// <summary>
        /// Gets the receiver that encapsulates the input list of removed skeletons from first camera.
        /// </summary>
        public Receiver<List<uint>> InCamera1RemovedBodies => this.inCamera1RemovedBodiesConnector.In;

        private Connector<List<uint>> inCamera2RemovedBodiesConnector;

        /// <summary>
        /// Gets the receiver that encapsulates the input list of removed skeletons from second camera.
        /// </summary>
        public Receiver<List<uint>> InCamera2RemovedBodies => this.inCamera2RemovedBodiesConnector.In;

        private readonly BodiesSelectionConfiguration configuration;
        private readonly Dictionary<(uint, uint), uint> generatedIdsMap = new Dictionary<(uint, uint), uint>();
        private readonly Dictionary<(uint, uint), List<uint>> notPairable = new Dictionary<(uint, uint), List<uint>>();
        private Dictionary<uint, LearnedBody> camera1LearnedBodies = new Dictionary<uint, LearnedBody>();
        private Dictionary<uint, LearnedBody> camera2LearnedBodies = new Dictionary<uint, LearnedBody>();
        private readonly string name;
        private uint idCount = 1;

        /// <summary>
        /// Enumeration of tuple states for correspondence mapping.
        /// </summary>
        private enum TupleState
        {
            /// <summary>Already exists in the map.</summary>
            AlreadyExist,

            /// <summary>Key already inserted.</summary>
            KeyAlreadyInserted,

            /// <summary>Good to insert.</summary>
            GoodToInsert,

            /// <summary>Should replace existing entry.</summary>
            Replace
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BodiesSelection"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration for body selection.</param>
        /// <param name="name">Optional name for the component.</param>
        public BodiesSelection(Pipeline parent, BodiesSelectionConfiguration? configuration = null, string name = nameof(BodiesSelection))
        {
            this.name = name;
            this.configuration = configuration ?? new BodiesSelectionConfiguration();

            this.inCamera1BodiesConnector = parent.CreateConnector<List<SimplifiedBody>>($"{name}-InCamera1BodiesConnector");
            this.inCamera2BodiesConnector = parent.CreateConnector<List<SimplifiedBody>>($"{name}-InCamera2BodiesConnector");
            this.inCamera1LearnedBodiesConnector = parent.CreateConnector<List<LearnedBody>>($"{name}-InCamera1LearnedBodiesConnector");
            this.inCamera2LearnedBodiesConnector = parent.CreateConnector<List<LearnedBody>>($"{name}-InCamera2LearnedBodiesConnector");
            this.inCamera1RemovedBodiesConnector = parent.CreateConnector<List<uint>>($"{name}-InCamera1RemovedBodiesConnector");
            this.inCamera2RemovedBodiesConnector = parent.CreateConnector<List<uint>>($"{name}-InCamera2RemovedBodiesConnector");
            this.inCalibrationMatrixConnector = parent.CreateConnector<CoordinateSystem>($"{name}-InCalibrationMatrixConnector");
            this.OutBodiesCalibrated = parent.CreateEmitter<List<SimplifiedBody>>(this, $"{name}-OutBodiesCalibrated");
            this.OutBodiesRemoved = parent.CreateEmitter<List<uint>>(this, $"{name}-OutBodiesRemoved");

            if (this.configuration.Camera2ToCamera1Transformation == null)
            {
                this.inCamera1BodiesConnector.Pair(this.inCamera2BodiesConnector).Out.Fuse(this.inCalibrationMatrixConnector.Out, Available.Nearest<CoordinateSystem>()).Do(this.Process);
            }
            else
            {
                this.inCamera1BodiesConnector.Pair(this.inCamera2BodiesConnector).Do(this.Process);
            }

            this.inCamera1LearnedBodiesConnector.Do(this.LearnedBodyProcessing1);
            this.inCamera2LearnedBodiesConnector.Do(this.LearnedBodyProcessing2);

            this.inCamera1RemovedBodiesConnector.Do(this.RemovedBodyProcessing1);
            this.inCamera2RemovedBodiesConnector.Do(this.RemovedBodyProcessing2);
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process((List<SimplifiedBody>, List<SimplifiedBody>, CoordinateSystem) bodies, Envelope envelope)
        {
            this.configuration.Camera2ToCamera1Transformation = bodies.Item3;
            this.Process((bodies.Item1, bodies.Item2), envelope);
        }

        private void Process((List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            Dictionary<uint, SimplifiedBody> dicsC1 = new Dictionary<uint, SimplifiedBody>(), dicsC2 = new Dictionary<uint, SimplifiedBody>();
            this.UpdateCorrespondanceMap(bodies.Item1, bodies.Item2, ref dicsC1, ref dicsC2, envelope.OriginatingTime);
            var bbody = this.SelectBestBody(dicsC1, dicsC2);
            this.OutBodiesCalibrated.Post(bbody, envelope.OriginatingTime);
        }

        private void LearnedBodyProcessing1(List<LearnedBody> list, Envelope envelope)
        {
            this.LearnedBodyProcessing(list, ref this.camera1LearnedBodies);
        }

        private void LearnedBodyProcessing2(List<LearnedBody> list, Envelope envelope)
        {
            this.LearnedBodyProcessing(list, ref this.camera2LearnedBodies);
        }

        private void LearnedBodyProcessing(List<LearnedBody> list, ref Dictionary<uint, LearnedBody> dic)
        {
            lock (this)
            {
                foreach (var item in list)
                {
                    if (dic.ContainsKey(item.Id))
                    {
                        continue;
                    }

                    dic.Add(item.Id, item);
                }
            }
        }

        private void RemovedBodyProcessing1(List<uint> list, Envelope envelope)
        {
            this.RemovedBodyProcessing(list, true, ref this.camera1LearnedBodies, envelope);
        }

        private void RemovedBodyProcessing2(List<uint> list, Envelope envelope)
        {
            this.RemovedBodyProcessing(list, false, ref this.camera2LearnedBodies, envelope);
        }

        private void RemovedBodyProcessing(List<uint> list, bool isMaster, ref Dictionary<uint, LearnedBody> dic, Envelope envelope)
        {
            lock (this)
            {
                List<uint> removedId = new List<uint>();
                foreach (uint id in list)
                {
                    if (dic.ContainsKey(id))
                    {
                        dic.Remove(id);
                    }

                    (uint, uint) tuple, ouTuple;
                    if (isMaster)
                    {
                        tuple = (id, 0);
                    }
                    else
                    {
                        tuple = (0, id);
                    }

                    if (this.KeyOrValueExistInList(tuple, out ouTuple) != 0)
                    {
                        removedId.Add(this.generatedIdsMap[(ouTuple.Item1, ouTuple.Item2)]);
                        this.generatedIdsMap.Remove((ouTuple.Item1, ouTuple.Item2));
                    }
                }

                this.OutBodiesRemoved.Post(removedId, envelope.OriginatingTime);
            }
        }

        private void UpdateCorrespondanceMap(List<SimplifiedBody> camera1, List<SimplifiedBody> camera2, ref Dictionary<uint, SimplifiedBody> d1, ref Dictionary<uint, SimplifiedBody> d2, DateTime time)
        {
            var newMapping = this.ComputeCorrespondenceMap(camera1, camera2, ref d1, ref d2);

            foreach (var iterator in newMapping)
            {
                (uint, uint) tuple;
                switch (this.KeyOrValueExistInList(iterator, out tuple))
                {
                    case TupleState.AlreadyExist:
                        break;
                    case TupleState.KeyAlreadyInserted:
                        this.FindCorrectPairFromBones(ref d1, ref d2, iterator, tuple, time);
                        break;
                    case TupleState.GoodToInsert:
                        this.generatedIdsMap[(iterator.Item1, iterator.Item2)] = this.idCount++;
                        break;
                    case TupleState.Replace:
                        this.IntegrateInDicsAndList(tuple, iterator, time);
                        break;
                }
            }
        }

        private void IntegrateInDicsAndList((uint, uint) old, (uint, uint) newItem, DateTime time)
        {
            if (old == newItem)
            {
                return;
            }

            if (this.generatedIdsMap.ContainsKey((old.Item1, old.Item2)))
            {
                this.generatedIdsMap[(newItem.Item1, newItem.Item2)] = this.generatedIdsMap[(old.Item1, old.Item2)];
                this.generatedIdsMap.Remove((old.Item1, old.Item2));
                if (this.generatedIdsMap.ContainsKey((newItem.Item1, 0)))
                {
                    List<uint> removedId = new List<uint>();
                    removedId.Add(this.generatedIdsMap[(newItem.Item1, 0)]);
                    this.OutBodiesRemoved.Post(removedId, time);
                    this.generatedIdsMap.Remove((newItem.Item1, 0));
                }

                if (this.generatedIdsMap.ContainsKey((0, newItem.Item2)))
                {
                    List<uint> removedId = new List<uint>();
                    removedId.Add(this.generatedIdsMap[(0, newItem.Item2)]);
                    this.OutBodiesRemoved.Post(removedId, time);
                    this.generatedIdsMap.Remove((0, newItem.Item2));
                }
            }
            else if (!this.generatedIdsMap.ContainsKey((newItem.Item1, newItem.Item2)))
            {
                this.generatedIdsMap[(newItem.Item1, newItem.Item2)] = this.idCount++;
            }
        }

        private void FindCorrectPairFromBones(ref Dictionary<uint, SimplifiedBody> d1, ref Dictionary<uint, SimplifiedBody> d2, (uint, uint) iterator, (uint, uint) tuple, DateTime time)
        {
            LearnedBody unique, p1, p2;
            if (iterator.Item1 == tuple.Item1)
            {
                p1 = this.camera2LearnedBodies[iterator.Item2];
                p2 = this.camera2LearnedBodies[tuple.Item2];
                unique = this.camera1LearnedBodies[iterator.Item1];
            }
            else if (iterator.Item2 == tuple.Item2)
            {
                p1 = this.camera1LearnedBodies[iterator.Item1];
                p2 = this.camera1LearnedBodies[tuple.Item1];
                unique = this.camera2LearnedBodies[iterator.Item2];
            }
            else
            {
                throw new Exception("oups");
            }

            List<double> dist1 = new List<double>(), dist2 = new List<double>();
            foreach (var bones in unique.LearnedBones)
            {
                if (bones.Value == 0.0)
                {
                    continue;
                }

                if (p1.LearnedBones[bones.Key] > 0.0)
                {
                    dist1.Add(Math.Abs(p1.LearnedBones[bones.Key] - bones.Value));
                }

                if (p2.LearnedBones[bones.Key] > 0.0)
                {
                    dist2.Add(Math.Abs(p2.LearnedBones[bones.Key] - bones.Value));
                }
            }

            var statistics1 = MathNet.Numerics.Statistics.Statistics.MeanStandardDeviation(dist1);
            var statistics2 = MathNet.Numerics.Statistics.Statistics.MeanStandardDeviation(dist2);

            if (statistics1.Item2 < statistics2.Item2)
            {
                this.IntegrateInDicsAndList(tuple, iterator, time);
            }
            else
            {
                this.IntegrateInDicsAndList(iterator, tuple, time);
            }
        }

        private List<(uint, uint)> ComputeCorrespondenceMap(List<SimplifiedBody> camera1, List<SimplifiedBody> camera2, ref Dictionary<uint, SimplifiedBody> d1, ref Dictionary<uint, SimplifiedBody> d2)
        {
            if ((camera1.Count == 0 && camera2.Count == 0) || this.configuration.Camera2ToCamera1Transformation == null)
            {
                return new List<(uint, uint)>();
            }

            // Bruteforce ftm, might simplify to check directly the max allowed distance.
            Dictionary<uint, List<Tuple<double, uint>>> distances = new Dictionary<uint, List<Tuple<double, uint>>>();

            foreach (SimplifiedBody bodyC1 in camera1)
            {
                d1[bodyC1.Id] = bodyC1;
                distances[bodyC1.Id] = new List<Tuple<double, uint>>();
                foreach (SimplifiedBody bodyC2 in camera2)
                {
                    if (!this.notPairable.ContainsKey((bodyC1.Id, 0)) || !this.notPairable[(bodyC1.Id, 0)].Contains(bodyC2.Id))
                    {
                        distances[bodyC1.Id].Add(new Tuple<double, uint>(MathNet.Numerics.Distance.Euclidean(bodyC1.Joints[this.configuration.JointUsedForCorrespondence].Item2.ToVector(), this.configuration.Camera2ToCamera1Transformation.Transform(bodyC2.Joints[this.configuration.JointUsedForCorrespondence].Item2).ToVector()), bodyC2.Id));
                    }
                }
            }

            List<(uint, uint)> correspondanceMap = new List<(uint, uint)>();
            List<uint> notMissingC2 = new List<uint>();
            foreach (var iterator in distances)
            {
                iterator.Value.Sort(new TupleDoubleUintComparer());

                // To check if sort is good
                if (iterator.Value.Count > 1)
                {
                    if (iterator.Value.First().Item1 < this.configuration.MaxDistance)
                    {
                        correspondanceMap.Add((iterator.Key, iterator.Value.First().Item2));
                        notMissingC2.Add(iterator.Value.First().Item2);
                    }

                    foreach (var pair in iterator.Value)
                    {
                        if (iterator.Value.First().Item1 > this.configuration.NotPairableDistanceThreshold)
                        {
                            if (!this.notPairable.ContainsKey((iterator.Key, 0)))
                            {
                                this.notPairable.Add((iterator.Key, 0), new List<uint>());
                            }

                            this.notPairable[(iterator.Key, 0)].Add(pair.Item2);
                            if (!this.notPairable.ContainsKey((0, pair.Item2)))
                            {
                                this.notPairable.Add((0, pair.Item2), new List<uint>());
                            }

                            this.notPairable[(0, pair.Item2)].Add(iterator.Key);
                        }
                    }
                }
                else
                {
                    correspondanceMap.Add((iterator.Key, 0));
                }
            }

            foreach (SimplifiedBody bodyC2 in camera2)
            {
                d2[bodyC2.Id] = bodyC2;
                if (!notMissingC2.Contains(bodyC2.Id))
                {
                    correspondanceMap.Add((0, bodyC2.Id));
                }
            }

            return correspondanceMap;
        }

        private void SelectByConfidence(Dictionary<uint, SimplifiedBody> camera1, Dictionary<uint, SimplifiedBody> camera2, (uint, uint) ids, ref List<SimplifiedBody> bestBodies)
        {
            if (this.AccumulatedConfidence(camera1[ids.Item1]) < this.AccumulatedConfidence(camera2[ids.Item2]))
            {
                SimplifiedBody body = camera1[ids.Item1];
                body.Id = this.generatedIdsMap[(ids.Item1, ids.Item2)];
                bestBodies.Add(body);
            }
            else
            {
                SimplifiedBody body = camera2[ids.Item2];
                body.Id = this.generatedIdsMap[(ids.Item1, ids.Item2)];
                bestBodies.Add(this.TransformBody(body));
            }
        }

        private void SelectByLearnedBodies(Dictionary<uint, SimplifiedBody> camera1, Dictionary<uint, SimplifiedBody> camera2, (uint, uint) ids, ref List<SimplifiedBody> bestBodies)
        {
            SimplifiedBody b1 = camera1[ids.Item1], b2 = camera2[ids.Item2];
            LearnedBody l1 = this.camera1LearnedBodies[ids.Item1], l2 = this.camera2LearnedBodies[ids.Item2];

            List<double> dist1 = new List<double>(), dist2 = new List<double>();
            lock (this)
            {
                foreach (var bones in l1.LearnedBones)
                {
                    if (!l2.LearnedBones.ContainsKey(bones.Key))
                    {
                        continue;
                    }

                    if (bones.Value > 0.0)
                    {
                        dist1.Add(Math.Abs(MathNet.Numerics.Distance.Euclidean(b1.Joints[bones.Key.ParentJoint].Item2.ToVector(), b1.Joints[bones.Key.ChildJoint].Item2.ToVector()) - bones.Value));
                    }

                    if (l2.LearnedBones[bones.Key] > 0.0)
                    {
                        dist2.Add(Math.Abs(MathNet.Numerics.Distance.Euclidean(b2.Joints[bones.Key.ParentJoint].Item2.ToVector(), b2.Joints[bones.Key.ChildJoint].Item2.ToVector()) - l2.LearnedBones[bones.Key]));
                    }
                }
            }

            var statistics1 = MathNet.Numerics.Statistics.Statistics.MeanStandardDeviation(dist1);
            var statistics2 = MathNet.Numerics.Statistics.Statistics.MeanStandardDeviation(dist2);
            if (statistics1.Item2 < statistics2.Item2)
            {
                SimplifiedBody body = camera1[ids.Item1];
                body.Id = this.generatedIdsMap[(ids.Item1, ids.Item2)];
                bestBodies.Add(body);
            }
            else
            {
                SimplifiedBody body = camera2[ids.Item2];
                body.Id = this.generatedIdsMap[(ids.Item1, ids.Item2)];
                bestBodies.Add(this.TransformBody(body));
            }
        }

        private List<SimplifiedBody> SelectBestBody(Dictionary<uint, SimplifiedBody> camera1, Dictionary<uint, SimplifiedBody> camera2)
        {
            List<SimplifiedBody> bestBodies = new List<SimplifiedBody>();
            foreach (var pair in this.generatedIdsMap)
            {
                if (camera1.ContainsKey(pair.Key.Item1) && camera2.ContainsKey(pair.Key.Item2))
                {
                    if (this.camera1LearnedBodies.ContainsKey(pair.Key.Item1) && this.camera1LearnedBodies.ContainsKey(pair.Key.Item2))
                    {
                        this.SelectByLearnedBodies(camera1, camera2, pair.Key, ref bestBodies);
                    }
                    else
                    {
                        this.SelectByConfidence(camera1, camera2, pair.Key, ref bestBodies);
                    }
                }
                else if (pair.Key.Item1 == 0 || !camera1.ContainsKey(pair.Key.Item1))
                {
                    if (!camera2.ContainsKey(pair.Key.Item2))
                    {
                        continue;
                    }

                    (uint, uint) tuple;
                    var enumT = this.KeyOrValueExistInList(pair.Key, out tuple);
                    switch (enumT)
                    {
                        case TupleState.KeyAlreadyInserted:
                        case TupleState.GoodToInsert:
                            throw new Exception("oups");
                        case TupleState.AlreadyExist:
                        case TupleState.Replace:
                            break;
                    }

                    SimplifiedBody simplifiedBody = this.TransformBody(camera2[pair.Key.Item2]);
                    simplifiedBody.Id = this.generatedIdsMap[(tuple.Item1, tuple.Item2)];
                    bestBodies.Add(simplifiedBody);
                }
                else if (pair.Key.Item2 == 0 || !camera2.ContainsKey(pair.Key.Item2))
                {
                    if (!camera1.ContainsKey(pair.Key.Item1))
                    {
                        continue;
                    }

                    (uint, uint) tuple;
                    var enumT = this.KeyOrValueExistInList(pair.Key, out tuple);
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
                    simplifiedBody.Id = this.generatedIdsMap[(tuple.Item1, tuple.Item2)];
                    bestBodies.Add(simplifiedBody);
                }
            }

            return bestBodies;
        }

        private TupleState KeyOrValueExistInList((uint, uint) tuple, out (uint, uint) value)
        {
            bool checkTupleItem1 = tuple.Item1 != 0;
            bool checkTupleItem2 = tuple.Item2 != 0;
            foreach (var iterator in this.generatedIdsMap)
            {
                bool checkIteratorItem1 = iterator.Key.Item1 != 0;
                bool checkIteratorItem2 = iterator.Key.Item2 != 0;
                bool checkSameItem1 = iterator.Key.Item1 == tuple.Item1;
                bool checkSameItem2 = iterator.Key.Item2 == tuple.Item2;
                if (this.generatedIdsMap.ContainsKey(tuple) || (!checkTupleItem1 && checkSameItem2) || (checkSameItem1 && !checkTupleItem2))
                {
                    value = tuple;
                    return TupleState.AlreadyExist;
                }
                else if ((!checkIteratorItem1 && checkSameItem2) || (checkSameItem1 && !checkIteratorItem2))
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
            // Might use coef for useful joints.
            int accumulator = 0;
            foreach (var joint in body.Joints)
            {
                accumulator += (int)joint.Value.Item1;
            }

            return accumulator;
        }

        private SimplifiedBody TransformBody(SimplifiedBody body)
        {
            if (this.configuration.Camera2ToCamera1Transformation == null)
            {
                return body;
            }

            SimplifiedBody transformed = body.DeepClone();
            foreach (var joint in body.Joints)
            {
                transformed.Joints[joint.Key] = new Tuple<JointConfidenceLevel, Vector3D>(joint.Value.Item1, this.configuration.Camera2ToCamera1Transformation.Transform(joint.Value.Item2));
            }

            return transformed;
        }

        /// <summary>
        /// Comparer for tuples of double and uint, sorting by the double value.
        /// </summary>
        internal class TupleDoubleUintComparer : Comparer<Tuple<double, uint>>
        {
            /// <inheritdoc/>
            public override int Compare(Tuple<double, uint> a, Tuple<double, uint> b)
            {
                if (a.Item1 == b.Item1)
                {
                    return 0;
                }

                return a.Item1 > b.Item1 ? 1 : -1;
            }
        }
    }
}
