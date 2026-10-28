// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.Numerics;
using SAAC.CollaborationIndices;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// Deterministic session of three participants, used as the common input of the tests.
    ///
    /// Everything is a closed form of the time, so the same data can be regenerated anywhere
    /// and the expected value of an index can be derived by hand. No timestamp of one stream
    /// ever equals a timestamp of another one: the result therefore does not depend on the
    /// order in which \psi delivers simultaneous messages.
    /// </summary>
    internal static class SyntheticData
    {
        /// <summary>Beginning of the session. Deliberately far from the current date.</summary>
        public static readonly DateTime Start = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        /// <summary>Participants of the session.</summary>
        public static readonly uint[] Participants = { 0, 1, 2 };

        /// <summary>Body parts tracked for every participant.</summary>
        public static readonly string[] BodyParts = { BodyPartNames.Head, BodyPartNames.LeftHand, BodyPartNames.RightHand };

        /// <summary>Period of the position streams (25 Hz).</summary>
        public static readonly TimeSpan SamplePeriod = TimeSpan.FromMilliseconds(40);

        /// <summary>Duration of the session.</summary>
        public static readonly TimeSpan Duration = TimeSpan.FromSeconds(60);

        /// <summary>Tracking loss of participant 2, in seconds from the start.</summary>
        public static readonly (double From, double To) TrackingGap = (25.0, 28.5);

        private static readonly Dictionary<uint, (double Start, double End)[]> SpeechSeconds = new Dictionary<uint, (double, double)[]>
        {
            { 0, new[] { (2.0, 5.5), (8.2, 9.1), (14.0, 20.3), (31.0, 33.3), (44.4, 49.9) } },
            { 1, new[] { (5.9, 8.0), (10.0, 13.5), (21.0, 24.2), (40.0, 43.0), (46.0, 47.5) } },
            { 2, new[] { (25.0, 30.5), (34.0, 36.6), (52.0, 57.0) } },
        };

        /// <summary>Time of the session, from seconds.</summary>
        /// <param name="seconds">Seconds since the beginning of the session.</param>
        /// <returns>The absolute time.</returns>
        public static DateTime At(double seconds) => Start + TimeSpan.FromTicks((long)Math.Round(seconds * TimeSpan.TicksPerSecond));

        /// <summary>Position of one body part at a given time.</summary>
        /// <param name="participantId">The participant.</param>
        /// <param name="bodyPart">The body part.</param>
        /// <param name="seconds">Seconds since the beginning of the session.</param>
        /// <returns>The position.</returns>
        public static Vector3 PositionAt(uint participantId, string bodyPart, double seconds)
        {
            // Participants 0 and 1 share the same slow envelope, participant 2 follows another one:
            // the synchrony of the pair 0-1 is therefore higher than the two others.
            double envelopePhase = participantId == 2 ? 1.7 : 0.0;
            double envelope = 0.6 + (0.4 * Math.Sin((2 * Math.PI * 0.11 * seconds) + envelopePhase));

            double partOffset = bodyPart == BodyPartNames.Head ? 0.0 : (bodyPart == BodyPartNames.LeftHand ? 0.9 : 2.1);
            double frequency = 0.7 + (0.13 * participantId);
            double amplitude = bodyPart == BodyPartNames.Head ? 0.05 : 0.12;
            double angle = (2 * Math.PI * frequency * seconds) + partOffset;

            double x = participantId + (envelope * amplitude * Math.Sin(angle));
            double y = 1.6 + (envelope * amplitude * 0.5 * Math.Cos(1.3 * angle));
            double z = (0.01 * seconds) + (envelope * amplitude * 0.25 * Math.Sin(0.5 * angle));
            return new Vector3((float)x, (float)y, (float)z);
        }

        /// <summary>Position stream of one body part of one participant.</summary>
        /// <param name="participantId">The participant.</param>
        /// <param name="bodyPart">The body part.</param>
        /// <returns>The timestamped positions.</returns>
        public static IEnumerable<(Vector3, DateTime)> Positions(uint participantId, string bodyPart)
        {
            // Each stream has its own sub-millisecond offset so that no two streams share a timestamp.
            long streamOffsetTicks = ((participantId * 3) + (uint)Array.IndexOf(BodyParts, bodyPart)) * 1000L;
            int count = (int)(Duration.Ticks / SamplePeriod.Ticks);

            for (int i = 0; i < count; i++)
            {
                double seconds = i * SamplePeriod.TotalSeconds;
                if (participantId == 2 && seconds >= TrackingGap.From && seconds < TrackingGap.To)
                {
                    continue;
                }

                yield return (PositionAt(participantId, bodyPart, seconds), At(seconds).AddTicks(streamOffsetTicks));
            }
        }

        /// <summary>Speech intervals of one participant, in seconds from the start.</summary>
        /// <param name="participantId">The participant.</param>
        /// <returns>The intervals.</returns>
        public static IReadOnlyList<(double Start, double End)> SpeechOf(uint participantId) => SpeechSeconds[participantId];

        /// <summary>
        /// Speech stream of one participant. As a speech recogniser does, each interval is
        /// posted once it is over.
        /// </summary>
        /// <param name="participantId">The participant.</param>
        /// <returns>The timestamped intervals.</returns>
        public static IEnumerable<(InteractionInterval, DateTime)> Speech(uint participantId)
        {
            foreach ((double start, double end) in SpeechSeconds[participantId])
            {
                var interval = new InteractionInterval(At(start), At(end), IndexCategories.Speaking, participantId);
                yield return (interval, At(end).AddTicks(130000 + (participantId * 1000L)));
            }
        }

        /// <summary>Clock of the indices.</summary>
        /// <param name="period">Period of the clock.</param>
        /// <param name="offset">Offset from the beginning of the session, which keeps the ticks away from the data timestamps.</param>
        /// <returns>The timestamped ticks.</returns>
        public static IEnumerable<(bool, DateTime)> Ticks(TimeSpan period, TimeSpan offset)
        {
            for (DateTime time = Start + offset; time < Start + Duration; time += period)
            {
                yield return (true, time);
            }
        }

        /// <summary>A few events at the given times.</summary>
        /// <param name="seconds">Seconds since the beginning of the session.</param>
        /// <returns>The timestamped events.</returns>
        public static IEnumerable<(bool, DateTime)> Events(params double[] seconds)
        {
            foreach (double second in seconds)
            {
                yield return (true, At(second).AddTicks(77000));
            }
        }

        /// <summary>
        /// Time spent speaking by a participant inside a window, computed directly from the
        /// definition. An interval only counts once it has been published, which
        /// <see cref="Speech"/> does about 13 ms after its end.
        /// </summary>
        /// <param name="participantId">The participant.</param>
        /// <param name="windowStart">Beginning of the window, in seconds.</param>
        /// <param name="windowEnd">End of the window, in seconds.</param>
        /// <returns>The speaking time in seconds.</returns>
        public static double ExpectedSpeakingSeconds(uint participantId, double windowStart, double windowEnd)
        {
            double publicationDelay = (130000 + (participantId * 1000L)) / (double)TimeSpan.TicksPerSecond;

            double total = 0;
            foreach ((double start, double end) in SpeechSeconds[participantId])
            {
                if (end + publicationDelay > windowEnd)
                {
                    continue;
                }

                double from = Math.Max(start, windowStart);
                double to = Math.Min(end, windowEnd);
                if (to > from)
                {
                    total += to - from;
                }
            }

            return total;
        }
    }
}
