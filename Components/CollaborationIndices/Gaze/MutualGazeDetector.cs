// <copyright file="MutualGazeDetector.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class MutualGazeDetectorConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>
        /// Shortest time two participants must look at each other for it to count. Zero
        /// (default) counts any time their looks overlap.
        /// </summary>
        public TimeSpan MinimumDuration { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// How long a look is kept, to be compared with the looks of the other participant
        /// that end after it.
        /// </summary>
        public TimeSpan HistoryDuration { get; set; } = TimeSpan.FromSeconds(60);
    }

    /// <summary>
    /// Mutual gazes: two participants look at each other at the same time. They are found in
    /// the looks at peers (GazeEpisodeDetector.PeerGazeIntervalOut): a look of A at B and a
    /// look of B at A that overlap in time are one mutual gaze, which lasts from the start of
    /// the later look to the end of the earlier one.
    ///
    /// A look is only known once it has ended, so a mutual gaze is published when the second
    /// of its two looks ends. One long look met by three short ones gives three mutual gazes.
    ///
    /// Outputs:
    ///  - Out: an event per mutual gaze, the two participants in ParticipantId (the lower
    ///    identifier) and TargetId, stamped with the end of the mutual gaze; to the MutualGaze
    ///    indicator;
    ///  - IntervalOut: the same mutual gazes with their duration.
    /// </summary>
    public class MutualGazeDetector
    {
        private readonly MutualGazeDetectorConfiguration configuration;
        private readonly List<InteractionInterval> looks = new List<InteractionInterval>();
        private readonly MonotonicPoster poster = new MonotonicPoster(NonMonotonicPolicy.Advance);

        public MutualGazeDetector(Pipeline pipeline, MutualGazeDetectorConfiguration configuration, string name = nameof(MutualGazeDetector))
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.PeerGazeIn = pipeline.CreateReceiver<InteractionInterval>(this, this.ReceiveLook, $"{name}-PeerGaze");
            this.Out = pipeline.CreateEmitter<InteractionEvent>(this, $"{name}-MutualGaze");
            this.IntervalOut = pipeline.CreateEmitter<InteractionInterval>(this, $"{name}-MutualGazeInterval");
        }

        /// <summary>Looks at peers, the gazer in ParticipantId and the gazed in TargetId.</summary>
        public Receiver<InteractionInterval> PeerGazeIn { get; }

        /// <summary>Mutual gazes as events: category MutualGaze, the two participants, the end of the mutual gaze.</summary>
        public Emitter<InteractionEvent> Out { get; }

        /// <summary>Mutual gazes as intervals: category MutualGaze, the two participants.</summary>
        public Emitter<InteractionInterval> IntervalOut { get; }

        /// <summary>
        /// The time during which two looks are both running, when they are looks of two
        /// participants at each other.
        /// </summary>
        /// <param name="first">A look at a peer.</param>
        /// <param name="second">Another look at a peer.</param>
        /// <param name="minimumDuration">Shortest time that counts.</param>
        /// <returns>The mutual gaze, or null when there is none.</returns>
        public static InteractionInterval? MutualGaze(InteractionInterval first, InteractionInterval second, TimeSpan minimumDuration)
        {
            if (!first.TargetId.HasValue || !second.TargetId.HasValue
                || first.ParticipantId == second.ParticipantId
                || first.TargetId.Value != second.ParticipantId || second.TargetId.Value != first.ParticipantId)
            {
                return null;
            }

            DateTime start = first.StartTime > second.StartTime ? first.StartTime : second.StartTime;
            DateTime end = first.EndTime < second.EndTime ? first.EndTime : second.EndTime;
            if (end <= start || end - start < minimumDuration)
            {
                return null;
            }

            var pair = new ParticipantPair(first.ParticipantId, second.ParticipantId);
            return new InteractionInterval(start, end, IndexCategories.MutualGaze, pair.A, pair.B);
        }

        private void ReceiveLook(InteractionInterval look, Envelope envelope)
        {
            if (look == null || look.IsOpen || !look.TargetId.HasValue
                || !this.configuration.ParticipantIds.Contains(look.ParticipantId)
                || !this.configuration.ParticipantIds.Contains(look.TargetId.Value))
            {
                return;
            }

            DateTime oldest = look.EndTime - this.configuration.HistoryDuration;
            this.looks.RemoveAll(other => other.EndTime < oldest);

            var mutualGazes = this.looks
                .Select(other => MutualGaze(look, other, this.configuration.MinimumDuration))
                .Where(mutual => mutual != null)
                .Select(mutual => mutual!)
                .OrderBy(mutual => mutual.EndTime)
                .ToList();

            foreach (InteractionInterval mutual in mutualGazes)
            {
                var mutualEvent = new InteractionEvent(mutual.EndTime, IndexCategories.MutualGaze, mutual.ParticipantId, mutual.TargetId);
                this.poster.Post(this.Out, mutualEvent, envelope.OriginatingTime);
                this.poster.Post(this.IntervalOut, mutual, envelope.OriginatingTime);
            }

            // A copy: the message belongs to the pipeline once the handler returns.
            this.looks.Add(look.Clone());
        }
    }
}
