// <copyright file="SimultaneousSpeechComponent.cs" company="SAAC">
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
    public class SimultaneousSpeechConfiguration : IndexComponentConfiguration
    {
        /// <summary>Category of the speaking intervals.</summary>
        public string SpeakingCategory { get; set; } = IndexCategories.Speaking;

        /// <summary>
        /// Fewest participants speaking at the same time for the time to count:
        /// 0 with a maximum of 0 measures the silence, 2 the cross-talk.
        /// </summary>
        public int MinimumSpeakers { get; set; } = 2;

        /// <summary>Most participants speaking at the same time for the time to count.</summary>
        public int MaximumSpeakers { get; set; } = int.MaxValue;

        /// <summary>
        /// If false (default), the cumulated time is published in seconds.
        /// If true, it is published as a share of the window (0 to 1).
        /// </summary>
        public bool AsRatioOfWindow { get; set; } = false;
    }

    /// <summary>
    /// Cumulated time, over the sliding window, during which a given number of participants
    /// speak at the same time:
    ///  - nobody (MinimumSpeakers = MaximumSpeakers = 0): the silence of the group;
    ///  - at least two (MinimumSpeakers = 2): the cross-talk.
    ///
    /// The time is measured on the speech intervals themselves, cut on the window edges: an
    /// instant counts once, however many participants speak, and two intervals of the same
    /// participant that overlap count as one speech. With the same intervals as the verbal
    /// participation, silence + time during which somebody speaks = window.
    ///
    /// As for every interval based index, a speech only exists once its interval has been
    /// received, at the end of the utterance: the time of an utterance in progress is silence
    /// until then.
    ///
    /// Inputs: GetIntervalInput(participantId), IntervalIn or IntervalsIn.
    /// Outputs:
    ///  - Out: the time for the group;
    ///  - PairOut: the same measured on the two participants of each pair (both silent,
    ///    or both speaking).
    /// </summary>
    public class SimultaneousSpeechComponent : MultiParticipantIntervalComponent<SimultaneousSpeechConfiguration>, IProducer<double>
    {
        public SimultaneousSpeechComponent(Pipeline pipeline, SimultaneousSpeechConfiguration configuration, string name = nameof(SimultaneousSpeechComponent))
            : base(pipeline, configuration, name)
        {
            this.Out = pipeline.CreateEmitter<double>(this, $"{name}-Group");
            this.PairOut = pipeline.CreateEmitter<Dictionary<ParticipantPair, double>>(this, $"{name}-Pair");
            this.PairEmitters = new KeyedEmitters<ParticipantPair>(pipeline, this, configuration.Pairs(), $"{name}-Pair");
        }

        /// <summary>Cumulated time for the group, in seconds or as a share of the window.</summary>
        public Emitter<double> Out { get; }

        /// <summary>Cumulated time for each pair.</summary>
        public Emitter<Dictionary<ParticipantPair, double>> PairOut { get; }

        public KeyedEmitters<ParticipantPair> PairEmitters { get; }

        /// <summary>
        /// Time, in seconds, during which the number of speakers is between
        /// <paramref name="minimumSpeakers"/> and <paramref name="maximumSpeakers"/>.
        /// </summary>
        /// <param name="speech">Speech of each participant, as intervals inside the window that do not overlap.</param>
        /// <param name="windowStart">Start of the window.</param>
        /// <param name="windowEnd">End of the window.</param>
        /// <param name="minimumSpeakers">Fewest speakers for the time to count.</param>
        /// <param name="maximumSpeakers">Most speakers for the time to count.</param>
        /// <returns>The cumulated time.</returns>
        public static double Measure(IEnumerable<List<KeyValuePair<DateTime, DateTime>>> speech, DateTime windowStart, DateTime windowEnd, int minimumSpeakers, int maximumSpeakers)
        {
            // Each start adds a speaker, each end removes one.
            var changes = new List<KeyValuePair<DateTime, int>>();
            foreach (var intervals in speech)
            {
                foreach (var interval in intervals)
                {
                    changes.Add(new KeyValuePair<DateTime, int>(interval.Key, 1));
                    changes.Add(new KeyValuePair<DateTime, int>(interval.Value, -1));
                }
            }

            changes.Sort((a, b) => a.Key.CompareTo(b.Key));

            double total = 0;
            int speakers = 0;
            DateTime previous = windowStart;
            foreach (var change in changes)
            {
                if (speakers >= minimumSpeakers && speakers <= maximumSpeakers)
                {
                    total += (change.Key - previous).TotalSeconds;
                }

                speakers += change.Value;
                previous = change.Key;
            }

            if (speakers >= minimumSpeakers && speakers <= maximumSpeakers)
            {
                total += (windowEnd - previous).TotalSeconds;
            }

            return total;
        }

        protected override void Compute(DateTime originatingTime)
        {
            DateTime start = this.WindowStart(originatingTime);
            var speech = new Dictionary<uint, List<KeyValuePair<DateTime, DateTime>>>();
            foreach (uint participantId in this.configuration.ParticipantIds)
            {
                speech[participantId] = this.SpeechWithin(participantId, start, originatingTime);
            }

            int minimum = this.configuration.MinimumSpeakers;
            int maximum = this.configuration.MaximumSpeakers;
            double scale = this.configuration.AsRatioOfWindow && this.WindowSeconds > double.Epsilon ? 1.0 / this.WindowSeconds : 1.0;

            double group = Measure(speech.Values, start, originatingTime, minimum, maximum) * scale;
            this.Out.Post(this.Normalize(group), originatingTime);

            // A pair has two speakers at most: "at least two" means both of them.
            var pairs = new Dictionary<ParticipantPair, double>();
            foreach (ParticipantPair pair in this.configuration.Pairs())
            {
                double value = Measure(new[] { speech[pair.A], speech[pair.B] }, start, originatingTime, Math.Min(minimum, 2), Math.Min(maximum, 2)) * scale;
                pairs[pair] = this.Normalize(value);
            }

            this.PairOut.Post(pairs, originatingTime);
            this.PairEmitters.PostAll(pairs, originatingTime);
        }

        // The speech of one participant inside the window: cut on its edges, sorted, and merged
        // where two intervals overlap, so that the participant counts as one speaker.
        private List<KeyValuePair<DateTime, DateTime>> SpeechWithin(uint participantId, DateTime windowStart, DateTime windowEnd)
        {
            var cut = new List<KeyValuePair<DateTime, DateTime>>();
            foreach (InteractionInterval interval in this.intervals.Get(participantId, this.configuration.SpeakingCategory))
            {
                DateTime start = interval.StartTime > windowStart ? interval.StartTime : windowStart;
                DateTime end = interval.EndTime < windowEnd ? interval.EndTime : windowEnd;
                if (end > start)
                {
                    cut.Add(new KeyValuePair<DateTime, DateTime>(start, end));
                }
            }

            var merged = new List<KeyValuePair<DateTime, DateTime>>();
            foreach (var interval in cut.OrderBy(i => i.Key))
            {
                if (merged.Count > 0 && interval.Key <= merged[merged.Count - 1].Value)
                {
                    var last = merged[merged.Count - 1];
                    if (interval.Value > last.Value)
                    {
                        merged[merged.Count - 1] = new KeyValuePair<DateTime, DateTime>(last.Key, interval.Value);
                    }
                }
                else
                {
                    merged.Add(interval);
                }
            }

            return merged;
        }
    }
}
