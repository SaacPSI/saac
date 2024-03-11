using Microsoft.Psi.Components;
using Microsoft.Psi;
using MathNet.Spatial.Euclidean;
using System.Collections.Generic;

namespace SAAC.Groups
{
    public class FlockGroupIntersection : IConsumerProducer<Dictionary<uint, SimplifiedFlockGroup>, Dictionary<(uint, uint), double>>
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<(uint, uint), double>> Out { get; private set; }

        /// <summary>
        /// Gets the connector of lists of flock groups.
        /// </summary>
        private Connector<Dictionary<uint, SimplifiedFlockGroup>> InConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of groups
        /// </summary>
        public Receiver<Dictionary<uint, SimplifiedFlockGroup>> In => InConnector.In;

        public FlockGroupIntersection(Pipeline parent)
        {
            InConnector = parent.CreateConnector<Dictionary<uint, SimplifiedFlockGroup>>(nameof(InConnector));
            Out = parent.CreateEmitter<Dictionary<(uint, uint), double>>(this, nameof(Out));
            InConnector.Out.Do(Process);
        }

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
                Out.Post(intersectors, envelope.OriginatingTime);
        }
    }
}
