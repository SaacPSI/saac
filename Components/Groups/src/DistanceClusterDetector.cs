using Microsoft.Psi;
using Microsoft.Psi.Components;
using MathNet.Spatial.Euclidean;

namespace SAAC.Groups
{
    public class DistanceClusterDetector : IConsumerProducer<Dictionary<uint, Vector3D>, Dictionary<uint, DistanceClusterDefinition>>
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, DistanceClusterDefinition>> Out { get; private set; }

        /// <summary>
        /// Gets the  connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<Dictionary<uint, Vector3D>> InConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of skeletons
        /// </summary>
        public Receiver<Dictionary<uint, Vector3D>> In => InConnector.In;

        public DistanceClusterDetectorConfiguration Configuration { get; set; }


        private Dictionary<uint, Queue<Point2D>> BodiesMemory;

        public DistanceClusterDetector(Pipeline parent, DistanceClusterDetectorConfiguration? configuration = null)
        {
            Configuration = configuration ?? new DistanceClusterDetectorConfiguration();
            InConnector = parent.CreateConnector<Dictionary<uint, Vector3D>>(nameof(InConnector));
            Out = parent.CreateEmitter<Dictionary<uint, DistanceClusterDefinition>>(this, nameof(Out));
            InConnector.Out.Do(Process);
        }

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
                    if (distance > Configuration.DistanceThreshold)
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

            Dictionary<uint, DistanceClusterDefinition> outData = new Dictionary<uint, DistanceClusterDefinition>();
            foreach (var group in GroupsHelpers.GenerateGroups(ref rawGroups))
            {
                List<Point3D> points = new List<Point3D>();
                foreach (var id in group.Value)
                    points.Add(skeletons[id].ToPoint3D());
                Point3D centroid = Point3D.Centroid(points);
                outData.Add(group.Key, new DistanceClusterDefinition(group.Value, new Circle3D(centroid, UnitVector3D.YAxis, Configuration.DistanceThreshold)));
            }

            Out.Post(outData, envelope.OriginatingTime);
        }

        private void UpdateMemory(Dictionary<uint, Vector3D> skeletons, Envelope envelope)
        {
            foreach(var skeleton in skeletons)
            {

            }
        }
    }
}
