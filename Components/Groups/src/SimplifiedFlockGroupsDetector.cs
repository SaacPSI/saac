// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using MathNet.Numerics.Statistics;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Simplified model detection for detecting groups inspired by Flock agents (https://ieeexplore.ieee.org/abstract/document/1605401).
    /// Uses a variant of Reynolds rules:
    /// * Flock Centering: attempt to stay close to nearby flockmates (distance).
    /// * Collision Avoidance: avoid collisions with nearby flockmates (same direction in movement).
    /// * Velocity Matching: attempt to match velocity with nearby flockmates (velocity).
    /// </summary>
    public class SimplifiedFlockGroupsDetector : IConsumerProducer<Dictionary<uint, Vector3D>, Dictionary<uint, SimplifiedFlockGroup>>
    {
        private readonly SimplifiedFlockGroupsDetectorConfiguration configuration;
        private readonly Dictionary<uint, Queue<Vector2D>> bodiesMemory;
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimplifiedFlockGroupsDetector"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration for the detector.</param>
        /// <param name="name">The name of the component.</param>
        public SimplifiedFlockGroupsDetector(Pipeline parent, SimplifiedFlockGroupsDetectorConfiguration? configuration = null, string name = nameof(SimplifiedFlockGroupsDetector))
        {
            this.name = name;
            this.bodiesMemory = new Dictionary<uint, Queue<Vector2D>>();
            this.configuration = configuration ?? new SimplifiedFlockGroupsDetectorConfiguration();
            this.In = parent.CreateReceiver<Dictionary<uint, Vector3D>>(this, this.Process, $"{name}-In");
            this.Out = parent.CreateEmitter<Dictionary<uint, SimplifiedFlockGroup>>(this, $"{name}-Out");
        }

        /// <summary>
        /// Gets the emitter of detected groups.
        /// </summary>
        public Emitter<Dictionary<uint, SimplifiedFlockGroup>> Out { get; private set; }

        /// <summary>
        /// Gets the receiver that encapsulates the input dictionary of body positions.
        /// </summary>
        public Receiver<Dictionary<uint, Vector3D>> In { get; private set; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(Dictionary<uint, Vector3D> skeletons, Envelope envelope)
        {
            if (skeletons.Count == 0)
            {
                return;
            }

            this.UpdateMemory(skeletons);
            Dictionary<uint, (Vector2D, double, Vector2D)> rawData = new Dictionary<uint, (Vector2D, double, Vector2D)>();
            foreach (var body in this.bodiesMemory)
            {
                var means = this.MeanVectors(body.Value, this.configuration.QueueMaxCount / 2);
                if (means.Count < 2)
                {
                    continue;
                }

                var (velocities, directions) = this.CalculateDistancesAndDirections(means);
                rawData.Add(body.Key, (means.First(), velocities.First(), directions.First()));
            }

            Dictionary<uint, List<uint>> rawGroups = new Dictionary<uint, List<uint>>();
            for (int iterator1 = 0; iterator1 < rawData.Count; iterator1++)
            {
                for (int iterator2 = iterator1 + 1; iterator2 < rawData.Count; iterator2++)
                {
                    uint idBody1 = rawData.ElementAt(iterator1).Key;
                    uint idBody2 = rawData.ElementAt(iterator2).Key;

                    // Distance
                    double distance = MathNet.Numerics.Distance.Euclidean(rawData[idBody1].Item1.ToVector(), rawData[idBody2].Item1.ToVector());

                    // Velocity
                    double velocity = Math.Abs(rawData[idBody1].Item2 - rawData[idBody2].Item2);

                    // Direction
                    double direction = rawData[idBody1].Item3.AngleTo(rawData[idBody2].Item3).Radians;

                    double distanceComponent = this.configuration.DistanceWeight * this.CalculateBaseModelComponent(distance);
                    double velocityComponent = this.configuration.VelocityWeight * this.CalculateBaseModelComponent(velocity);
                    double directionComponent = this.configuration.DirectionWeight * this.CalculateBaseModelComponent(direction);
                    var modelValue = distanceComponent + velocityComponent + directionComponent;

                    if (modelValue < this.configuration.ModelThreshold)
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

            Dictionary<uint, SimplifiedFlockGroup> outData = new Dictionary<uint, SimplifiedFlockGroup>();
            foreach (var group in GroupsHelpers.GenerateGroups(ref rawGroups))
            {
                List<Point2D> positions = new List<Point2D>();
                List<double> velocities = new List<double>();
                (List<double>, List<double>) directions = (new List<double>(), new List<double>());
                double maxDist = 0;
                foreach (var id in group.Value)
                {
                    var vectPos = rawData[id].Item1.ToVector();
                    foreach (Point2D pos in positions)
                    {
                        double distance = MathNet.Numerics.Distance.Euclidean(vectPos, pos.ToVector());
                        if (maxDist < distance)
                        {
                            maxDist = distance;
                        }
                    }

                    positions.Add(Point2D.OfVector(vectPos));
                    velocities.Add(rawData[id].Item2);
                    directions.Item1.Add(rawData[id].Item3.X);
                    directions.Item2.Add(rawData[id].Item3.Y);
                }

                outData.Add(group.Key, new SimplifiedFlockGroup(group.Key, group.Value, new Circle2D(Point2D.Centroid(positions), maxDist), new Vector2D(directions.Item1.Mean(), directions.Item2.Mean()), velocities.Mean()));
            }

            this.Out.Post(outData, envelope.OriginatingTime);
        }

        private void UpdateMemory(Dictionary<uint, Vector3D> skeletons)
        {
            // We are not tacking account of drop of tracking here
            // Adding this might raise to much the complexity.
            foreach (var skeleton in skeletons)
            {
                if (!this.bodiesMemory.ContainsKey(skeleton.Key))
                {
                    this.bodiesMemory.Add(skeleton.Key, new Queue<Vector2D>());
                }

                this.bodiesMemory[skeleton.Key].Enqueue(new Vector2D(skeleton.Value.X, skeleton.Value.Z));
                while (this.bodiesMemory[skeleton.Key].Count > this.configuration.QueueMaxCount)
                {
                    this.bodiesMemory[skeleton.Key].Dequeue();
                }
            }
        }

        private List<Vector2D> MeanVectors(in Queue<Vector2D> queue, in uint numberInMeans = 30)
        {
            List<Vector2D> points = new List<Vector2D>();
            double x = 0, y = 0;
            int count = 0;
            foreach (Vector2D point in queue)
            {
                x += point.X;
                y += point.Y;
                count++;
                if (count >= numberInMeans)
                {
                    points.Add(new Vector2D(x / count, y / count));
                    x = y = count = 0;
                }
            }

            if (count != 0)
            {
                points.Add(new Vector2D(x / count, y / count));
            }

            points.Reverse();
            return points;
        }

        private (List<double>, List<Vector2D>) CalculateDistancesAndDirections(in List<Vector2D> points)
        {
            List<double> velocities = new List<double>();
            List<Vector2D> directions = new List<Vector2D>();
            for (int iterator = 1; iterator < points.Count; iterator++)
            {
                var vect = points.ElementAt(iterator) - points.ElementAt(iterator - 1);
                velocities.Add(vect.Length);
                directions.Add(vect.Normalize());
            }

            velocities.Reverse();
            directions.Reverse();
            return (velocities, directions);
        }

        private double CalculateBaseModelComponent(in double input)
        {
            double baseComponent = 1.0 / Math.Exp(input);
            return (baseComponent > 1.0 ? 1.0 : baseComponent);
        }
    }
}
