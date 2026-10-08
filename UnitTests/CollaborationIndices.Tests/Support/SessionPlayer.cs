// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Psi;
using SAAC.CollaborationIndices;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// Replays the synthetic session into a pipeline, the way a store would.
    ///
    /// Every message of the session comes out of a single generator, in time order, and is
    /// then routed to its own stream. Independent generators would be simpler, but \psi starts
    /// them one after the other and, when the replay is not paced by the clock, the first one
    /// runs ahead of the others: the first values of a run then depend on the start-up order.
    /// A single source in a single threaded pipeline makes a run exactly reproducible.
    /// </summary>
    internal sealed class SessionPlayer
    {
        private const string TickStream = "Tick";
        private const string AttentionTickStream = "AttentionTick";
        private const string PhaseStartStream = "PhaseStart";
        private const string PhaseEndStream = "PhaseEnd";

        private readonly Dictionary<string, IProducer<Vector3>> positions = new Dictionary<string, IProducer<Vector3>>();
        private readonly Dictionary<uint, IProducer<InteractionInterval>> speech = new Dictionary<uint, IProducer<InteractionInterval>>();

        /// <summary>Initializes a new instance of the <see cref="SessionPlayer"/> class.</summary>
        /// <param name="pipeline">Pipeline receiving the session.</param>
        /// <param name="tickPeriod">Period of the clock of the indices.</param>
        /// <param name="phaseStarts">Beginning of the phases, in seconds. Null for a session without phases.</param>
        /// <param name="phaseEnds">End of the phases, in seconds.</param>
        /// <param name="shift">Moves the whole session in time, to check that nothing depends on the date.</param>
        public SessionPlayer(Pipeline pipeline, TimeSpan tickPeriod, double[] phaseStarts = null, double[] phaseEnds = null, TimeSpan shift = default)
            : this(Generators.Sequence(pipeline, BuildMessages(tickPeriod, phaseStarts, phaseEnds, shift)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionPlayer"/> class from a stream
        /// that already carries the session, for instance one read back from a store.
        /// </summary>
        /// <param name="source">The stream of session messages.</param>
        public SessionPlayer(IProducer<SessionMessage> source)
        {
            this.Source = source;

            foreach (uint participantId in SyntheticData.Participants)
            {
                foreach (string bodyPart in SyntheticData.BodyParts)
                {
                    string stream = PositionStream(participantId, bodyPart);
                    this.positions[stream] = source.Where(message => message.Stream == stream).Select(message => message.Position);
                }

                string speechStream = SpeechStream(participantId);
                this.speech[participantId] = source.Where(message => message.Stream == speechStream).Select(message => message.Interval.Clone());
            }

            this.Ticks = this.Signal(TickStream);
            this.AttentionTicks = this.Signal(AttentionTickStream);
            this.PhaseStarts = this.Signal(PhaseStartStream);
            this.PhaseEnds = this.Signal(PhaseEndStream);
        }

        /// <summary>Gets the single stream carrying the whole session.</summary>
        public IProducer<SessionMessage> Source { get; }

        /// <summary>Gets the clock of the indices.</summary>
        public IProducer<bool> Ticks { get; }

        /// <summary>Gets the fast clock of the attention accumulator.</summary>
        public IProducer<bool> AttentionTicks { get; }

        /// <summary>Gets the beginning of the phases.</summary>
        public IProducer<bool> PhaseStarts { get; }

        /// <summary>Gets the end of the phases.</summary>
        public IProducer<bool> PhaseEnds { get; }

        /// <summary>Messages of the session, in time order.</summary>
        /// <param name="tickPeriod">Period of the clock of the indices.</param>
        /// <param name="phaseStarts">Beginning of the phases, in seconds.</param>
        /// <param name="phaseEnds">End of the phases, in seconds.</param>
        /// <param name="shift">Moves the whole session in time.</param>
        /// <returns>The timestamped messages.</returns>
        public static List<(SessionMessage, DateTime)> BuildMessages(TimeSpan tickPeriod, double[] phaseStarts = null, double[] phaseEnds = null, TimeSpan shift = default)
        {
            var messages = new List<(SessionMessage, DateTime)>();

            foreach (uint participantId in SyntheticData.Participants)
            {
                foreach (string bodyPart in SyntheticData.BodyParts)
                {
                    string stream = PositionStream(participantId, bodyPart);
                    messages.AddRange(SyntheticData.Positions(participantId, bodyPart)
                        .Select(sample => (new SessionMessage { Stream = stream, Position = sample.Item1 }, sample.Item2 + shift)));
                }

                string speechStream = SpeechStream(participantId);
                foreach ((InteractionInterval interval, DateTime time) in SyntheticData.Speech(participantId))
                {
                    interval.StartTime += shift;
                    interval.EndTime += shift;
                    messages.Add((new SessionMessage { Stream = speechStream, Interval = interval }, time + shift));
                }
            }

            messages.AddRange(Signals(TickStream, SyntheticData.Ticks(tickPeriod, TimeSpan.FromMilliseconds(20)), shift));
            messages.AddRange(Signals(AttentionTickStream, SyntheticData.Ticks(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(5)), shift));
            messages.AddRange(Signals(PhaseStartStream, SyntheticData.Events(phaseStarts ?? new double[0]), shift));
            messages.AddRange(Signals(PhaseEndStream, SyntheticData.Events(phaseEnds ?? new double[0]), shift));

            messages.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            return messages;
        }

        /// <summary>Position stream of one body part of one participant.</summary>
        /// <param name="participantId">The participant.</param>
        /// <param name="bodyPart">The body part.</param>
        /// <returns>The stream.</returns>
        public IProducer<Vector3> Positions(uint participantId, string bodyPart) => this.positions[PositionStream(participantId, bodyPart)];

        /// <summary>Speech stream of one participant.</summary>
        /// <param name="participantId">The participant.</param>
        /// <returns>The stream.</returns>
        public IProducer<InteractionInterval> Speech(uint participantId) => this.speech[participantId];

        private static string PositionStream(uint participantId, string bodyPart) => $"Position-{participantId}-{bodyPart}";

        private static string SpeechStream(uint participantId) => $"Speech-{participantId}";

        private static IEnumerable<(SessionMessage, DateTime)> Signals(string stream, IEnumerable<(bool, DateTime)> times, TimeSpan shift)
            => times.Select(sample => (new SessionMessage { Stream = stream }, sample.Item2 + shift));

        private IProducer<bool> Signal(string stream) => this.Source.Where(message => message.Stream == stream).Select(_ => true);

        /// <summary>One message of the session, whatever its stream.</summary>
        internal sealed class SessionMessage
        {
            /// <summary>Gets or sets the stream the message belongs to.</summary>
            public string Stream { get; set; }

            /// <summary>Gets or sets the position, for a position stream.</summary>
            public Vector3 Position { get; set; }

            /// <summary>Gets or sets the interval, for a speech stream.</summary>
            public InteractionInterval Interval { get; set; }
        }
    }
}
