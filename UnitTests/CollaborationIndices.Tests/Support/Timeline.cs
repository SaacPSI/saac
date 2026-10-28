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
    /// A hand written scenario: a few timestamped messages on named streams, played from a
    /// single source so that a single threaded run is exactly reproducible (see SessionPlayer).
    /// </summary>
    internal sealed class Timeline
    {
        private readonly List<(Entry, DateTime)> entries = new List<(Entry, DateTime)>();
        private IProducer<Entry> source;

        /// <summary>Adds a tick stream.</summary>
        /// <param name="stream">Name of the stream.</param>
        /// <param name="fromSeconds">First tick, in seconds from the start of the session.</param>
        /// <param name="toSeconds">End of the ticks, exclusive.</param>
        /// <param name="periodSeconds">Period of the ticks.</param>
        /// <returns>The timeline.</returns>
        public Timeline Ticks(string stream, double fromSeconds, double toSeconds, double periodSeconds)
        {
            for (int i = 0; fromSeconds + (i * periodSeconds) < toSeconds; i++)
            {
                this.entries.Add((new Entry { Stream = stream }, SyntheticData.At(fromSeconds + (i * periodSeconds))));
            }

            return this;
        }

        /// <summary>Adds a position.</summary>
        /// <param name="stream">Name of the stream.</param>
        /// <param name="seconds">Time, in seconds from the start of the session.</param>
        /// <param name="position">The position.</param>
        /// <returns>The timeline.</returns>
        public Timeline Position(string stream, double seconds, Vector3 position)
        {
            this.entries.Add((new Entry { Stream = stream, Position = position }, SyntheticData.At(seconds)));
            return this;
        }

        /// <summary>Adds an event, posted at the time it happened.</summary>
        /// <param name="stream">Name of the stream.</param>
        /// <param name="seconds">Time, in seconds from the start of the session.</param>
        /// <param name="category">Category of the event.</param>
        /// <param name="participantId">Participant who produced it.</param>
        /// <param name="targetId">Second participant, if any.</param>
        /// <param name="label">Label of the event.</param>
        /// <returns>The timeline.</returns>
        public Timeline Event(string stream, double seconds, string category, uint participantId, uint? targetId = null, string label = "")
        {
            DateTime time = SyntheticData.At(seconds);
            this.entries.Add((new Entry { Stream = stream, Event = new InteractionEvent(time, category, participantId, targetId, 1.0, label) }, time));
            return this;
        }

        /// <summary>Adds an interval, posted once it is over.</summary>
        /// <param name="stream">Name of the stream.</param>
        /// <param name="startSeconds">Beginning, in seconds from the start of the session.</param>
        /// <param name="endSeconds">End, which is also when it is posted.</param>
        /// <param name="category">Category of the interval.</param>
        /// <param name="participantId">Participant it belongs to.</param>
        /// <returns>The timeline.</returns>
        /// <param name="label">Label of the interval.</param>
        public Timeline Interval(string stream, double startSeconds, double endSeconds, string category, uint participantId, string label = "")
        {
            var interval = new InteractionInterval(SyntheticData.At(startSeconds), SyntheticData.At(endSeconds), category, participantId, null, label);
            this.entries.Add((new Entry { Stream = stream, Interval = interval }, SyntheticData.At(endSeconds)));
            return this;
        }

        /// <summary>Adds a number.</summary>
        /// <param name="stream">Name of the stream.</param>
        /// <param name="seconds">Time, in seconds from the start of the session.</param>
        /// <param name="value">The value.</param>
        /// <returns>The timeline.</returns>
        public Timeline Number(string stream, double seconds, double value)
        {
            this.entries.Add((new Entry { Stream = stream, Number = value }, SyntheticData.At(seconds)));
            return this;
        }

        /// <summary>Starts playing the timeline in a pipeline. Call it once, after the last addition.</summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <returns>The timeline.</returns>
        public Timeline Play(Pipeline pipeline)
        {
            // Two messages cannot share a timestamp on one stream: nudge the later ones by a tick each.
            var ordered = new List<(Entry, DateTime)>();
            DateTime last = DateTime.MinValue;
            foreach ((Entry entry, DateTime time) in this.entries.OrderBy(e => e.Item2))
            {
                DateTime stamped = time > last ? time : last.AddTicks(1);
                ordered.Add((entry, stamped));
                last = stamped;
            }

            this.source = Generators.Sequence(pipeline, ordered);
            return this;
        }

        /// <summary>A tick stream.</summary>
        /// <param name="stream">Name of the stream.</param>
        /// <returns>The stream.</returns>
        public IProducer<bool> TicksOf(string stream) => this.Of(stream).Select(_ => true);

        /// <summary>A position stream.</summary>
        /// <param name="stream">Name of the stream.</param>
        /// <returns>The stream.</returns>
        public IProducer<Vector3> PositionsOf(string stream) => this.Of(stream).Select(entry => entry.Position);

        /// <summary>A stream of numbers.</summary>
        /// <param name="stream">Name of the stream.</param>
        /// <returns>The stream.</returns>
        public IProducer<double> NumbersOf(string stream) => this.Of(stream).Select(entry => entry.Number);

        /// <summary>An event stream.</summary>
        /// <param name="stream">Name of the stream.</param>
        /// <returns>The stream.</returns>
        public IProducer<InteractionEvent> EventsOf(string stream) => this.Of(stream).Select(entry => entry.Event.Clone());

        /// <summary>An interval stream.</summary>
        /// <param name="stream">Name of the stream.</param>
        /// <returns>The stream.</returns>
        public IProducer<InteractionInterval> IntervalsOf(string stream) => this.Of(stream).Select(entry => entry.Interval.Clone());

        private IProducer<Entry> Of(string stream)
        {
            if (this.source == null)
            {
                throw new InvalidOperationException("Call Play before reading a stream of the timeline.");
            }

            return this.source.Where(entry => entry.Stream == stream);
        }

        /// <summary>One message of the timeline, whatever its stream.</summary>
        internal sealed class Entry
        {
            /// <summary>Gets or sets the stream the message belongs to.</summary>
            public string Stream { get; set; }

            /// <summary>Gets or sets the position, for a position stream.</summary>
            public Vector3 Position { get; set; }

            /// <summary>Gets or sets the value, for a stream of numbers.</summary>
            public double Number { get; set; }

            /// <summary>Gets or sets the event, for an event stream.</summary>
            public InteractionEvent Event { get; set; }

            /// <summary>Gets or sets the interval, for an interval stream.</summary>
            public InteractionInterval Interval { get; set; }
        }
    }
}
