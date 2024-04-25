using Microsoft.Psi;
using Microsoft.Psi.Components;
using MathNet.Spatial.Euclidean;

namespace SAAC.Groups
{
    public class InstantGroupsDetector : IConsumerProducer<Dictionary<uint, Vector3D>, Dictionary<uint, List<uint>>>
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, List<uint>>> Out { get; private set; }


        /// <summary>
        /// Receiver that encapsulates the input list of skeletons
        /// </summary>
        public Receiver<Dictionary<uint, Vector3D>> In { get; private set; }

        public InstantGroupsDetectorConfiguration configuration;
        private string name;

        public InstantGroupsDetector(Pipeline parent, InstantGroupsDetectorConfiguration? configuration = null, string name = nameof(InstantGroupsDetector)) 
        {
            this.name = name;
            this.configuration = configuration ?? new InstantGroupsDetectorConfiguration();
            In = parent.CreateReceiver<Dictionary<uint, Vector3D>>(this, Process, $"{name}-In");
            Out = parent.CreateEmitter<Dictionary<uint, List<uint>>>(this, $"{name}-Out");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(Dictionary<uint, Vector3D> skeletons, Envelope envelope)
        {
            Dictionary<uint, List<uint>> rawGroups = new Dictionary<uint, List<uint>>();
            for (int iterator1 = 0; iterator1 < skeletons.Count; iterator1++)
            {
                for (int iterator2 = iterator1 + 1; iterator2 < skeletons.Count; iterator2++)
                {
                    uint idBody1 = skeletons.ElementAt(iterator1).Key;
                    uint idBody2 = skeletons.ElementAt(iterator2).Key;
                    double distance = MathNet.Numerics.Distance.Euclidean(skeletons.ElementAt(iterator1).Value.ToVector(), skeletons.ElementAt(iterator2).Value.ToVector());
                    if (distance > configuration.DistanceThreshold)
                        continue;

                    if (rawGroups.ContainsKey(idBody1))
                        rawGroups[idBody1].Add(idBody2);
                    else
                    {
                        List<uint> group = [idBody1, idBody2];
                        rawGroups.Add(idBody1, group);
                    }

                    if (rawGroups.ContainsKey(idBody2))
                        rawGroups[idBody2].Add(idBody1);
                    else 
                    {
                        List<uint> group = [idBody1, idBody2];
                        rawGroups.Add(idBody2, group); 
                    }
                }
            }

            ReduceGroups(ref rawGroups);

            Dictionary<uint, List<uint>> outData = new Dictionary<uint, List<uint>>();
            foreach (var rawGroup in rawGroups)
            {
                rawGroup.Value.Sort();
                List<uint> group = rawGroup.Value.Distinct().ToList();
                uint uid = GroupsHelpers.CantorParingSequence(group);
                outData.Add(uid, group);
            }
            Out.Post(outData, envelope.OriginatingTime);
        }

        protected void ReduceGroups(ref Dictionary<uint, List<uint>> groups)
        {
            //bool call = true;
            //while (call)
            //{
                bool call = false;
                foreach (var group in groups)
                {
                    foreach (var id in group.Value)
                    {
                        if (groups.ContainsKey(id) && group.Key != id)
                        {
                            groups[group.Key].AddRange(groups[id]);
                            groups.Remove(id);
                            call = true;
                            break;
                        }
                    }
                    if (call)
                        break;
                }
            //}
            if (call)
                ReduceGroups(ref groups);
        }
    }
}
