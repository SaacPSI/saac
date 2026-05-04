using System;
using System.Collections.Generic;
using System.IdentityModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Data;
using SAAC.Groups;
using SAAC.PipelineServices;

namespace SAAC.Components.CollaborationModules
{

    public class LOFConfiguration
    {
        public int NumberOfPeoples = 0;
        public Session SessionName;
    }

    public class LOF
    {
        private List<Tuple<Vector3, Vector3>> positionOrientationsIndividuals = new List<Tuple<Vector3, Vector3>>();
        private List<PersonSample> peopleSamples = new List<PersonSample>();
        private LOFConfiguration config;

        public LOF(Pipeline pipeline, DatasetPipeline server, LOFConfiguration? configuration = null)
        {
            this.config = configuration ?? new LOFConfiguration();
            this.HeadPositionOrientation1In = pipeline.CreateReceiver<Tuple<Vector3, Vector3>>(this, this.ProcessHeadPositionOrientation1In, nameof(this.HeadPositionOrientation1In));
            this.HeadPositionOrientation2In = pipeline.CreateReceiver<Tuple<Vector3, Vector3>>(this, this.ProcessHeadPositionOrientation2In, nameof(this.HeadPositionOrientation2In));
            this.HeadPositionOrientation3In = pipeline.CreateReceiver<Tuple<Vector3, Vector3>>(this, this.ProcessHeadPositionOrientation3In, nameof(this.HeadPositionOrientation3In));
            this.LOFOut = pipeline.CreateEmitter<LofRenderState>(this, nameof(this.LOFOut));

            for (int i = 0; i < this.config.NumberOfPeoples; i++)
            {
                this.positionOrientationsIndividuals.Add(new Tuple<Vector3, Vector3>(Vector3.Zero, Vector3.Zero));
                this.peopleSamples.Add(new PersonSample { Id = i, Position = Vector3.Zero, Direction = Vector3.Zero, Volume = 0 });
            }

            server.CreateConnectorAndStore("LOF", "LOFProcessing", this.config.SessionName, pipeline, this.LOFOut.Type, this.LOFOut, true);
        }


        public Receiver<Tuple<Vector3, Vector3>> HeadPositionOrientation1In { get; set; }

        public Receiver<Tuple<Vector3, Vector3>> HeadPositionOrientation2In { get; set; }

        public Receiver<Tuple<Vector3, Vector3>> HeadPositionOrientation3In { get; set; }

        public Emitter<LofRenderState> LOFOut { get; }

        public Receiver<Tuple<Vector3, Vector3>> GetHeadPositionOrientationReceiver(int id)
        {
            Receiver<Tuple<Vector3, Vector3>> receiver = null;

            switch (id)
            {
                case 0: receiver = this.HeadPositionOrientation1In;
                        break;
                case 1: receiver = this.HeadPositionOrientation2In;
                        break;
                case 2: receiver = this.HeadPositionOrientation3In;
                        break;
            }

            return receiver;
        }

        private void ProcessHeadPositionOrientation1In(Tuple<Vector3, Vector3> tuple, Envelope envelope)
        {
            this.AddHeadPositionOrientation(0, tuple);
        }

        private void ProcessHeadPositionOrientation2In(Tuple<Vector3, Vector3> tuple, Envelope envelope)
        {
            this.AddHeadPositionOrientation(1, tuple);
            if (this.positionOrientationsIndividuals[0] != null && this.positionOrientationsIndividuals[1] != null && this.positionOrientationsIndividuals[2] != null)
            {
                // this.TryComputeLOFV2();
                this.LOFOut.Post(this.ComputeLof(this.peopleSamples), envelope.OriginatingTime);
            }
        }

        private void ProcessHeadPositionOrientation3In(Tuple<Vector3, Vector3> tuple, Envelope envelope)
        {
            this.AddHeadPositionOrientation(2, tuple);
        }

