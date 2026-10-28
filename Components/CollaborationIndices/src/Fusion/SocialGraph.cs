// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.CollaborationIndices
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Numerics;
    using Microsoft.Psi;
    using SAAC.Groups;

    /// <summary>
    /// How the indices of one tick become the graph of the sociogram.
    /// </summary>
    public class SocialGraphOptions
    {
        /// <summary>Gets or sets the length of the sliding window, written in the graph.</summary>
        public TimeSpan Window { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets a value indicating whether the Proximity index is a score in [0, 1],
        /// as the indicator publishes it by default, and not a distance
        /// (<see cref="ProximityOptions.AsProximityScore"/>).
        /// </summary>
        public bool ProximityIsScore { get; set; } = true;

        /// <summary>
        /// Gets or sets the distance at which the proximity score is zero
        /// (<see cref="ProximityOptions.MaximumDistance"/>), in metres.
        /// </summary>
        public double MaximumDistance { get; set; } = 3.0;

        /// <summary>
        /// Gets or sets the names of the group values that are scores of the collaboration
        /// model, in the order the sociogram lists them. Null for the default model.
        /// </summary>
        public List<string>? ScoreNames { get; set; } = null;

        /// <summary>
        /// Gets or sets how long the last place given for a participant stands for where it
        /// is: a participant that has not been seen for longer has no place at the instant.
        /// </summary>
        public TimeSpan PlaceLifetime { get; set; } = TimeSpan.FromSeconds(2);
    }

    /// <summary>
    /// The graphs of a session, one snapshot after the other. Beside what
    /// <see cref="SocialGraph.From"/> puts in a graph, it holds what a single snapshot cannot give.
    ///
    /// The mean of the distance of each pair over the window
    /// (<see cref="GraphMetricNames.MeanDistance"/>). The proximity of the indices is the
    /// distance at the tick, while every other index is a statistic of the window: drawn from
    /// it, the sociogram would show where the participants are now and what they did during
    /// the last window. The mean distance puts the places on the same span of time as the
    /// rest. It is computed here, from the snapshots, and changes nothing to the indices: the
    /// mean is the one of the ticks of the window that were given, so it only covers a whole
    /// window one window after the first snapshot. Beyond the maximum distance of the
    /// proximity score the distance is not known and counts as that maximum.
    ///
    /// The place of each participant in the room, seen from above, when it is given with
    /// <see cref="Place"/>: at the tick (<see cref="GraphMetricNames.PositionX"/>,
    /// <see cref="GraphMetricNames.PositionY"/>) and on average over the window
    /// (<see cref="GraphMetricNames.MeanPositionX"/>, <see cref="GraphMetricNames.MeanPositionY"/>).
    /// A pair whose proximity is not computed then gets its distance from the places.
    /// </summary>
    public class SocialGraphAssembler
    {
        private readonly object sync = new object();
        private readonly List<uint> participants;
        private readonly SocialGraphOptions options;
        private readonly Dictionary<string, Queue<(DateTime Time, double Distance)>> distances = new Dictionary<string, Queue<(DateTime Time, double Distance)>>();
        private readonly Dictionary<uint, List<(DateTime Time, double X, double Y)>> places = new Dictionary<uint, List<(DateTime Time, double X, double Y)>>();
        private DateTime lastTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SocialGraphAssembler"/> class.
        /// </summary>
        /// <param name="participantIds">The participants.</param>
        /// <param name="options">The window and how the proximity is read; null for the defaults.</param>
        public SocialGraphAssembler(IEnumerable<uint> participantIds, SocialGraphOptions? options = null)
        {
            this.participants = participantIds.ToList();
            this.options = options ?? new SocialGraphOptions();
        }

        /// <summary>
        /// Gives the place of a participant in the room at a time, seen from above. The places
        /// of a participant are given in the order of their times, at the rate of its stream;
        /// they may be given from another thread than the snapshots.
        /// </summary>
        /// <param name="participantId">The participant.</param>
        /// <param name="time">The time of the place.</param>
        /// <param name="x">First horizontal coordinate, in metres: drawn from left to right.</param>
        /// <param name="y">Second horizontal coordinate, in metres: drawn from the bottom to the top.</param>
        public void Place(uint participantId, DateTime time, double x, double y)
        {
            if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y))
            {
                return;
            }

            lock (this.sync)
            {
                if (!this.places.TryGetValue(participantId, out var samples))
                {
                    this.places[participantId] = samples = new List<(DateTime Time, double X, double Y)>();
                }

                // A session that starts again, or a replay moved backwards.
                if (samples.Count > 0 && time < samples[samples.Count - 1].Time)
                {
                    samples.Clear();
                }

                samples.Add((time, x, y));
            }
        }

        /// <summary>The graph of the next snapshot of the session.</summary>
        /// <param name="snapshot">The indices of one tick, later than those given before.</param>
        /// <returns>The graph, with the places of the participants and the mean distance of each pair over the window.</returns>
        public IndividualPairCharacteristics Next(IndexSnapshot? snapshot)
        {
            IndividualPairCharacteristics graph = SocialGraph.From(snapshot, this.participants, this.options);
            if (snapshot == null)
            {
                return graph;
            }

            lock (this.sync)
            {
                // A session that starts again, or a replay moved backwards: nothing is kept of what follows.
                DateTime now = snapshot.OriginatingTime;
                if (now < this.lastTime)
                {
                    this.distances.Clear();
                }

                this.lastTime = now;
                TimeSpan window = this.options.Window > TimeSpan.Zero ? this.options.Window : TimeSpan.Zero;

                // ---- The places: where each participant is at the tick, and on average over the window.
                TimeSpan kept = window > this.options.PlaceLifetime ? window : this.options.PlaceLifetime;
                foreach (uint id in this.participants)
                {
                    if (!this.places.TryGetValue(id, out var samples))
                    {
                        continue;
                    }

                    samples.RemoveAll(sample => sample.Time <= now - kept);
                    int last = samples.FindLastIndex(sample => sample.Time <= now);
                    PersonNode? node = graph.FindNode(SocialGraph.NodeId(id));
                    if (last < 0 || node == null)
                    {
                        continue;
                    }

                    // A participant that has not been seen for a while has no place at the instant.
                    if (now - samples[last].Time <= this.options.PlaceLifetime)
                    {
                        node.Set(GraphMetricNames.PositionX, samples[last].X);
                        node.Set(GraphMetricNames.PositionY, samples[last].Y);
                    }

                    double sumX = 0.0;
                    double sumY = 0.0;
                    int count = 0;
                    for (int i = last; i >= 0 && (window > TimeSpan.Zero ? samples[i].Time > now - window : i == last && node.Has(GraphMetricNames.PositionX)); i--)
                    {
                        sumX += samples[i].X;
                        sumY += samples[i].Y;
                        count++;
                    }

                    if (count > 0)
                    {
                        node.Set(GraphMetricNames.MeanPositionX, sumX / count);
                        node.Set(GraphMetricNames.MeanPositionY, sumY / count);
                    }
                }

                // ---- The distance of a pair whose proximity is not computed: the one of the places.
                foreach (PersonEdge edge in graph.PersonEdges)
                {
                    if (!edge.Has(GraphMetricNames.Distance)
                        && graph.FindNode(edge.First) is PersonNode first && first.TryGetPlace(false, out double ax, out double ay)
                        && graph.FindNode(edge.Second) is PersonNode second && second.TryGetPlace(false, out double bx, out double by))
                    {
                        edge.Set(GraphMetricNames.Distance, Math.Sqrt(((ax - bx) * (ax - bx)) + ((ay - by) * (ay - by))));
                    }
                }

                // ---- The mean of the distance of each pair over the window.
                foreach (PersonEdge edge in graph.PersonEdges)
                {
                    string pair = GraphMetrics.PairKey(edge.First, edge.Second);
                    if (!this.distances.TryGetValue(pair, out var ofTheWindow))
                    {
                        this.distances[pair] = ofTheWindow = new Queue<(DateTime Time, double Distance)>();
                    }

                    if (edge.Has(GraphMetricNames.Distance))
                    {
                        ofTheWindow.Enqueue((now, edge.Get(GraphMetricNames.Distance)));
                    }

                    // The window is the one of the indices: what is older than its length is left out.
                    while (ofTheWindow.Count > 0 && (window <= TimeSpan.Zero ? ofTheWindow.Peek().Time < now : ofTheWindow.Peek().Time <= now - window))
                    {
                        ofTheWindow.Dequeue();
                    }

                    if (ofTheWindow.Count > 0)
                    {
                        edge.Set(GraphMetricNames.MeanDistance, ofTheWindow.Average(sample => sample.Distance));
                    }
                }
            }

            return graph;
        }
    }

    /// <summary>
    /// The sociogram of the window (<see cref="IndividualPairCharacteristics"/>, to be drawn
    /// in PsiStudio), from the collaboration indices.
    ///
    /// The graph takes whatever the indices hold, for any number of participants: every index
    /// of a snapshot is copied under its name, at its level (participant, pair, ordered pair,
    /// group), and an index that is not declared is simply not in the graph. Nothing is
    /// required: a snapshot without any index gives the nodes and the edges alone.
    ///
    /// A participant is named by its number, the identifier plus one: it is how TalkingMost
    /// and TaskingMost name a participant, and how the streams of a session are numbered.
    /// </summary>
    public static class SocialGraph
    {
        /// <summary>The identifier of the node of a participant: its number.</summary>
        /// <param name="participantId">Identifier of the participant in the indices.</param>
        /// <returns>"1" for the participant 0.</returns>
        public static string NodeId(uint participantId) => (participantId + 1).ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// The graph of one snapshot of indices, on its own: what it holds is what the
        /// snapshot holds, and the distance of a pair is the one of the tick.
        /// </summary>
        /// <param name="snapshot">The indices of one tick; null for a graph without metrics.</param>
        /// <param name="participantIds">The participants: each one has a node, each pair an edge.</param>
        /// <param name="options">The window and how the proximity is read; null for the defaults.</param>
        /// <returns>The graph.</returns>
        public static IndividualPairCharacteristics From(IndexSnapshot? snapshot, IEnumerable<uint> participantIds, SocialGraphOptions? options = null)
        {
            options ??= new SocialGraphOptions();
            List<uint> participants = participantIds.Distinct().OrderBy(id => id).ToList();
            IndividualPairCharacteristics graph = IndividualPairCharacteristics.ForParticipants(participants.Select(NodeId));
            graph.WindowSeconds = options.Window.TotalSeconds;
            if (options.ScoreNames != null)
            {
                graph.PersonGroup.ScoreNames = new List<string>(options.ScoreNames);
            }

            if (snapshot == null)
            {
                return graph;
            }

            foreach (var metric in snapshot.Individual)
            {
                foreach (var value in metric.Value)
                {
                    graph.FindNode(NodeId(value.Key))?.Set(metric.Key, value.Value);
                }
            }

            foreach (var metric in snapshot.Pair)
            {
                foreach (var value in metric.Value)
                {
                    graph.FindEdge(NodeId(value.Key.A), NodeId(value.Key.B))?.Set(metric.Key, value.Value);
                }
            }

            foreach (var metric in snapshot.DirectedPair)
            {
                foreach (var value in metric.Value)
                {
                    string from = NodeId(value.Key.From);
                    PersonEdge? edge = graph.FindEdge(from, NodeId(value.Key.To));
                    if (edge != null)
                    {
                        (edge.First == from ? edge.ForwardMetrics : edge.BackwardMetrics)[metric.Key] = value.Value;
                    }
                }
            }

            foreach (var metric in snapshot.Group)
            {
                graph.PersonGroup.Set(metric.Key, metric.Value);
            }

            // The sociogram places its nodes from distances in metres. The proximity index is
            // a score by default: 1 when the participants are together, 0 from the maximum
            // distance on, beyond which the distance is no longer known.
            foreach (PersonEdge edge in graph.PersonEdges)
            {
                if (edge.Has(IndexNames.Proximity) && !edge.Has(GraphMetricNames.Distance))
                {
                    double proximity = edge.Get(IndexNames.Proximity);
                    double distance = options.ProximityIsScore
                        ? Math.Min(Math.Max(1.0 - proximity, 0.0), 1.0) * options.MaximumDistance
                        : Math.Max(proximity, 0.0);
                    edge.Set(GraphMetricNames.Distance, distance);
                }
            }

            return graph;
        }

        /// <summary>
        /// The stream of graphs of a stream of snapshots, each pair with the mean of its
        /// distance over the window (<see cref="SocialGraphAssembler"/>).
        /// </summary>
        /// <param name="snapshots">The snapshots, for instance <see cref="SlidingAverageComputation.SnapshotOut"/>.</param>
        /// <param name="participantIds">The participants.</param>
        /// <param name="options">The window and how the proximity is read; null for the defaults.</param>
        /// <returns>One graph per snapshot, at the time of its tick.</returns>
        public static IProducer<IndividualPairCharacteristics> ToSocialGraph(this IProducer<IndexSnapshot> snapshots, IEnumerable<uint> participantIds, SocialGraphOptions? options = null)
        {
            var assembler = new SocialGraphAssembler(participantIds, options);
            return snapshots.Select(snapshot => assembler.Next(snapshot));
        }

        /// <summary>
        /// The stream of graphs of a set of indices: its participants, its window, the
        /// settings of its proximity indicator and the scores of its collaboration model.
        /// </summary>
        /// <param name="indices">The indices of one window, as the builder returns them.</param>
        /// <returns>One graph per tick.</returns>
        public static IProducer<IndividualPairCharacteristics> ToSocialGraph(this SlidingAverageComputation indices)
            => indices.SnapshotOut.ToSocialGraph(indices.ParticipantIds, OptionsOf(indices));

        /// <summary>
        /// The stream of graphs of a set of indices, with the place of each participant in
        /// the room: the sociogram can then draw the participants where they are, seen from
        /// above, instead of placing them from the distances of the pairs.
        /// </summary>
        /// <param name="indices">The indices of one window, as the builder returns them.</param>
        /// <param name="positions">The position of each participant in the room, for instance of its head. A participant without a stream has no place.</param>
        /// <param name="upAxis">The vertical axis of the positions: Y for the frame of a Unity scene, Z for the standardised frame.</param>
        /// <returns>One graph per tick.</returns>
        public static IProducer<IndividualPairCharacteristics> ToSocialGraph(this SlidingAverageComputation indices, IReadOnlyDictionary<uint, IProducer<Vector3>> positions, Vector3 upAxis)
        {
            var assembler = new SocialGraphAssembler(indices.ParticipantIds, OptionsOf(indices));
            foreach (var position in positions)
            {
                uint id = position.Key;
                position.Value.Do((value, envelope) =>
                {
                    (double x, double y) = OnFloor(value, upAxis);
                    assembler.Place(id, envelope.OriginatingTime, x, y);
                });
            }

            return indices.SnapshotOut.Select(snapshot => assembler.Next(snapshot));
        }

        /// <summary>
        /// The place on the floor of a position of the room, as seen from above.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="upAxis">The vertical axis of the frame.</param>
        /// <returns>
        /// The two horizontal coordinates: (X, Z) with Y up, the frame of a Unity scene, where
        /// X goes to the right and Z forward; (X, Y) with Z up, the standardised frame, which
        /// is the same floor.
        /// </returns>
        public static (double X, double Y) OnFloor(Vector3 position, Vector3 upAxis)
        {
            float x = Math.Abs(upAxis.X);
            float y = Math.Abs(upAxis.Y);
            float z = Math.Abs(upAxis.Z);
            if (y >= x && y >= z)
            {
                return (position.X, position.Z);
            }

            return z >= x ? (position.X, position.Y) : (position.Y, position.Z);
        }

        private static SocialGraphOptions OptionsOf(SlidingAverageComputation indices)
        {
            ProximityOptions? proximity = indices.Find<ProximityIndicator>()?.Options;
            return new SocialGraphOptions
            {
                Window = indices.WindowDuration,
                ProximityIsScore = proximity?.AsProximityScore ?? true,
                MaximumDistance = proximity?.MaximumDistance ?? 3.0,
                ScoreNames = indices.ScoreNames.Count > 0 ? indices.ScoreNames.ToList() : null,
            };
        }
    }
}
