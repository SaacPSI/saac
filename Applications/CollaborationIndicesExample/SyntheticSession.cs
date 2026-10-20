// <copyright file="SyntheticSession.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

namespace CollaborationIndicesExample
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Microsoft.Psi;
    using SAAC.CollaborationIndices;

    /// <summary>
    /// One frame of the session: the body of every participant, and the utterances that
    /// ended during the frame.
    /// </summary>
    internal sealed class SessionFrame
    {
        /// <summary>Gets or sets the position of each body part of each participant.</summary>
        public Dictionary<uint, Dictionary<string, Vector3>> Bodies { get; set; } = new Dictionary<uint, Dictionary<string, Vector3>>();

        /// <summary>Gets or sets the utterance that just ended, for the participants who have one.</summary>
        public Dictionary<uint, InteractionInterval> Speech { get; set; } = new Dictionary<uint, InteractionInterval>();
    }

    /// <summary>
    /// Stands for the sources of a real session, so that the example runs without any sensor
    /// or store: tracked positions of the head and hands, and speech intervals as a speech
    /// recognizer would give them.
    ///
    /// The messages carry their own timestamps, dated in the past, exactly like the streams
    /// of a store being replayed: the indices are computed on those timestamps, at whatever
    /// speed the pipeline runs.
    /// </summary>
    internal sealed class SyntheticSession
    {
        /// <summary>Beginning of the recorded session.</summary>
        public static readonly DateTime Start = new DateTime(2026, 3, 12, 14, 0, 0, DateTimeKind.Utc);

        private static readonly string[] BodyParts = { BodyPartNames.Head, BodyPartNames.LeftHand, BodyPartNames.RightHand };

        // A scripted conversation: who speaks, for how long. The first speaker of the list
        // dominates the first half, the second half is more balanced.
        private static readonly (int Speaker, double Duration)[] Turns =
        {
            (0, 4.5), (1, 1.2), (0, 6.0), (2, 1.5), (0, 5.2), (1, 2.0), (0, 4.8), (0, 3.5), (2, 1.0), (0, 5.5),
            (1, 3.8), (2, 4.1), (0, 3.6), (1, 4.2), (2, 3.9), (1, 3.4), (0, 4.0), (2, 4.4), (1, 3.7), (2, 3.2),
        };

        private readonly List<uint> participants;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntheticSession"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline receiving the session.</param>
        /// <param name="participants">Participants of the session.</param>
        /// <param name="duration">Duration of the session.</param>
        /// <param name="frameRate">Number of frames per second.</param>
        public SyntheticSession(Pipeline pipeline, IEnumerable<uint> participants, TimeSpan duration, double frameRate = 30)
        {
            this.participants = participants.ToList();
            this.Duration = duration;
            this.Frames = Generators.Sequence(pipeline, this.Generate(frameRate));
        }

        /// <summary>Gets the duration of the session.</summary>
        public TimeSpan Duration { get; }

        /// <summary>Gets the frames of the session: a dense stream, which also makes a good clock.</summary>
        public IProducer<SessionFrame> Frames { get; }

        /// <summary>Positions of the body parts of one participant.</summary>
        /// <param name="participantId">The participant.</param>
        /// <returns>One dictionary per frame, by body part name.</returns>
        public IProducer<Dictionary<string, Vector3>> Body(uint participantId)
            => this.Frames.Select(frame => new Dictionary<string, Vector3>(frame.Bodies[participantId]));

        /// <summary>Utterances of one participant, each one posted when it ends.</summary>
        /// <param name="participantId">The participant.</param>
        /// <returns>The speech intervals.</returns>
        public IProducer<InteractionInterval> Speech(uint participantId)
            => this.Frames.Where(frame => frame.Speech.ContainsKey(participantId)).Select(frame => frame.Speech[participantId].Clone());

        private static Vector3 PositionOf(int index, int count, string bodyPart, double seconds)
        {
            // Participants stand on a circle. The first two sway on the same slow rhythm,
            // the others on their own, so that one pair is more synchronous than the rest.
            double angle = 2 * Math.PI * index / Math.Max(1, count);
            double rhythm = index < 2 ? 0.0 : 1.3 * index;
            double energy = 0.6 + (0.4 * Math.Sin((2 * Math.PI * 0.08 * seconds) + rhythm));

            double amplitude = bodyPart == BodyPartNames.Head ? 0.04 : 0.15;
            double phase = bodyPart == BodyPartNames.Head ? 0.0 : (bodyPart == BodyPartNames.LeftHand ? 1.1 : 2.3);
            double swing = (2 * Math.PI * (0.6 + (0.1 * index)) * seconds) + phase;

            return new Vector3(
                (float)(Math.Cos(angle) + (energy * amplitude * Math.Sin(swing))),
                (float)((bodyPart == BodyPartNames.Head ? 1.65 : 1.05) + (energy * amplitude * 0.5 * Math.Cos(1.4 * swing))),
                (float)(Math.Sin(angle) + (energy * amplitude * 0.4 * Math.Sin(0.7 * swing))));
        }

        private IEnumerable<(SessionFrame, DateTime)> Generate(double frameRate)
        {
            var utterances = new Queue<InteractionInterval>(this.Script());
            int frameCount = (int)(this.Duration.TotalSeconds * frameRate);

            for (int i = 0; i < frameCount; i++)
            {
                double seconds = i / frameRate;
                DateTime time = Start + TimeSpan.FromSeconds(seconds);
                var frame = new SessionFrame();

                for (int p = 0; p < this.participants.Count; p++)
                {
                    frame.Bodies[this.participants[p]] = BodyParts.ToDictionary(part => part, part => PositionOf(p, this.participants.Count, part, seconds));
                }

                // An utterance is known once it is over, as with a speech recognizer.
                while (utterances.Count > 0 && utterances.Peek().EndTime <= time)
                {
                    InteractionInterval utterance = utterances.Dequeue();
                    frame.Speech[utterance.ParticipantId] = utterance;
                }

                yield return (frame, time);
            }
        }

        private IEnumerable<InteractionInterval> Script()
        {
            // Not a whole number of seconds: an utterance must not end exactly on a tick of the
            // clock, where being inside or outside the window would depend on delivery order.
            double cursor = 2.02;
            foreach ((int speaker, double duration) in Turns)
            {
                double end = cursor + duration;
                if (end >= this.Duration.TotalSeconds)
                {
                    yield break;
                }

                uint participantId = this.participants[speaker % this.participants.Count];
                yield return new InteractionInterval(
                    Start + TimeSpan.FromSeconds(cursor),
                    Start + TimeSpan.FromSeconds(end),
                    IndexCategories.Speaking,
                    participantId);

                // A short silence between two turns.
                cursor = end + 0.5;
            }
        }
    }
}
