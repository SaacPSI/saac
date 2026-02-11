// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that detects entry groups by analyzing the stability of groups over time.
    /// Once a group remains stable for a configured duration, it is considered as a formed entry group.
    /// </summary>
    public class EntryGroupsDetector : IConsumerProducer<Dictionary<uint, List<uint>>, Dictionary<uint, List<uint>>>
    {
        private readonly EntryGroupsDetectorConfiguration configuration;
        private readonly Dictionary<uint, DateTime> groupDateTime = new Dictionary<uint, DateTime>();
        private readonly Dictionary<uint, List<uint>> formedEntryGroups = new Dictionary<uint, List<uint>>();
        private readonly List<uint> fixedBodies = new List<uint>();
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntryGroupsDetector"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration for the detector.</param>
        /// <param name="name">The name of the component.</param>
        public EntryGroupsDetector(Pipeline parent, EntryGroupsDetectorConfiguration? configuration = null, string name = nameof(EntryGroupsDetector))
        {
            this.name = name;
            this.configuration = configuration ?? new EntryGroupsDetectorConfiguration();
            this.In = parent.CreateReceiver<Dictionary<uint, List<uint>>>(this, this.Process, $"{name}-In");
            this.InRemovedBodies = parent.CreateReceiver<List<uint>>(this, this.ProcessBodiesRemoving, $"{name}-InRemovedBodies");
            this.Out = parent.CreateEmitter<Dictionary<uint, List<uint>>>(this, $"{name}-Out");
        }

        /// <summary>
        /// Gets the emitter of detected entry groups.
        /// </summary>
        public Emitter<Dictionary<uint, List<uint>>> Out { get; private set; }

        /// <summary>
        /// Gets the receiver that encapsulates the instant groups.
        /// </summary>
        public Receiver<Dictionary<uint, List<uint>>> In { get; private set; }

        /// <summary>
        /// Gets the receiver for removed body IDs.
        /// </summary>
        public Receiver<List<uint>> InRemovedBodies { get; private set; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Processes instant groups and determines which groups have been stable long enough to be considered entry groups.
        /// </summary>
        /// <param name="instantGroups">Dictionary of instant group detections.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process(Dictionary<uint, List<uint>> instantGroups, Envelope envelope)
        {
            // Entry algo:
            // Once a group is stable for x seconds we consder it as stable for entry group (basic)
            // First clean storage from groups that does not exist in this frame and are not already considered as formed.
            // Check if groups exists and if it's stable enough set it as formed
            foreach (var group in instantGroups)
            {
                if (this.groupDateTime.ContainsKey(group.Key))
                {
                    if (!this.formedEntryGroups.ContainsKey(group.Key) && (this.fixedBodies.Intersect(group.Value).Count() == 0) && ((envelope.OriginatingTime - this.groupDateTime[group.Key]) > this.configuration.GroupFormationDelay))
                    {
                        foreach (var body in group.Value)
                        {
                            this.fixedBodies.Add(body);
                        }

                        this.formedEntryGroups.Add(group.Key, group.Value.DeepClone());
                    }
                }
                else
                {
                    // Checking collision with formed groups?
                    bool noCollision = true;
                    foreach (uint body in group.Value)
                    {
                        if (this.fixedBodies.Contains(body))
                        {
                            noCollision = false;
                            break;
                        }
                    }

                    if (noCollision)
                    {
                        this.groupDateTime.Add(group.Key, envelope.OriginatingTime);
                    }
                }
            }

            this.Out.Post(this.formedEntryGroups, envelope.OriginatingTime);
        }

        /// <summary>
        /// Processes removal of bodies from tracking, cleaning up entry groups that become invalid.
        /// </summary>
        /// <param name="idsToRemove">List of body IDs to remove.</param>
        /// <param name="envelope">The message envelope.</param>
        private void ProcessBodiesRemoving(List<uint> idsToRemove, Envelope envelope)
        {
            lock (this)
            {
                foreach (uint id in idsToRemove)
                {
                    if (!this.fixedBodies.Contains(id))
                    {
                        continue;
                    }

                    this.fixedBodies.Remove(id);
                    uint groupId = 0;
                    foreach (var group in this.formedEntryGroups)
                    {
                        if (group.Value.Contains(id))
                        {
                            groupId = group.Key;
                            break;
                        }
                    }

                    this.formedEntryGroups[groupId].Remove(id);
                    if (this.formedEntryGroups[groupId].Count <= 1)
                    {
                        this.fixedBodies.Remove(this.formedEntryGroups[groupId].ElementAt(0));
                        this.formedEntryGroups.Remove(groupId);
                        this.groupDateTime.Remove(groupId);
                    }
                }
            }
        }
    }
}
