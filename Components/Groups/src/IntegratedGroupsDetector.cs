// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that integrates instant group detections over time to produce stable group assignments.
    /// Uses weighted scoring based on temporal consistency to determine group membership.
    /// </summary>
    public class IntegratedGroupsDetector : IConsumerProducer<Dictionary<uint, List<uint>>, Dictionary<uint, List<uint>>>
    {
        private readonly IntegratedGroupsDetectorConfiguration configuration;
        private readonly Dictionary<uint, uint> groupPairing = new Dictionary<uint, uint>();
        private readonly Dictionary<uint, DateTime> bodyDateTime = new Dictionary<uint, DateTime>();
        private readonly Dictionary<uint, List<uint>> groupsParameters = new Dictionary<uint, List<uint>>();
        private readonly Dictionary<uint, Dictionary<uint, double>> bodyToWeightedGroups = new Dictionary<uint, Dictionary<uint, double>>();
        private readonly List<uint> bodyRemoved = new List<uint>();
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegratedGroupsDetector"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration for the detector.</param>
        /// <param name="name">The name of the component.</param>
        public IntegratedGroupsDetector(Pipeline parent, IntegratedGroupsDetectorConfiguration? configuration = null, string name = nameof(IntegratedGroupsDetector))
        {
            this.name = name;
            this.configuration = configuration ?? new IntegratedGroupsDetectorConfiguration();
            this.In = parent.CreateReceiver<Dictionary<uint, List<uint>>>(this, this.Process, $"{name}-In");
            this.InRemovedBodies = parent.CreateReceiver<List<uint>>(this, this.ProcessBodiesRemoving, $"{name}-InRemovedBodies");
            this.Out = parent.CreateEmitter<Dictionary<uint, List<uint>>>(this, $"{name}-Out");
        }

        /// <summary>
        /// Gets the emitter of detected integrated groups.
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

        /// <summary>
        /// Processes instant group detections and integrates them over time using weighted scoring.
        /// </summary>
        /// <param name="instantGroups">Dictionary of instant group detections.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process(Dictionary<uint, List<uint>> instantGroups, Envelope envelope)
        {
            // Integrationg data
            // TO DO describ basic algo
            lock (this)
            {
                Dictionary<uint, List<uint>> groupsToRemove = new Dictionary<uint, List<uint>>();
                foreach (var newGroup in instantGroups)
                {
                    foreach (var group in this.groupsParameters)
                    {
                        if (newGroup.Value.Count <= group.Value.Count)
                        {
                            continue;
                        }

                        var intersection = group.Value.Intersect(newGroup.Value);
                        if (intersection.Any() && (group.Value.Count / intersection.Count()) >= this.configuration.IntersectionPercentage)
                        {
                            if (groupsToRemove.ContainsKey(newGroup.Key))
                            {
                                groupsToRemove[newGroup.Key].Add(group.Key);
                            }
                            else
                            {
                                groupsToRemove[newGroup.Key] = new List<uint> { group.Key };
                            }
                        }
                    }
                }

                foreach (var listToRemove in groupsToRemove)
                {
                    if (listToRemove.Value.Count > 1)
                    {
                        foreach (var groupToRemove in listToRemove.Value)
                        {
                            this.groupPairing[groupToRemove] = listToRemove.Key;
                        }
                    }

                    foreach (var toRemove in listToRemove.Value)
                    {
                        this.groupsParameters.Remove(toRemove);
                        foreach (var group in this.bodyToWeightedGroups)
                        {
                            group.Value.Remove(toRemove);
                        }
                    }
                }

                foreach (var group in instantGroups)
                {
                    if (group.Value.Intersect(this.bodyRemoved).Count() != 0)
                    {
                        continue;
                    }

                    if (!this.groupsParameters.ContainsKey(group.Key))
                    {
                        this.groupsParameters[group.Key] = group.Value.DeepClone();
                    }

                    foreach (var body in group.Value)
                    {
                        if (this.bodyToWeightedGroups.ContainsKey(body))
                        {
                            TimeSpan span = envelope.OriginatingTime - this.bodyDateTime[body];
                            if (this.bodyToWeightedGroups[body].ContainsKey(group.Key))
                            {
                                this.bodyToWeightedGroups[body][group.Key] += Math.Pow(span.TotalMilliseconds, this.configuration.IncreaseWeightFactor);
                            }
                            else
                            {
                                this.bodyToWeightedGroups[body].Add(group.Key, Math.Pow(span.TotalMilliseconds, this.configuration.IncreaseWeightFactor /** BodyToWeightedGroups[body].Count == 0 ? 10.0 :1.0 */));
                            }

                            if (this.groupPairing.ContainsKey(group.Key) && this.bodyToWeightedGroups[body].ContainsKey(this.groupPairing[group.Key]))
                            {
                                this.bodyToWeightedGroups[body][this.groupPairing[group.Key]] += Math.Pow(span.TotalMilliseconds, this.configuration.IncreaseWeightFactor);
                            }

                            for (uint iterator = 0; iterator < this.bodyToWeightedGroups[body].Count; iterator++)
                            {
                                uint key = this.bodyToWeightedGroups[body].ElementAt((int)iterator).Key;
                                if (key == group.Key)
                                {
                                    continue;
                                }

                                this.bodyToWeightedGroups[body][key] -= Math.Pow(span.TotalMilliseconds, this.configuration.DecreaseWeightFactor);
                            }

                            this.bodyDateTime[body] = envelope.OriginatingTime;
                        }
                        else
                        {
                            Dictionary<uint, double> nDic = new Dictionary<uint, double>();
                            nDic.Add(group.Key, this.configuration.IncreaseWeightFactor);
                            this.bodyToWeightedGroups.Add(body, nDic);
                            this.bodyDateTime.Add(body, envelope.OriginatingTime);
                        }
                    }
                }

                // Generating Interated Groups
                Dictionary<uint, List<uint>> integratedGroups = new Dictionary<uint, List<uint>>();
                foreach (var iterator in this.bodyToWeightedGroups)
                {
                    if (iterator.Value.Count == 0)
                    {
                        continue;
                    }

                    var list = iterator.Value.ToList();
                    list.Sort((x, y) => y.Value.CompareTo(x.Value));
                    uint groupId = list.ElementAt(0).Key;
                    if (integratedGroups.ContainsKey(groupId))
                    {
                        integratedGroups[groupId].Add(iterator.Key);
                    }
                    else
                    {
                        List<uint> groupList = new List<uint>();
                        groupList.Add(iterator.Key);
                        integratedGroups.Add(groupId, groupList);
                    }
                }

                this.Out.Post(integratedGroups, envelope.OriginatingTime);
            }
        }

        /// <summary>
        /// Processes removal of bodies from tracking, cleaning up associated group data.
        /// </summary>
        /// <param name="idsToRemove">List of body IDs to remove.</param>
        /// <param name="envelope">The message envelope.</param>
        private void ProcessBodiesRemoving(List<uint> idsToRemove, Envelope envelope)
        {
            lock (this)
            {
                foreach (uint id in idsToRemove)
                {
                    this.bodyDateTime.Remove(id);
                    if (this.bodyToWeightedGroups.ContainsKey(id))
                    {
                        foreach (var pair in this.bodyToWeightedGroups[id])
                        {
                            List<uint> bodiesInGroup = new List<uint>();
                            foreach (var body in this.bodyToWeightedGroups)
                            {
                                if (body.Key == id)
                                {
                                    continue;
                                }

                                if (body.Value.ContainsKey(pair.Key))
                                {
                                    bodiesInGroup.Add(body.Key);
                                }
                            }

                            if (bodiesInGroup.Count == 1)
                            {
                                this.bodyToWeightedGroups[bodiesInGroup.ElementAt(0)].Remove(pair.Key);
                            }

                            if (bodiesInGroup.Count != 0)
                            {
                                this.groupsParameters.Remove(pair.Key);
                            }
                        }
                    }

                    this.bodyToWeightedGroups.Remove(id);
                    this.bodyRemoved.Add(id);
                }
            }
        }
    }
}