        private void AddHeadPositionOrientation(int id, Tuple<Vector3, Vector3> tuple)
        {
            if (id < 0 || id >= this.config.NumberOfPeoples)
            {
                throw new ArgumentOutOfRangeException(nameof(id), $"ID must be between 0 and {this.config.NumberOfPeoples - 1}");
            }

            this.positionOrientationsIndividuals[id] = new Tuple<Vector3, Vector3>(new Vector3(tuple.Item1.X, tuple.Item1.Z, tuple.Item1.Y), new Vector3(tuple.Item2.X, tuple.Item2.Z, tuple.Item2.Y));
            this.peopleSamples[id] = new PersonSample
            {
                Id = id,
                Position = this.positionOrientationsIndividuals[id].Item1,
                Direction = this.DirectionFromUnityEuler(this.positionOrientationsIndividuals[id].Item2),
                Volume = 1.0f
            };
        }

        private Vector3 DirectionFromUnityEuler(Vector3 unityEuler)
        {
            double deg2rad = Math.PI / 180f;

            // swap Y and Z → yaw comes from Z
            double yaw = unityEuler.Z * deg2rad;

            return new Vector3(
                (float)Math.Sin(yaw),
                0,
                (float)Math.Cos(yaw));
        }

        private float[,] BuildLocusField(List<PersonSample> people, int width, int height)
        {
            var grid = new float[width, height];

            float minX = people.Min(p => p.Position.X) - 1;
            float maxX = people.Max(p => p.Position.X) + 1;
            float minZ = people.Min(p => p.Position.Z) - 1;
            float maxZ = people.Max(p => p.Position.Z) + 1;

            foreach (var p in people)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        Vector3 cell = this.CellToWorld(x, z, width, height, minX, maxX, minZ, maxZ);

                        Vector3 toCell = (cell - p.Position);
                        float dist = toCell.Length();

                        if (dist < 0.001f) continue;

                        toCell /= dist;

                        // directional alignment (like FOV weighting)
                        float alignment = Math.Max(0, Vector3.Dot(p.Direction, toCell));

                        // distance decay
                        float falloff = 1.0f / (1.0f + dist * dist);

                        float contribution = alignment * falloff * p.Volume;

                        grid[x, z] += contribution;
                    }
                }
            }

            return grid;
        }

        private Vector3 CellToWorld(int x, int z, int width, int height,
                    float minX, float maxX, float minZ, float maxZ)
        {
            float wx = minX + (x / (float)(width - 1)) * (maxX - minX);
            float wz = minZ + (z / (float)(height - 1)) * (maxZ - minZ);

            return new Vector3(wx, 0, wz);
        }

        private LofFeatures ExtractFeatures(float[,] grid, float threshold)
        {
            int w = grid.GetLength(0);
            int h = grid.GetLength(1);

            float sum = 0;
            float weightedX = 0;
            float weightedZ = 0;
            float mass = 0;

            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z < h; z++)
                {
                    float val = grid[x, z];
                    sum += val;

                    if (val > threshold)
                    {
                        mass += 1;
                        weightedX += x;
                        weightedZ += z;
                    }
                }
            }

            Vector3 centroid = mass > 0
                ? new Vector3(weightedX / mass, 0, weightedZ / mass)
                : Vector3.Zero;

            float density = mass > 0 ? sum / mass : 0;

            return new LofFeatures
            {
                Centroid = centroid,
                Mass = mass,
                Density = density
            };
        }

        private float ComputeLofScore(LofFeatures f)
        {
            // tune weights empirically
            return (f.Mass * 0.4f) +
                   (f.Density * 0.4f) +
                   (1.0f / (1.0f + f.Spread)) * 0.2f;
        }

        private LofRenderState ComputeLof(List<PersonSample> people)
        {
            var grid = this.BuildLocusField(people, 64, 64);

            var features = this.ExtractFeatures(grid, threshold: 0.5f);

            float score = this.ComputeLofScore(features);

            Console.WriteLine($"Person1_ Position: {people[0].Position}, Direction: {people[0].Direction}\nPerson2_ Position: {people[1].Position}, Direction: {people[1].Direction}\nPerson3_ Position: {people[2].Position}, Direction: {people[2].Direction}\n");
            Console.WriteLine($"LOF Score: {score}, Mass: {features.Mass}, Density: {features.Density}");
            return new LofRenderState
            {
                Score = score,
                Centroid = features.Centroid,
                Mass = features.Mass,
                Density = features.Density,
                Persons = people
            };
        }

        private void TryComputeLOF(Envelope envelope)
        {
            var score = this.ComputeLOF(envelope);
        }

        private static Vector2 To2dPosition(Vector3 p) => new Vector2(p.X, p.Z);

        private static Vector2 To2DDirectionFromForward(Vector3 forward)
        {
            var v = new Vector2(forward.X, forward.Z);
            if (v.LengthSquared() < 1e-8f)
            {
                return Vector2.UnitX;
            }

            return Vector2.Normalize(v);
        }

        private static Vector2 FromYawDegrees(float yawDegrees)
        {
            double yaw = yawDegrees * Math.PI / 180.0;
            return Vector2.Normalize(new Vector2((float)Math.Sin(yaw), (float)Math.Cos(yaw)));
        }

        private static double ComputeLlofScore(List<LofPersonRenderState> people)
        {
            if (people == null || people.Count < 2)
            {
                return 0.0;
            }

            double sum = 0.0;
            double weightSum = 0.0;

            for (int i = 0; i < people.Count; i++)
            {
                for (int j = i + 1; j < people.Count; j++)
                {
                    var pi = people[i];
                    var pj = people[j];

                    var delta = pj.Position - pi.Position;
                    float dist = delta.Length();
                    if (dist < 1e-6f)
                    {
                        continue;
                    }

                    var dirIJ = Vector2.Normalize(delta);
                    var dirJI = -dirIJ;

                    double fi = pi.Forward.LengthSquared() > 1e-6f
                        ? Vector2.Dot(Vector2.Normalize(pi.Forward), dirIJ)
                        : 0.0;

                    double fj = pj.Forward.LengthSquared() > 1e-6f
                        ? Vector2.Dot(Vector2.Normalize(pj.Forward), dirJI)
                        : 0.0;

                    double pairScore = 0.5 * (fi + fj);
                    double weight = 1.0 / (1.0 + dist);

                    sum += pairScore * weight;
                    weightSum += weight;
                }
            }

            return weightSum > 0.0 ? sum / weightSum : 0.0;
        }

        private LofScore ComputeLOF(Envelope envelope)
        {
            var p1 = this.positionOrientationsIndividuals[0];
            var p2 = this.positionOrientationsIndividuals[1];

            Vector3 position1 = new Vector3(p1.Item1.X, p1.Item1.Z, p1.Item1.Y);
            Vector3 forward1 = new Vector3(p1.Item2.X, p1.Item2.Z, p1.Item2.Y);
            Vector3 position2 = new Vector3(p2.Item1.X, p2.Item1.Z, p2.Item1.Y);
            Vector3 forward2 = new Vector3(p2.Item2.X, p2.Item2.Z, p2.Item2.Y); ;

            Vector3 delta = position2 - position1;
            double distance = delta.Length();

            Vector3 direction12 = distance > 1e-6f ? Vector3.Normalize(delta) : Vector3.Zero;
            Vector3 direction21 = -direction12;

            float facingScore1 = forward1.LengthSquared() > 1e-6f ? Vector3.Dot(Vector3.Normalize(forward1), direction12) : 0f;
            float facingScore2 = forward2.LengthSquared() > 1e-6f ? Vector3.Dot(Vector3.Normalize(forward2), direction21) : 0f;

            double score = 0.5 * (facingScore1 + facingScore2);

            return new LofScore
            {
                FrameTime = envelope.OriginatingTime,
                MuX = position1.X,
                MuY = position1.Y,
                Vx = forward1.X,
                Vy = forward1.Y,
                Ux = forward2.X,
                Uy = forward2.Y,
                Mass = distance,
                Density = score,
                AspectRatio = 1.0,
                KfX = position1.X,
                KfY = position1.Y,
                KfVx = forward1.X,
                KfVy = forward1.Y,
                KfVel = forward1.Length(),
                Score = score
            };
        }
    }
}
