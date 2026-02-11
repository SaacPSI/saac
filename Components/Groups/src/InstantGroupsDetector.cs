// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that detects groups instantly based on proximity (distance threshold between bodies).
    /// </summary>
    public class InstantGroupsDetector : IConsumerProducer<Dictionary<uint, Vector3D>, Dictionary<uint, List<uint>>>
    {
        private readonly InstantGroupsDetectorConfiguration configuration;
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstantGroupsDetector"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration for the detector.</param>
        /// <param name="name">The name of the component.</param>
        public InstantGroupsDetector(Pipeline parent, InstantGroupsDetectorConfiguration? configuration = null, string name = nameof(InstantGroupsDetector))
        {
            this.name = name;
            this.configuration = configuration ?? new InstantGroupsDetectorConfiguration();
            this.In = parent.CreateReceiver<Dictionary<uint, Vector3D>>(this, this.Process, $"{name}-In");
            this.Out = parent.CreateEmitter<Dictionary<uint, List<uint>>>(this, $"{name}-Out");
        }

        /// <summary>
        /// Gets the emitter of detected groups.
        /// </summary>
        public Emitter<Dictionary<uint, List<uint>>> Out { get; private set; }

        /// <summary>
        /// Gets the receiver that encapsulates the input positions.
        /// </summary>
        public Receiver<Dictionary<uint, Vector3D>> In { get; private set; }

        /// <summary>
        /// Gets the configuration for the detector.
        /// </summary>
        public InstantGroupsDetectorConfiguration Configuration => this.configuration;

        /// <summary>
        /// Processes body positions and detects groups based on distance threshold.
        /// </summary>
        /// <param name="skeletons">Dictionary of body positions.</param>
        /// <param name="envelope">The message envelope.</param>
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
                    if (distance > this.configuration.DistanceThreshold)
                    {
                        continue;
                    }

                    if (rawGroups.ContainsKey(idBody1))
                    {
                        rawGroups[idBody1].Add(idBody2);
                    }
                    else
                    {
                        List<uint> group =
                            [idBody1, idBody2];
                        rawGroups.Add(idBody1, group);
                    }

                    if (rawGroups.ContainsKey(idBody2))
                    {
                        rawGroups[idBody2].Add(idBody1);
                    }
                    else
                    {
                        List<uint> group =
                            [idBody1, idBody2];
                        rawGroups.Add(idBody2, group);
                    }
                }
            }

            this.ReduceGroups(ref rawGroups);

            Dictionary<uint, List<uint>> outData = new Dictionary<uint, List<uint>>();
            foreach (var rawGroup in rawGroups)
            {
                rawGroup.Value.Sort();
                List<uint> group = rawGroup.Value.Distinct().ToList();
                uint uid = GroupsHelpers.CantorParingSequence(group);
                outData.Add(uid, group);
            }

            this.Out.Post(outData, envelope.OriginatingTime);
        }

        /// <summary>
        /// Reduces and merges overlapping groups by combining groups that share members.
        /// </summary>
        /// <param name="groups">The dictionary of groups to reduce.</param>
        protected void ReduceGroups(ref Dictionary<uint, List<uint>> groups)
        {
            // bool call = true;
            // while (call)
            // {
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
                {
                    break;
                }
            }

            // }
            if (call)
            {
                this.ReduceGroups(ref groups);
            }
        }
    }
}
