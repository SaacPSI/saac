// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Psi;

    /// <summary>
    /// Component that identifies bodies across frames by learning their physical characteristics.
    /// </summary>
    public class BodiesIdentification : IConsumer<List<SimplifiedBody>>
    {
        /// <summary>
        /// Gets the emitter of identified bodies.
        /// </summary>
        public Emitter<List<SimplifiedBody>> OutBodiesIdentified { get; private set; }

        /// <summary>
        /// Gets the emitter of new learned bodies.
        /// </summary>
        public Emitter<List<LearnedBody>> OutLearnedBodies { get; private set; }

        /// <summary>
        /// Gets the emitter of removed body IDs.
        /// </summary>
        public Emitter<List<uint>> OutBodiesRemoved { get; private set; }

        /// <summary>
        /// Gets the receiver that encapsulates the input list of skeletons.
        /// </summary>
        public Receiver<List<SimplifiedBody>> In { get; private set; }

        private readonly BodiesIdentificationConfiguration configuration;
        private readonly Dictionary<uint, uint> correspondanceMap = new Dictionary<uint, uint>();
        private readonly Dictionary<uint, LearnedBody> learnedBodies = new Dictionary<uint, LearnedBody>();
        private readonly Dictionary<uint, LearningBody> learningBodies = new Dictionary<uint, LearningBody>();
        private readonly List<LearnedBody> newLearnedBodies = new List<LearnedBody>();
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="BodiesIdentification"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration for body identification.</param>
        /// <param name="name">Optional name for the component.</param>
        public BodiesIdentification(Pipeline parent, BodiesIdentificationConfiguration? configuration = null, string name = nameof(BodiesIdentification))
        {
            this.name = name;
            this.configuration = configuration ?? new BodiesIdentificationConfiguration();
            this.In = parent.CreateReceiver<List<SimplifiedBody>>(this, this.Process, $"{name}-In");
            this.OutBodiesIdentified = parent.CreateEmitter<List<SimplifiedBody>>(this, $"{name}-OutBodiesIdentified");
            this.OutLearnedBodies = parent.CreateEmitter<List<LearnedBody>>(this, $"{name}-OutLearnedBodies");
            this.OutBodiesRemoved = parent.CreateEmitter<List<uint>>(this, $"{name}-OutBodiesRemoved");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(List<SimplifiedBody> bodies, Envelope envelope)
        {
            List<SimplifiedBody> identifiedBodies = new List<SimplifiedBody>();
            List<uint> foundBodies = new List<uint>();
            List<uint> idsBodies = new List<uint>();
            List<uint> idsBodiesforCollision = new List<uint>();
            List<uint> idsToRemove = new List<uint>();
            this.RemoveOldIds(envelope.OriginatingTime, ref idsToRemove);
            foreach (var body in bodies)
            {
                idsBodiesforCollision.Add(body.Id);
                if (this.correspondanceMap.ContainsKey(body.Id))
                {
                    idsBodies.Add(this.correspondanceMap[body.Id]);
                    idsBodies.Add(body.Id);
                    body.Id = this.correspondanceMap[body.Id];
                    this.learnedBodies[body.Id].LastSeen = envelope.OriginatingTime;
                    this.learnedBodies[body.Id].LastPosition = body.Joints[JointId.Pelvis].Item2;
                    identifiedBodies.Add(body);
                    foundBodies.Add(body.Id);
                    continue;
                }
                else if (this.learnedBodies.ContainsKey(body.Id))
                {
                    if (envelope.OriginatingTime - this.learnedBodies[body.Id].LastSeen < this.configuration.MaximumLostTime || this.learnedBodies[body.Id].SeemsTheSame(body, this.configuration.MaximumDeviationAllowed, this.configuration.MinimumConfidenceLevelForLearning))
                    {
                        this.learnedBodies[body.Id].LastSeen = envelope.OriginatingTime;
                        this.learnedBodies[body.Id].LastPosition = body.Joints[JointId.Pelvis].Item2;
                        identifiedBodies.Add(body);
                        foundBodies.Add(body.Id);
                        idsBodies.Add(body.Id);
                    }
                    else
                    {
                        idsToRemove.Add(body.Id);
                    }
                }
            }

            if (idsToRemove.Count > 0)
            {
                this.RemoveIds(idsToRemove);
                this.OutBodiesRemoved.Post(idsToRemove, envelope.OriginatingTime);
            }

            foreach (var body in bodies)
            {
                if (!foundBodies.Contains(body.Id))
                {
                    this.ProcessLearningBody(body, envelope.OriginatingTime, idsBodies);
                }
            }

            if (this.newLearnedBodies.Count > 0)
            {
                this.OutLearnedBodies.Post(this.newLearnedBodies, envelope.OriginatingTime);
                this.newLearnedBodies.Clear();
            }

            this.CheckCorrespondanceCollision(ref idsBodiesforCollision);
            this.OutBodiesIdentified.Post(identifiedBodies, envelope.OriginatingTime);
        }

        private void CheckCorrespondanceCollision(ref List<uint> idsBodies)
        {
            var intersection = idsBodies.Intersect(this.correspondanceMap.Keys);
            if (intersection.Count() > 0)
            {
                foreach (var body in intersection)
                {
                    if (idsBodies.Contains(this.correspondanceMap[body]))
                    {
                        this.correspondanceMap.Remove(body);
                    }
                }
            }
        }

        private bool ProcessLearningBody(SimplifiedBody body, DateTime timestamp, List<uint> idsBodies)
        {
            if (!this.learningBodies.ContainsKey(body.Id))
            {
                this.learningBodies.Add(body.Id, new LearningBody(body.Id, timestamp, this.configuration.BonesUsedForCorrespondence));
            }

            if (this.learningBodies[body.Id].StillLearning(timestamp, this.configuration.MaximumIdentificationTime, this.configuration.MinimumBonesForIdentification))
            {
                this.ProcessLearning(ref body, this.learningBodies[body.Id]);
            }
            else
            {
                List<LearnedBody> learnedBodiesNotVisible = new List<LearnedBody>();
                foreach (var learnedBody in this.learnedBodies)
                {
                    if (idsBodies.Contains(learnedBody.Key))
                    {
                        continue;
                    }

                    learnedBodiesNotVisible.Add(learnedBody.Value);
                }

                LearnedBody newLearnedBody = this.learningBodies[body.Id].GeneratorLearnedBody(this.configuration.MaximumDeviationAllowed);
                this.newLearnedBodies.Add(newLearnedBody);
                newLearnedBody.LastSeen = timestamp;
                newLearnedBody.LastPosition = body.Joints[JointId.Pelvis].Item2;
                this.learningBodies.Remove(body.Id);
                uint correspondanceId = 0;
                if (learnedBodiesNotVisible.Count > 0)
                {
                    correspondanceId = newLearnedBody.FindClosest(learnedBodiesNotVisible, this.configuration.MaximumDeviationAllowed);
                }

                if (correspondanceId > 0)
                {
                    this.correspondanceMap[body.Id] = correspondanceId;
                }
                else
                {
                    this.learnedBodies.Add(body.Id, newLearnedBody);
                    idsBodies.Add(body.Id);
                }

                return false;
            }

            return true;
        }

        private void ProcessLearning(ref SimplifiedBody body, LearningBody learningBody)
        {
            foreach (var bone in this.configuration.BonesUsedForCorrespondence)
            {
                if (body.Joints[bone.ParentJoint].Item1 >= this.configuration.MinimumConfidenceLevelForLearning && body.Joints[bone.ChildJoint].Item1 >= this.configuration.MinimumConfidenceLevelForLearning)
                {
                    learningBody.LearningBones[bone].Add(MathNet.Numerics.Distance.Euclidean(body.Joints[bone.ParentJoint].Item2.ToVector(), body.Joints[bone.ChildJoint].Item2.ToVector()));
                }
            }
        }

        private void RemoveOldIds(DateTime current, ref List<uint> idsToRemove)
        {
            foreach (var body in this.learnedBodies)
            {
                if ((current - body.Value.LastSeen) > this.configuration.MaximumLostTime)
                {
                    idsToRemove.Add(body.Key);
                }
            }

            this.RemoveIds(idsToRemove);
        }

        private void RemoveIds(List<uint> ids)
        {
            foreach (uint id in ids)
            {
                this.learnedBodies.Remove(id);
                this.correspondanceMap.Remove(id);
                foreach (var iterator in this.correspondanceMap)
                {
                    if (iterator.Value == id)
                    {
                        this.correspondanceMap.Remove(iterator.Key);
                        break;
                    }
                }
            }
        }
    }
}
