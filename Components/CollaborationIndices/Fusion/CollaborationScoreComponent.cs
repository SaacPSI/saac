using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// A dimension of collaboration, defined as a set of named indices and the way they combine.
    /// </summary>
    public class ScoreDimension
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Names of the indices that feed this dimension.</summary>
        public List<string> IndexNames { get; set; } = new List<string>();

        /// <summary>Optional weight per index. Missing entries default to 1.</summary>
        public Dictionary<string, double> Weights { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// If true, each index is passed through log(1 + s) before averaging, which compresses
        /// the high values and was the behaviour of the legacy dimension scores.
        /// </summary>
        public bool UseLogCompression { get; set; } = true;

        /// <summary>
        /// Indices that are only included when their validity flag is true (see SetValidity).
        /// A dimension whose indices are all invalid is excluded from the global score.
        /// </summary>
        public List<string> ConditionalIndexNames { get; set; } = new List<string>();

        public double WeightFor(string indexName)
            => this.Weights != null && this.Weights.TryGetValue(indexName, out double weight) ? weight : 1.0;
    }

    public class CollaborationScoreConfiguration : IndexComponentConfiguration
    {
        public CollaborationScoreConfiguration()
        {
            this.ComputationInterval = TimeSpan.Zero;
        }

        public List<ScoreDimension> Dimensions { get; set; } = new List<ScoreDimension>();

        /// <summary>
        /// Global score computed as the mean of the dimensions (true) or as the mean of all the
        /// individual indices (false, the legacy "collaboration score").
        /// </summary>
        public bool GlobalFromDimensions { get; set; } = true;

        /// <summary>Indices used by the flat global score when GlobalFromDimensions is false.</summary>
        public List<string> GlobalIndexNames { get; set; } = new List<string>();

        /// <summary>Value published when an index is missing.</summary>
        public double MissingIndexValue { get; set; } = 0;
    }

    /// <summary>
    /// Fusion of the normalized indices into dimension scores and a global collaboration score.
    ///
    /// The legacy CheckGlobalScore hard coded s1..s12, the four dimensions and the four
    /// branches handling the usability of the equality indices. Here the dimensions are
    /// declared in the configuration, and an index can be marked invalid at runtime through
    /// SetValidityIn, which excludes it from its dimension and from the global score.
    ///
    /// Inputs:
    ///  - IndexIn: a named index value (name, value);
    ///  - IndicesIn: several at once;
    ///  - ValidityIn: (name, isUsable).
    ///
    /// Outputs:
    ///  - Out: global score;
    ///  - DimensionsOut: every dimension score;
    ///  - GetDimensionEmitter(name): dedicated stream.
    /// </summary>
    public class CollaborationScoreComponent : IndexComponentBase<CollaborationScoreConfiguration>,
                                               IProducer<double>
    {
        private readonly Dictionary<string, double> indices = new Dictionary<string, double>();
        private readonly Dictionary<string, bool> validity = new Dictionary<string, bool>();
        private readonly KeyedEmitters<string> dimensionEmitters;

        private double? lastGlobal;
        private Dictionary<string, double> lastDimensionScores = new Dictionary<string, double>();

        public CollaborationScoreComponent(Pipeline pipeline, CollaborationScoreConfiguration configuration, string name = nameof(CollaborationScoreComponent))
            : base(pipeline, configuration, name)
        {
            this.IndexIn = pipeline.CreateReceiver<Tuple<string, double>>(this, this.ReceiveIndex, $"{name}-IndexIn");
            this.IndicesIn = pipeline.CreateReceiver<Dictionary<string, double>>(this, this.ReceiveIndices, $"{name}-IndicesIn");
            this.ValidityIn = pipeline.CreateReceiver<Tuple<string, bool>>(this, this.ReceiveValidity, $"{name}-ValidityIn");
            this.SnapshotIn = pipeline.CreateReceiver<IndexSnapshot>(this, this.ReceiveSnapshot, $"{name}-SnapshotIn");

            this.Out = pipeline.CreateEmitter<double>(this, $"{name}-Global");
            this.DimensionsOut = pipeline.CreateEmitter<Dictionary<string, double>>(this, $"{name}-Dimensions");
            this.NormalizedIndicesOut = pipeline.CreateEmitter<Dictionary<string, double>>(this, $"{name}-Indices");
            this.SnapshotOut = pipeline.CreateEmitter<IndexSnapshot>(this, $"{name}-Snapshot");
            this.dimensionEmitters = new KeyedEmitters<string>(pipeline, this, configuration.Dimensions.Select(d => d.Name), $"{name}-Dimension");
        }

        public Receiver<Tuple<string, double>> IndexIn { get; }

        public Receiver<Dictionary<string, double>> IndicesIn { get; }

        public Receiver<Tuple<string, bool>> ValidityIn { get; }

        /// <summary>
        /// All the indices of one tick at once. This is the input to prefer: fed one index at
        /// a time (GetIndexInput), the score is computed as soon as the first index of a tick
        /// arrives, with the previous value of the others.
        /// </summary>
        public Receiver<IndexSnapshot> SnapshotIn { get; }

        /// <summary>
        /// The snapshot received on SnapshotIn, completed with the global score (under
        /// IndexNames.CollaborationScore) and the dimension scores in its Group values, so
        /// that a graph or an export chained behind stays aligned on the same tick.
        /// </summary>
        public Emitter<IndexSnapshot> SnapshotOut { get; }

        public Emitter<double> Out { get; }

        public Emitter<Dictionary<string, double>> DimensionsOut { get; }

        /// <summary>Snapshot of every index used for the last score, for export and debugging.</summary>
        public Emitter<Dictionary<string, double>> NormalizedIndicesOut { get; }

        public Emitter<double> GetDimensionEmitter(string dimensionName) => this.dimensionEmitters[dimensionName];

        /// <summary>
        /// Creates a receiver that feeds one named index, so that an indicator component can be
        /// piped directly: activity.GroupActivityLevelOut.PipeTo(score.GetIndexInput("Movement")).
        /// </summary>
        public Receiver<double> GetIndexInput(string indexName)
        {
            string key = indexName;
            return this.pipeline.CreateReceiver<double>(
                this,
                (value, envelope) =>
                {
                    this.indices[key] = value;
                    this.TryCompute(envelope.OriginatingTime);
                },
                $"{this.name}-In-{key}");
        }

        /// <summary>Creates a receiver that enables or disables one named index.</summary>
        public Receiver<bool> GetValidityInput(string indexName)
        {
            string key = indexName;
            return this.pipeline.CreateReceiver<bool>(this, (isValid, _) => this.validity[key] = isValid, $"{this.name}-Valid-{key}");
        }

        protected override void Compute(DateTime originatingTime)
        {
            if (this.indices.Count == 0)
            {
                this.HasPublished = false;
                return;
            }

            var dimensionScores = new Dictionary<string, double>();

            foreach (ScoreDimension dimension in this.configuration.Dimensions)
            {
                double weightedSum = 0;
                double totalWeight = 0;

                foreach (string indexName in dimension.IndexNames)
                {
                    if (!this.IsUsable(dimension, indexName, out double value))
                    {
                        continue;
                    }

                    double weight = dimension.WeightFor(indexName);
                    double contribution = dimension.UseLogCompression ? Math.Log(1 + value) : value;
                    weightedSum += weight * contribution;
                    totalWeight += weight;
                }

                if (totalWeight > double.Epsilon)
                {
                    dimensionScores[dimension.Name] = weightedSum / totalWeight;
                }
            }

            this.DimensionsOut.Post(dimensionScores, originatingTime);
            this.dimensionEmitters.PostAll(dimensionScores, originatingTime);

            double global;
            if (this.configuration.GlobalFromDimensions)
            {
                global = dimensionScores.Count > 0 ? dimensionScores.Values.Average() : 0;
            }
            else
            {
                var used = new List<double>();
                foreach (string indexName in this.configuration.GlobalIndexNames)
                {
                    if (this.indices.TryGetValue(indexName, out double value) && this.IsValid(indexName))
                    {
                        used.Add(value);
                    }
                }

                global = used.Count > 0 ? used.Average() : 0;
            }

            this.lastGlobal = global;
            this.lastDimensionScores = dimensionScores;

            this.Out.Post(global, originatingTime);
            this.NormalizedIndicesOut.Post(new Dictionary<string, double>(this.indices), originatingTime);
        }

        private void ReceiveSnapshot(IndexSnapshot snapshot, Envelope envelope)
        {
            if (snapshot == null)
            {
                return;
            }

            foreach (var entry in snapshot.ScoreInputs)
            {
                this.indices[entry.Key] = entry.Value;
            }

            foreach (var entry in snapshot.Validity)
            {
                this.validity[entry.Key] = entry.Value;
            }

            bool computed = this.TryCompute(envelope.OriginatingTime);

            // A copy: the received snapshot goes back to \psi once this handler returns.
            IndexSnapshot completed = snapshot.Clone();
            if (computed && this.lastGlobal.HasValue)
            {
                completed.Group[IndexNames.CollaborationScore] = this.lastGlobal.Value;
                foreach (var entry in this.lastDimensionScores)
                {
                    completed.Group[entry.Key] = entry.Value;
                }
            }

            this.SnapshotOut.Post(completed, envelope.OriginatingTime);
        }

        private bool IsUsable(ScoreDimension dimension, string indexName, out double value)
        {
            if (!this.indices.TryGetValue(indexName, out value))
            {
                value = this.configuration.MissingIndexValue;
                return false;
            }

            if (dimension.ConditionalIndexNames != null && dimension.ConditionalIndexNames.Contains(indexName) && !this.IsValid(indexName))
            {
                return false;
            }

            return true;
        }

        private bool IsValid(string indexName) => !this.validity.TryGetValue(indexName, out bool valid) || valid;

        private void ReceiveIndex(Tuple<string, double> value, Envelope envelope)
        {
            if (value == null)
            {
                return;
            }

            this.indices[value.Item1] = value.Item2;
            this.TryCompute(envelope.OriginatingTime);
        }

        private void ReceiveIndices(Dictionary<string, double> values, Envelope envelope)
        {
            if (values == null)
            {
                return;
            }

            foreach (var entry in values)
            {
                this.indices[entry.Key] = entry.Value;
            }

            this.TryCompute(envelope.OriginatingTime);
        }

        private void ReceiveValidity(Tuple<string, bool> value, Envelope envelope)
        {
            if (value != null)
            {
                this.validity[value.Item1] = value.Item2;
            }
        }
    }
}
