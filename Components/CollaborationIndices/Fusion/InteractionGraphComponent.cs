using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Node of the interaction graph: everything measured on one participant.
    /// Metrics are stored by name so that adding an indicator does not change the type.
    /// </summary>
    public class IndexNode
    {
        public uint ParticipantId { get; set; }

        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();

        public double Get(string metric) => this.Metrics.TryGetValue(metric, out double value) ? value : 0;
    }

    /// <summary>
    /// Edge of the interaction graph: everything measured on a pair. Directed metrics
    /// (gaze from A to B) are stored in DirectedMetrics with the direction as key.
    /// </summary>
    public class IndexEdge
    {
        public ParticipantPair Pair { get; set; }

        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();

        public Dictionary<string, double> ForwardMetrics { get; set; } = new Dictionary<string, double>();

        public Dictionary<string, double> BackwardMetrics { get; set; } = new Dictionary<string, double>();

        public double Get(string metric) => this.Metrics.TryGetValue(metric, out double value) ? value : 0;
    }

    /// <summary>Group level metrics.</summary>
    public class IndexGroup
    {
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();
    }

    /// <summary>Complete snapshot of the interaction state at one instant.</summary>
    public class InteractionGraph
    {
        public DateTime OriginatingTime { get; set; }

        public List<IndexNode> Nodes { get; set; } = new List<IndexNode>();

        public List<IndexEdge> Edges { get; set; } = new List<IndexEdge>();

        public IndexGroup Group { get; set; } = new IndexGroup();
    }

    public class InteractionGraphConfiguration : IndexComponentConfiguration
    {
        public InteractionGraphConfiguration()
        {
            this.ComputationInterval = TimeSpan.Zero;
        }

        /// <summary>Also publish a flat text version of the graph, as the legacy gramStringOut did.</summary>
        public bool PublishTextVersion { get; set; } = true;

        /// <summary>Per participant indices read from a snapshot. Null keeps them all.</summary>
        public List<string>? NodeMetrics { get; set; } = null;

        /// <summary>Per pair indices read from a snapshot. Null keeps them all.</summary>
        public List<string>? EdgeMetrics { get; set; } = null;

        /// <summary>Per ordered pair indices read from a snapshot. Null keeps them all.</summary>
        public List<string>? DirectedEdgeMetrics { get; set; } = null;

        /// <summary>Group level indices read from a snapshot. Null keeps them all.</summary>
        public List<string>? GroupMetrics { get; set; } = null;
    }

    /// <summary>
    /// Assembles the outputs of all the indicator components into a single graph message.
    ///
    /// The legacy pipeline built PersonNodes / PersonEdges by writing into fixed array indices
    /// (personEdges[0] is AB, [1] is AC, [2] is BC), which is what tied the whole class to
    /// exactly three participants. Here nodes and edges are keyed by participant and by pair,
    /// and each indicator declares the metric it feeds by name.
    ///
    /// Usage:
    ///   activity.Out.PipeTo(graph.GetNodeMetricInput("Movement"));
    ///   synchrony.Out.PipeTo(graph.GetEdgeMetricInput("Synchrony"));
    ///   gaze.DirectedPairOut.PipeTo(graph.GetDirectedEdgeMetricInput("GazeOnPeers"));
    ///   score.Out.PipeTo(graph.GetGroupMetricInput("CollaborationScore"));
    /// </summary>
    public class InteractionGraphComponent : IndexComponentBase<InteractionGraphConfiguration>,
                                             IProducer<InteractionGraph>
    {
        private readonly Dictionary<uint, Dictionary<string, double>> nodeMetrics = new Dictionary<uint, Dictionary<string, double>>();
        private readonly Dictionary<ParticipantPair, Dictionary<string, double>> edgeMetrics = new Dictionary<ParticipantPair, Dictionary<string, double>>();
        private readonly Dictionary<ParticipantPair, Dictionary<string, double>> forwardMetrics = new Dictionary<ParticipantPair, Dictionary<string, double>>();
        private readonly Dictionary<ParticipantPair, Dictionary<string, double>> backwardMetrics = new Dictionary<ParticipantPair, Dictionary<string, double>>();
        private readonly Dictionary<string, double> groupMetrics = new Dictionary<string, double>();

        public InteractionGraphComponent(Pipeline pipeline, InteractionGraphConfiguration configuration, string name = nameof(InteractionGraphComponent))
            : base(pipeline, configuration, name)
        {
            this.Out = pipeline.CreateEmitter<InteractionGraph>(this, $"{name}-Graph");
            this.TextOut = pipeline.CreateEmitter<string>(this, $"{name}-GraphText");
            this.SnapshotIn = pipeline.CreateReceiver<IndexSnapshot>(this, this.ReceiveSnapshot, $"{name}-SnapshotIn");

            foreach (uint participantId in configuration.ParticipantIds)
            {
                this.nodeMetrics[participantId] = new Dictionary<string, double>();
            }

            foreach (ParticipantPair pair in configuration.Pairs())
            {
                this.edgeMetrics[pair] = new Dictionary<string, double>();
                this.forwardMetrics[pair] = new Dictionary<string, double>();
                this.backwardMetrics[pair] = new Dictionary<string, double>();
            }
        }

        public Emitter<InteractionGraph> Out { get; }

        public Emitter<string> TextOut { get; }

        /// <summary>
        /// All the indices of one tick at once: one graph is published per snapshot, with
        /// metrics that all describe the same tick. Fed metric by metric (GetNodeMetricInput
        /// and the like), a graph is published on every message with whatever has arrived.
        /// </summary>
        public Receiver<IndexSnapshot> SnapshotIn { get; }

        /// <summary>Feeds one metric of every node from a per participant stream.</summary>
        public Receiver<Dictionary<uint, double>> GetNodeMetricInput(string metricName)
        {
            string key = metricName;
            return this.pipeline.CreateReceiver<Dictionary<uint, double>>(
                this,
                (values, envelope) =>
                {
                    foreach (var entry in values)
                    {
                        if (this.nodeMetrics.TryGetValue(entry.Key, out var metrics))
                        {
                            metrics[key] = entry.Value;
                        }
                    }

                    this.TryCompute(envelope.OriginatingTime);
                },
                $"{this.name}-Node-{key}");
        }

        /// <summary>Feeds one metric of every edge from a per pair stream.</summary>
        public Receiver<Dictionary<ParticipantPair, double>> GetEdgeMetricInput(string metricName)
        {
            string key = metricName;
            return this.pipeline.CreateReceiver<Dictionary<ParticipantPair, double>>(
                this,
                (values, envelope) =>
                {
                    foreach (var entry in values)
                    {
                        if (this.edgeMetrics.TryGetValue(entry.Key, out var metrics))
                        {
                            metrics[key] = entry.Value;
                        }
                    }

                    this.TryCompute(envelope.OriginatingTime);
                },
                $"{this.name}-Edge-{key}");
        }

        /// <summary>Feeds one directed metric of every edge from a directed pair stream.</summary>
        public Receiver<Dictionary<DirectedParticipantPair, double>> GetDirectedEdgeMetricInput(string metricName)
        {
            string key = metricName;
            return this.pipeline.CreateReceiver<Dictionary<DirectedParticipantPair, double>>(
                this,
                (values, envelope) =>
                {
                    foreach (var entry in values)
                    {
                        ParticipantPair pair = entry.Key.AsUndirected();
                        bool isForward = entry.Key.From == pair.A;
                        var target = isForward ? this.forwardMetrics : this.backwardMetrics;
                        if (target.TryGetValue(pair, out var metrics))
                        {
                            metrics[key] = entry.Value;
                        }
                    }

                    this.TryCompute(envelope.OriginatingTime);
                },
                $"{this.name}-DirectedEdge-{key}");
        }

        /// <summary>Feeds one group level metric.</summary>
        public Receiver<double> GetGroupMetricInput(string metricName)
        {
            string key = metricName;
            return this.pipeline.CreateReceiver<double>(
                this,
                (value, envelope) =>
                {
                    this.groupMetrics[key] = value;
                    this.TryCompute(envelope.OriginatingTime);
                },
                $"{this.name}-Group-{key}");
        }

        protected override void Compute(DateTime originatingTime)
        {
            var graph = new InteractionGraph { OriginatingTime = originatingTime };

            foreach (var entry in this.nodeMetrics)
            {
                graph.Nodes.Add(new IndexNode
                {
                    ParticipantId = entry.Key,
                    Metrics = new Dictionary<string, double>(entry.Value),
                });
            }

            foreach (var entry in this.edgeMetrics)
            {
                graph.Edges.Add(new IndexEdge
                {
                    Pair = entry.Key,
                    Metrics = new Dictionary<string, double>(entry.Value),
                    ForwardMetrics = new Dictionary<string, double>(this.forwardMetrics[entry.Key]),
                    BackwardMetrics = new Dictionary<string, double>(this.backwardMetrics[entry.Key]),
                });
            }

            graph.Group = new IndexGroup { Metrics = new Dictionary<string, double>(this.groupMetrics) };

            this.Out.Post(graph, originatingTime);

            if (this.configuration.PublishTextVersion)
            {
                this.TextOut.Post(Describe(graph), originatingTime);
            }
        }

        private static bool Accepts(List<string>? filter, string metricName) => filter == null || filter.Contains(metricName);

        private void ReceiveSnapshot(IndexSnapshot snapshot, Envelope envelope)
        {
            if (snapshot == null)
            {
                return;
            }

            foreach (var metric in snapshot.Individual)
            {
                if (!Accepts(this.configuration.NodeMetrics, metric.Key))
                {
                    continue;
                }

                foreach (var entry in metric.Value)
                {
                    if (this.nodeMetrics.TryGetValue(entry.Key, out var metrics))
                    {
                        metrics[metric.Key] = entry.Value;
                    }
                }
            }

            foreach (var metric in snapshot.Pair)
            {
                if (!Accepts(this.configuration.EdgeMetrics, metric.Key))
                {
                    continue;
                }

                foreach (var entry in metric.Value)
                {
                    if (this.edgeMetrics.TryGetValue(entry.Key, out var metrics))
                    {
                        metrics[metric.Key] = entry.Value;
                    }
                }
            }

            foreach (var metric in snapshot.DirectedPair)
            {
                if (!Accepts(this.configuration.DirectedEdgeMetrics, metric.Key))
                {
                    continue;
                }

                foreach (var entry in metric.Value)
                {
                    ParticipantPair pair = entry.Key.AsUndirected();
                    var target = entry.Key.From == pair.A ? this.forwardMetrics : this.backwardMetrics;
                    if (target.TryGetValue(pair, out var metrics))
                    {
                        metrics[metric.Key] = entry.Value;
                    }
                }
            }

            foreach (var metric in snapshot.Group)
            {
                if (Accepts(this.configuration.GroupMetrics, metric.Key))
                {
                    this.groupMetrics[metric.Key] = metric.Value;
                }
            }

            this.TryCompute(envelope.OriginatingTime);
        }

        private static string Describe(InteractionGraph graph)
        {
            var builder = new System.Text.StringBuilder();
            foreach (IndexNode node in graph.Nodes)
            {
                builder.Append(node.ParticipantId);
                foreach (var metric in node.Metrics)
                {
                    builder.Append($"_{metric.Key}={metric.Value:0.###}");
                }

                builder.AppendLine();
            }

            foreach (IndexEdge edge in graph.Edges)
            {
                builder.Append(edge.Pair);
                foreach (var metric in edge.Metrics)
                {
                    builder.Append($"_{metric.Key}={metric.Value:0.###}");
                }

                foreach (var metric in edge.ForwardMetrics)
                {
                    builder.Append($"_{metric.Key}>={metric.Value:0.###}");
                }

                foreach (var metric in edge.BackwardMetrics)
                {
                    builder.Append($"_{metric.Key}<={metric.Value:0.###}");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}
