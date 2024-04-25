using MathNet.Spatial.Euclidean;
using MathNet.Numerics.Statistics;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Azure.Kinect.BodyTracking;

namespace SAAC.Bodies
{
    public class BodiesIdentification : IConsumer<List<SimplifiedBody>>
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<List<SimplifiedBody>> OutBodiesIdentified { get; private set; }

        /// <summary>
        /// Gets the emitter of new learned bodies.
        /// </summary>
        public Emitter<List<LearnedBody>> OutLearnedBodies { get; private set; }

        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<List<uint>> OutBodiesRemoved { get; private set; }

        /// <summary>
        /// Receiver that encapsulates the input list of skeletons
        /// </summary>
        public Receiver<List<SimplifiedBody>> In { get; private set; }

        private BodiesIdentificationConfiguration configuration;

        private Dictionary<uint, uint> correspondanceMap = new Dictionary<uint, uint>();
        private Dictionary<uint, LearnedBody> learnedBodies = new Dictionary<uint, LearnedBody>();
        private Dictionary<uint, LearningBody> learningBodies = new Dictionary<uint, LearningBody>();
        private List<LearnedBody> newLearnedBodies = new List<LearnedBody>();


        public BodiesIdentification(Pipeline parent, BodiesIdentificationConfiguration? configuration = null)
        {
            this.configuration = configuration ?? new BodiesIdentificationConfiguration();
            In = parent.CreateReceiver<List<SimplifiedBody>>(this, Process, nameof(In));
            OutBodiesIdentified = parent.CreateEmitter<List<SimplifiedBody>>(this, nameof(OutBodiesIdentified));
            OutLearnedBodies = parent.CreateEmitter<List<LearnedBody>>(this, nameof(OutLearnedBodies));
            OutBodiesRemoved = parent.CreateEmitter<List<uint>>(this, nameof(OutBodiesRemoved));
        }

        private void Process(List<SimplifiedBody> bodies, Envelope envelope)
        {
            List<SimplifiedBody> identifiedBodies = new List<SimplifiedBody>();
            List<uint> foundBodies = new List<uint>();
            List<uint> idsBodies = new List<uint>();
            List<uint> idsBodiesforCollision = new List<uint>();
            List<uint> idsToRemove = new List<uint>();
            RemoveOldIds(envelope.OriginatingTime, ref idsToRemove);
            foreach (var body in bodies)
            {
                idsBodiesforCollision.Add(body.Id);
                if (correspondanceMap.ContainsKey(body.Id))
                {
                    idsBodies.Add(correspondanceMap[body.Id]);
                    idsBodies.Add(body.Id);
                    body.Id = correspondanceMap[body.Id];
                    learnedBodies[body.Id].LastSeen = envelope.OriginatingTime;
                    learnedBodies[body.Id].LastPosition = body.Joints[JointId.Pelvis].Item2;
                    identifiedBodies.Add(body);
                    foundBodies.Add(body.Id);
                    continue;
                }
                else if (learnedBodies.ContainsKey(body.Id))
                {
                    if (envelope.OriginatingTime - learnedBodies[body.Id].LastSeen < configuration.MaximumLostTime || learnedBodies[body.Id].SeemsTheSame(body, configuration.MaximumDeviationAllowed, configuration.MinimumConfidenceLevelForLearning))
                    {
                        learnedBodies[body.Id].LastSeen = envelope.OriginatingTime;
                        learnedBodies[body.Id].LastPosition = body.Joints[JointId.Pelvis].Item2;
                        identifiedBodies.Add(body);
                        foundBodies.Add(body.Id);
                        idsBodies.Add(body.Id);
                    }
                    else
                        idsToRemove.Add(body.Id);
                }
            }
            if (idsToRemove.Count > 0)
            { 
                RemoveIds(idsToRemove);
                OutBodiesRemoved.Post(idsToRemove, envelope.OriginatingTime);
            }
            foreach (var body in bodies)
                if (!foundBodies.Contains(body.Id))
                    ProcessLearningBody(body, envelope.OriginatingTime, idsBodies);
            if(newLearnedBodies.Count > 0)
            {
                OutLearnedBodies.Post(newLearnedBodies, envelope.OriginatingTime);
                newLearnedBodies.Clear();
            }
            CheckCorrespondanceCollision(ref idsBodiesforCollision);
            OutBodiesIdentified.Post(identifiedBodies, envelope.OriginatingTime);
        }

