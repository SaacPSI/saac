using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace SAAC.Groups
{
    internal class GroupInfo
    {
        public uint         Id { get; private set; }
        public DateTime     Timestamp { get; private set; }
        public List<uint>   Bodies { get; private set; }

        public GroupInfo(uint id, DateTime timestamp, List<uint> list)
        {
            Id = id;
            Timestamp = timestamp;
            Bodies = list;
        }
    };

    public class IntegratedGroupsDetector : IConsumerProducer<Dictionary<uint, List<uint>>, Dictionary<uint, List<uint>>>
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, List<uint>>> Out { get; private set; }

        /// <summary>
        /// Gets list of instant groups.
        /// </summary>
        private Connector<Dictionary<uint, List<uint>>> InConnector;
        /// <summary>
        /// Receiver that encapsulates the instant groups
        /// </summary>
        public Receiver<Dictionary<uint, List<uint>>> In => InConnector.In;

        /// <summary>
        ///Gets the nuitrack connector of lists of removed skeletons 
        /// </summary>
        private Connector<List<uint>> InRemovedBodiesConnector;
        /// <summary>
        /// Receiver that encapsulates the input list of Nuitrack skeletons
        /// </summary>
        public Receiver<List<uint>> InRemovedBodies => InRemovedBodiesConnector.In;

        private IntegratedGroupsDetectorConfiguration Configuration { get; }
        public IntegratedGroupsDetector(Pipeline parent, IntegratedGroupsDetectorConfiguration? configuration = null)
        {
            Configuration = configuration ?? new IntegratedGroupsDetectorConfiguration();
            InConnector = parent.CreateConnector<Dictionary<uint, List<uint>>>(nameof(InConnector));
            InRemovedBodiesConnector = parent.CreateConnector<List<uint>>(nameof(InRemovedBodiesConnector));
            Out = parent.CreateEmitter<Dictionary<uint, List<uint>>>(this, nameof(Out));
            InConnector.Out.Do(Process);
            InRemovedBodiesConnector.Do(ProcessBodiesRemoving);
        }

        private Dictionary<uint, uint> GroupPairing= new Dictionary<uint, uint>();
        private Dictionary<uint, DateTime> BodyDateTime = new Dictionary<uint, DateTime>();
        private Dictionary<uint, List<uint>> GroupsParameters = new Dictionary<uint, List<uint>>();
        private Dictionary<uint, Dictionary<uint, double>> BodyToWeightedGroups = new Dictionary<uint, Dictionary<uint, double>>();
        private List<uint> BodyRemoved = new List<uint>();

        private void Process(Dictionary<uint, List<uint>> instantGroups, Envelope envelope)
        {
            //Integrationg data
            //TO DO describ basic algo
            lock(this)
            { 
                Dictionary<uint, List<uint>> groupsToRemove = new Dictionary<uint, List<uint>>();
                foreach (var newGroup in instantGroups)
                {
                    foreach (var group in GroupsParameters)
                    {
                        if (newGroup.Value.Count <= group.Value.Count)
                            continue;
                        var intersection = group.Value.Intersect(newGroup.Value);
                        if(intersection.Any() && (group.Value.Count/intersection.Count()) >= Configuration.IntersectionPercentage)
                        {
                            if(groupsToRemove.ContainsKey(newGroup.Key))
                                groupsToRemove[newGroup.Key].Add(group.Key);
                            else
                                groupsToRemove[newGroup.Key] = new List<uint>{ group.Key };
                        }
                    }
                }

                foreach (var listToRemove in groupsToRemove)
                {
                    if(listToRemove.Value.Count > 1)
                    {
                        foreach (var groupToRemove in listToRemove.Value)
                            GroupPairing[groupToRemove] = listToRemove.Key;
                    }
                    foreach (var toRemove in listToRemove.Value)
                    {
                        GroupsParameters.Remove(toRemove);
                        foreach (var group in BodyToWeightedGroups)
                            group.Value.Remove(toRemove);
                    }
                }

                foreach (var group in instantGroups)
                {
                    if (group.Value.Intersect(BodyRemoved).Count() != 0)
                        continue;
                    if (!GroupsParameters.ContainsKey(group.Key))
                        GroupsParameters[group.Key] = group.Value.DeepClone();
                    foreach (var body in group.Value)
                    {
                        if (BodyToWeightedGroups.ContainsKey(body))
                        {
                            TimeSpan span = envelope.OriginatingTime - BodyDateTime[body];
                            if (BodyToWeightedGroups[body].ContainsKey(group.Key))
                                BodyToWeightedGroups[body][group.Key] += Math.Pow(span.TotalMilliseconds, Configuration.IncreaseWeightFactor);
                            else
                                BodyToWeightedGroups[body].Add(group.Key, Math.Pow(span.TotalMilliseconds, Configuration.IncreaseWeightFactor /** BodyToWeightedGroups[body].Count == 0 ? 10.0 :1.0 */));
                            if(GroupPairing.ContainsKey(group.Key) && BodyToWeightedGroups[body].ContainsKey(GroupPairing[group.Key]))
                                BodyToWeightedGroups[body][GroupPairing[group.Key]] += Math.Pow(span.TotalMilliseconds, Configuration.IncreaseWeightFactor);
                            for (uint iterator = 0; iterator < BodyToWeightedGroups[body].Count; iterator++)
                            {
                                uint Key = BodyToWeightedGroups[body].ElementAt((int)iterator).Key;
                                if (Key == group.Key)
                                    continue;
                                BodyToWeightedGroups[body][Key] -= Math.Pow(span.TotalMilliseconds, Configuration.DecreaseWeightFactor);
                            }
                            BodyDateTime[body] = envelope.OriginatingTime;
                        }
                        else
                        {
                            Dictionary<uint, double> nDic = new Dictionary<uint, double>();
                            nDic.Add(group.Key, Configuration.IncreaseWeightFactor);
                            BodyToWeightedGroups.Add(body, nDic);
                            BodyDateTime.Add(body, envelope.OriginatingTime);
                        }
                    }
                }

                // Generating Interated Groups
                Dictionary<uint, List<uint>> integratedGroups = new Dictionary<uint, List<uint>>();
                foreach (var iterator in BodyToWeightedGroups)
                {
                    if (iterator.Value.Count == 0)
                        continue;
                    var list = iterator.Value.ToList();
                    list.Sort((x, y) => y.Value.CompareTo(x.Value));
                    uint groupId = list.ElementAt(0).Key;
                    if (integratedGroups.ContainsKey(groupId))
                        integratedGroups[groupId].Add(iterator.Key);
                    else
                    {
                        List<uint> groupList = new List<uint>();
                        groupList.Add(iterator.Key);
                        integratedGroups.Add(groupId, groupList);
                    }
                }
            
            Out.Post(integratedGroups, envelope.OriginatingTime);
            }
        }

        private void ProcessBodiesRemoving(List<uint> idsToRemove, Envelope envelope)
        {
            lock (this)
            {
                foreach (uint id in idsToRemove)
                {
                    BodyDateTime.Remove(id);
                    if (BodyToWeightedGroups.ContainsKey(id))
                    {
                        foreach (var pair in BodyToWeightedGroups[id])
                        {
                            List<uint> bodiesInGroup = new List<uint>();
                            foreach (var body in BodyToWeightedGroups)
                            {
                                if (body.Key == id)
                                    continue;
                                if (body.Value.ContainsKey(pair.Key))
                                    bodiesInGroup.Add(body.Key);
                            }
                            if (bodiesInGroup.Count == 1)
                                BodyToWeightedGroups[bodiesInGroup.ElementAt(0)].Remove(pair.Key);
                            if (bodiesInGroup.Count != 0)
                                GroupsParameters.Remove(pair.Key);
                        }
                    }
                    BodyToWeightedGroups.Remove(id);
                    BodyRemoved.Add(id);
                }
            }
        }
    }
}
