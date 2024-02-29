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
        public Receiver<List<SimplifiedBody>> In;

        Receiver<List<SimplifiedBody>> IConsumer<List<SimplifiedBody>>.In => In;

        private BodiesIdentificationConfiguration Configuration { get; }

        private Dictionary<uint, uint> CorrespondanceMap = new Dictionary<uint, uint>();
        private Dictionary<uint, LearnedBody> LearnedBodies = new Dictionary<uint, LearnedBody>();
        private Dictionary<uint, LearningBody> LearningBodies = new Dictionary<uint, LearningBody>();
        private List<LearnedBody> NewLearnedBodies = new List<LearnedBody>();


        public BodiesIdentification(Pipeline parent, BodiesIdentificationConfiguration? configuration = null)
        {
            Configuration = configuration ?? new BodiesIdentificationConfiguration();
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
                if (CorrespondanceMap.ContainsKey(body.Id))
                {
                    idsBodies.Add(CorrespondanceMap[body.Id]);
                    idsBodies.Add(body.Id);
                    body.Id = CorrespondanceMap[body.Id];
                    LearnedBodies[body.Id].LastSeen = envelope.OriginatingTime;
                    LearnedBodies[body.Id].LastPosition = body.Joints[JointId.Pelvis].Item2;
                    identifiedBodies.Add(body);
                    foundBodies.Add(body.Id);
                    continue;
                }
                else if (LearnedBodies.ContainsKey(body.Id))
                {
                    if (envelope.OriginatingTime - LearnedBodies[body.Id].LastSeen < Configuration.MaximumLostTime || LearnedBodies[body.Id].SeemsTheSame(body, Configuration.MaximumDeviationAllowed, Configuration.MinimumConfidenceLevelForLearning))
                    {
                        LearnedBodies[body.Id].LastSeen = envelope.OriginatingTime;
                        LearnedBodies[body.Id].LastPosition = body.Joints[JointId.Pelvis].Item2;
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
            if(NewLearnedBodies.Count > 0)
            {
                OutLearnedBodies.Post(NewLearnedBodies, envelope.OriginatingTime);
                NewLearnedBodies.Clear();
            }
            CheckCorrespondanceCollision(ref idsBodiesforCollision);
            OutBodiesIdentified.Post(identifiedBodies, envelope.OriginatingTime);
        }

        private void CheckCorrespondanceCollision(ref List<uint> idsBodies)
        {
            var intersection = idsBodies.Intersect(CorrespondanceMap.Keys);
            if(intersection.Count() > 0 )
                foreach(var body in intersection)
                    if (idsBodies.Contains(CorrespondanceMap[body]))
                        CorrespondanceMap.Remove(body);
        }

        private bool ProcessLearningBody(SimplifiedBody body, DateTime timestamp, List<uint> idsBodies)
        {
           
            if (!LearningBodies.ContainsKey(body.Id))
                LearningBodies.Add(body.Id, new LearningBody(body.Id, timestamp, Configuration.BonesUsedForCorrespondence));

            if (LearningBodies[body.Id].StillLearning(timestamp, Configuration.MaximumIdentificationTime, Configuration.MinimumBonesForIdentification))
                ProcessLearning(ref body, LearningBodies[body.Id]);
            else
            {
                List<LearnedBody> learnedBodiesNotVisible = new List<LearnedBody>();
                foreach (var learnedBody in LearnedBodies)
                {
                    if (idsBodies.Contains(learnedBody.Key))
                        continue;
                    //if (timestamp - learnedBody.Value.LastSeen > Configuration.MinimumIdentificationTime)
                    learnedBodiesNotVisible.Add(learnedBody.Value);
                }
                LearnedBody newLearnedBody = LearningBodies[body.Id].GeneratorLearnedBody(Configuration.MaximumDeviationAllowed);
                NewLearnedBodies.Add(newLearnedBody);
                newLearnedBody.LastSeen = timestamp;
                newLearnedBody.LastPosition = body.Joints[JointId.Pelvis].Item2;
                LearningBodies.Remove(body.Id);
                uint correspondanceId = 0;
                if (learnedBodiesNotVisible.Count > 0)
                    correspondanceId = newLearnedBody.FindClosest(learnedBodiesNotVisible, Configuration.MaximumDeviationAllowed);
                if (correspondanceId > 0)
                    CorrespondanceMap[body.Id] = correspondanceId;
                else
                {
                    LearnedBodies.Add(body.Id, newLearnedBody);
                    idsBodies.Add(body.Id);
                }
                return false;
            }
            return true;
        }

        private void ProcessLearning(ref SimplifiedBody body, LearningBody learningBody)
        {
            foreach (var bone in Configuration.BonesUsedForCorrespondence)
            {
                if(body.Joints[bone.ParentJoint].Item1 >= Configuration.MinimumConfidenceLevelForLearning && body.Joints[bone.ChildJoint].Item1 >= Configuration.MinimumConfidenceLevelForLearning)
                    learningBody.LearningBones[bone].Add(MathNet.Numerics.Distance.Euclidean(body.Joints[bone.ParentJoint].Item2.ToVector(), body.Joints[bone.ChildJoint].Item2.ToVector()));
            }
        }

        private void RemoveOldIds(DateTime current, ref List<uint> idsToRemove)
        {
            foreach (var body in LearnedBodies)
                if((current - body.Value.LastSeen) > Configuration.MaximumLostTime)
                    idsToRemove.Add(body.Key);

            RemoveIds(idsToRemove);
        }

        private void RemoveIds(List<uint> ids)
        {
            foreach (uint id in ids)
            {
                LearnedBodies.Remove(id);
                CorrespondanceMap.Remove(id);
                foreach (var iterator in CorrespondanceMap)
                {
                    if (iterator.Value == id)
                    {
                        CorrespondanceMap.Remove(iterator.Key);
                        break;
                    }
                }
            }
        }
    }
}
