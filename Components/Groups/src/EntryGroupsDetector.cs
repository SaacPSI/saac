using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace SAAC.Groups
{
    public class EntryGroupsDetector : IConsumerProducer<Dictionary<uint, List<uint>>, Dictionary<uint, List<uint>>>
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, List<uint>>> Out { get; private set; }

        /// <summary>
        /// Receiver that encapsulates the instant groups
        /// </summary>
        public Receiver<Dictionary<uint, List<uint>>> In { get; private set; }

        /// <summary>
        /// Receiver that encapsulates the input list of Nuitrack skeletons
        /// </summary>
        public Receiver<List<uint>> InRemovedBodies { get; private set; }

        private EntryGroupsDetectorConfiguration configuration;
        private Dictionary<uint, DateTime> groupDateTime = new Dictionary<uint, DateTime>();
        private Dictionary<uint, List<uint>> formedEntryGroups = new Dictionary<uint, List<uint>>();
        private List<uint> fixedBodies = new List<uint>();
        private string name;

        public EntryGroupsDetector(Pipeline parent, EntryGroupsDetectorConfiguration? configuration = null, string name = nameof(EntryGroupsDetector))
        {
            this.name = name;
            this.configuration = configuration ?? new EntryGroupsDetectorConfiguration();
            In = parent.CreateReceiver<Dictionary<uint, List<uint>>>(this, Process, $"{name}-In");
            InRemovedBodies = parent.CreateReceiver<List<uint>>(this, ProcessBodiesRemoving, $"{name}-InRemovedBodies");
            Out = parent.CreateEmitter<Dictionary<uint, List<uint>>>(this, $"{name}-Out");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(Dictionary<uint, List<uint>> instantGroups, Envelope envelope)
        {
            // Entry algo:
            // Once a group is stable for x seconds we consder it as stable for entry group (basic)
            // First clean storage from groups that does not exist in this frame and are not already considered as formed.
            // Check if groups exists and if it's stable enough set it as formed
            foreach (var group in instantGroups)
            {
                if (groupDateTime.ContainsKey(group.Key))
                {
                    if (!formedEntryGroups.ContainsKey(group.Key) && (fixedBodies.Intersect(group.Value).Count() == 0) && ((envelope.OriginatingTime - groupDateTime[group.Key]) > configuration.GroupFormationDelay))
                    {
                        foreach (var body in group.Value)
                            fixedBodies.Add(body);
                        formedEntryGroups.Add(group.Key, group.Value.DeepClone());
                    }
                }
                else
                {
                    // Checking collision with formed groups?
                    bool noCollision = true;
                    foreach (uint body in group.Value)
                    {
                        if (fixedBodies.Contains(body))
                        {
                            noCollision = false;
                            break;
                        }
                    }
                    if (noCollision)
                        groupDateTime.Add(group.Key, envelope.OriginatingTime);
                }
            }
            
            Out.Post(formedEntryGroups, envelope.OriginatingTime);
        }
        private void ProcessBodiesRemoving(List<uint> idsToRemove, Envelope envelope)
        {
            lock(this)
            {
                foreach(uint id in idsToRemove)
                {
                    if (!fixedBodies.Contains(id))
                        continue;
                    fixedBodies.Remove(id);
                    uint groupId = 0;
                    foreach (var group in formedEntryGroups)
                    {
                        if (group.Value.Contains(id))
                        {
                            groupId = group.Key;
                            break;
                        }
                    }
                    formedEntryGroups[groupId].Remove(id);
                    if (formedEntryGroups[groupId].Count <= 1)
                    {
                        fixedBodies.Remove(formedEntryGroups[groupId].ElementAt(0));
                        formedEntryGroups.Remove(groupId);
                        groupDateTime.Remove(groupId);
                    }
                }
            }
        }
    }
}
