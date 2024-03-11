using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace SAAC.Groups
{
    public class EntryGroupsDetector : Subpipeline
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, List<uint>>> OutFormedEntryGroups { get; private set; }

        /// <summary>
        /// Gets list of instant groups.
        /// </summary>
        private Connector<Dictionary<uint, List<uint>>> InInstantGroupsConnector;

        /// <summary>
        /// Receiver that encapsulates the instant groups
        /// </summary>
        public Receiver<Dictionary<uint, List<uint>>> InInstantGroups => InInstantGroupsConnector.In;

        /// <summary>
        ///Gets the nuitrack connector of lists of removed skeletons 
        /// </summary>
        private Connector<List<uint>> InRemovedBodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of Nuitrack skeletons
        /// </summary>
        public Receiver<List<uint>> InRemovedBodies => InRemovedBodiesConnector.In;

        private EntryGroupsDetectorConfiguration Configuration { get; }
        public EntryGroupsDetector(Pipeline parent, EntryGroupsDetectorConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
            : base(parent, name, defaultDeliveryPolicy)
        {
            Configuration = configuration ?? new EntryGroupsDetectorConfiguration();
            InInstantGroupsConnector = CreateInputConnectorFrom<Dictionary<uint, List<uint>>>(parent, nameof(InInstantGroupsConnector));
            InRemovedBodiesConnector = CreateInputConnectorFrom<List<uint>>(parent, nameof(InRemovedBodiesConnector));
            OutFormedEntryGroups = parent.CreateEmitter<Dictionary<uint, List<uint>>>(this, nameof(OutFormedEntryGroups));
            InInstantGroupsConnector.Out.Do(Process);
            InRemovedBodiesConnector.Do(ProcessBodiesRemoving);
        }

        private Dictionary<uint, DateTime> GroupDateTime = new Dictionary<uint, DateTime>();
        private Dictionary<uint, List<uint>> FormedEntryGroups = new Dictionary<uint, List<uint>>();
        private List<uint> FixedBodies = new List<uint>();
        private void Process(Dictionary<uint, List<uint>> instantGroups, Envelope envelope)
        {
            // Entry algo:
            // Once a group is stable for x seconds we consder it as stable for entry group (basic)
            // First clean storage from groups that does not exist in this frame and are not already considered as formed.
            // Check if groups exists and if it's stable enough set it as formed
            foreach (var group in instantGroups)
            {
                if (GroupDateTime.ContainsKey(group.Key))
                {
                    if (!FormedEntryGroups.ContainsKey(group.Key) && (FixedBodies.Intersect(group.Value).Count() == 0) && ((envelope.OriginatingTime - GroupDateTime[group.Key]) > Configuration.GroupFormationDelay))
                    {
                        foreach (var body in group.Value)
                            FixedBodies.Add(body);
                        FormedEntryGroups.Add(group.Key, group.Value.DeepClone());
                    }
                }
                else
                {
                    // Checking collision with formed groups?
                    bool noCollision = true;
                    foreach (uint body in group.Value)
                    {
                        if (FixedBodies.Contains(body))
                        {
                            noCollision = false;
                            break;
                        }
                    }
                    if (noCollision)
                        GroupDateTime.Add(group.Key, envelope.OriginatingTime);
                }
            }
            
            OutFormedEntryGroups.Post(FormedEntryGroups, envelope.OriginatingTime);
        }
        private void ProcessBodiesRemoving(List<uint> idsToRemove, Envelope envelope)
        {
            lock(this)
            {
                foreach(uint id in idsToRemove)
                {
                    if (!FixedBodies.Contains(id))
                        continue;
                    FixedBodies.Remove(id);
                    uint groupId = 0;
                    foreach (var group in FormedEntryGroups)
                    {
                        if (group.Value.Contains(id))
                        {
                            groupId = group.Key;
                            break;
                        }
                    }
                    FormedEntryGroups[groupId].Remove(id);
                    if (FormedEntryGroups[groupId].Count <= 1)
                    {
                        FixedBodies.Remove(FormedEntryGroups[groupId].ElementAt(0));
                        FormedEntryGroups.Remove(groupId);
                        GroupDateTime.Remove(groupId);
                    }
                }
            }
        }
    }
}
