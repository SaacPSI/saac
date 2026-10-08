// <copyright file="DataClock.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Builds a regular tick stream out of the data itself.
    ///
    /// Why not a generator: Timers.Timer is driven by the OS clock, and Generators.Repeat is
    /// anchored on the start time of the pipeline that hosts it. Both are fine in a live
    /// pipeline, but a subpipeline created and started after the parent has begun replaying
    /// does not necessarily share the parent's virtual time, so its generated ticks can land
    /// outside the window the data occupies. Every index then computes over an empty window.
    ///
    /// A data clock has no such problem: its ticks carry the originating times of the messages
    /// that produced them, so they are on the data timeline by construction, at any replay
    /// speed and whatever the pipeline lifecycle.
    ///
    /// The trade-off is that the clock only advances while data flows: a gap in the source
    /// stream is a gap in the ticks. Pick a source that is dense and always present, typically
    /// head positions (about 30 Hz) rather than speech.
    /// </summary>
    public static class DataClock
    {
        /// <summary>
        /// One tick per <paramref name="interval"/> of data time, derived from any stream.
        /// </summary>
        /// <typeparam name="T">Type of the source, whose values are ignored.</typeparam>
        /// <param name="source">Dense stream used as the time reference.</param>
        /// <param name="interval">Period of the ticks, in data time.</param>
        public static IProducer<bool> FromStream<T>(IProducer<T> source, TimeSpan interval)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentException("The tick interval must be strictly positive.", nameof(interval));
            }

            DateTime last = DateTime.MinValue;

            return source.Process<T, bool>((_, envelope, emitter) =>
            {
                if (last == DateTime.MinValue)
                {
                    // First message: start the grid here rather than emitting immediately, so
                    // the first tick is a full interval into the data.
                    last = envelope.OriginatingTime;
                    return;
                }

                if (envelope.OriginatingTime - last < interval)
                {
                    return;
                }

                last = envelope.OriginatingTime;
                emitter.Post(true, envelope.OriginatingTime);
            });
        }

        /// <summary>
        /// Same, as TimeSpan, for the APIs that expect the shape of a \psi Timer.
        /// </summary>
        public static IProducer<TimeSpan> FromStreamAsTimeSpan<T>(IProducer<T> source, TimeSpan interval)
            => FromStream(source, interval).Select(_ => interval);

        /// <summary>
        /// Clock built from the first available source of a list, in order of preference.
        /// Lets a pipeline fall back on audio when no position stream exists, without the
        /// caller having to test each list.
        /// </summary>
        public static IProducer<bool> FromFirstAvailable(TimeSpan interval, params IProducer<object>[] candidates)
        {
            foreach (IProducer<object> candidate in candidates)
            {
                if (candidate != null)
                {
                    return FromStream(candidate, interval);
                }
            }

            throw new ArgumentException("No source stream available to build a data clock.", nameof(candidates));
        }

        /// <summary>
        /// Erases the type of a stream so it can be passed to FromFirstAvailable.
        /// </summary>
        public static IProducer<object> AsUntyped<T>(this IProducer<T> source)
            => source == null ? null : source.Select(value => (object)value);

        /// <summary>
        /// First element of a list, or null when the list is empty. Avoids an index out of
        /// range when a modality is absent from a given session.
        /// </summary>
        public static IProducer<T> FirstOrNull<T>(IReadOnlyList<IProducer<T>> producers)
            where T : class
            => producers != null && producers.Count > 0 ? producers[0] : null;
    }
}
