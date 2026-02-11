// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that calculates intersection points and time ratios between flock groups based on their trajectories.
    /// </summary>
    public class FlockGroupIntersection : IConsumerProducer<Dictionary<uint, SimplifiedFlockGroup>, Dictionary<(uint, uint), double>>
    {
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlockGroupIntersection"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public FlockGroupIntersection(Pipeline parent, string name = nameof(FlockGroupIntersection))
        {
            this.name = name;
            this.In = parent.CreateReceiver<Dictionary<uint, SimplifiedFlockGroup>>(this, this.Process, $"{name}-In");
            this.Out = parent.CreateEmitter<Dictionary<(uint, uint), double>>(this, $"{name}-Out");
        }

        /// <summary>
        /// Gets the emitter of group intersection ratios.
        /// </summary>
        public Emitter<Dictionary<(uint, uint), double>> Out { get; private set; }

        /// <summary>
        /// Gets the receiver for input flock groups.
        /// </summary>
        public Receiver<Dictionary<uint, SimplifiedFlockGroup>> In { get; private set; }

        /// <summary>
        /// Processes flock groups and calculates trajectory intersections between pairs of groups.
        /// </summary>
        /// <param name="groups">Dictionary of flock groups.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process(Dictionary<uint, SimplifiedFlockGroup> groups, Envelope envelope)
        {
            Dictionary<(uint, uint), double> intersectors = new Dictionary<(uint, uint), double>();
            for (int iterator1 = 0; iterator1 < groups.Count; iterator1++)
            {
                SimplifiedFlockGroup groupA = groups.ElementAt(iterator1).Value;
                Line2D lineA = new Line2D(groupA.Area.Center, groupA.Area.Center + groupA.Direction);
                for (int iterator2 = iterator1 + 1; iterator2 < groups.Count; iterator2++)
                {
                    SimplifiedFlockGroup groupB = groups.ElementAt(iterator2).Value;
                    Line2D lineB = new Line2D(groupB.Area.Center, groupB.Area.Center + groupB.Direction);
                    Point2D? intersection = lineA.IntersectWith(lineB);
                    if (intersection != null)
                    {
                        double ratio = groupA.Velocity / (intersection.Value - groupA.Area.Center).Length *
                                      groupB.Velocity / (intersection.Value - groupB.Area.Center).Length;
                        intersectors.Add((groupA.Id, groupB.Id), ratio);
                    }
                }
            }

            if (intersectors.Count > 0)
            {
                this.Out.Post(intersectors, envelope.OriginatingTime);
            }
        }
    }
}