        private void CheckCorrespondanceCollision(ref List<uint> idsBodies)
        {
            var intersection = idsBodies.Intersect(correspondanceMap.Keys);
            if(intersection.Count() > 0 )
                foreach(var body in intersection)
                    if (idsBodies.Contains(correspondanceMap[body]))
                        correspondanceMap.Remove(body);
        }

        private bool ProcessLearningBody(SimplifiedBody body, DateTime timestamp, List<uint> idsBodies)
        {
           
            if (!learningBodies.ContainsKey(body.Id))
                learningBodies.Add(body.Id, new LearningBody(body.Id, timestamp, configuration.BonesUsedForCorrespondence));

            if (learningBodies[body.Id].StillLearning(timestamp, configuration.MaximumIdentificationTime, configuration.MinimumBonesForIdentification))
                ProcessLearning(ref body, learningBodies[body.Id]);
            else
            {
                List<LearnedBody> learnedBodiesNotVisible = new List<LearnedBody>();
                foreach (var learnedBody in learnedBodies)
                {
                    if (idsBodies.Contains(learnedBody.Key))
                        continue;
                    //if (timestamp - learnedBody.Value.LastSeen > configuration.MinimumIdentificationTime)
                    learnedBodiesNotVisible.Add(learnedBody.Value);
                }
                LearnedBody newLearnedBody = learningBodies[body.Id].GeneratorLearnedBody(configuration.MaximumDeviationAllowed);
                newLearnedBodies.Add(newLearnedBody);
                newLearnedBody.LastSeen = timestamp;
                newLearnedBody.LastPosition = body.Joints[JointId.Pelvis].Item2;
                learningBodies.Remove(body.Id);
                uint correspondanceId = 0;
                if (learnedBodiesNotVisible.Count > 0)
                    correspondanceId = newLearnedBody.FindClosest(learnedBodiesNotVisible, configuration.MaximumDeviationAllowed);
                if (correspondanceId > 0)
                    correspondanceMap[body.Id] = correspondanceId;
                else
                {
                    learnedBodies.Add(body.Id, newLearnedBody);
                    idsBodies.Add(body.Id);
                }
                return false;
            }
            return true;
        }

        private void ProcessLearning(ref SimplifiedBody body, LearningBody learningBody)
        {
            foreach (var bone in configuration.BonesUsedForCorrespondence)
            {
                if(body.Joints[bone.ParentJoint].Item1 >= configuration.MinimumConfidenceLevelForLearning && body.Joints[bone.ChildJoint].Item1 >= configuration.MinimumConfidenceLevelForLearning)
                    learningBody.LearningBones[bone].Add(MathNet.Numerics.Distance.Euclidean(body.Joints[bone.ParentJoint].Item2.ToVector(), body.Joints[bone.ChildJoint].Item2.ToVector()));
            }
        }

        private void RemoveOldIds(DateTime current, ref List<uint> idsToRemove)
        {
            foreach (var body in learnedBodies)
                if((current - body.Value.LastSeen) > configuration.MaximumLostTime)
                    idsToRemove.Add(body.Key);

            RemoveIds(idsToRemove);
        }

        private void RemoveIds(List<uint> ids)
        {
            foreach (uint id in ids)
            {
                learnedBodies.Remove(id);
                correspondanceMap.Remove(id);
                foreach (var iterator in correspondanceMap)
                {
                    if (iterator.Value == id)
                    {
                        correspondanceMap.Remove(iterator.Key);
                        break;
                    }
                }
            }
        }
    }
}
