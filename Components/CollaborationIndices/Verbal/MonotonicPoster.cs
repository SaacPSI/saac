using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>What to do when a message would break the strictly increasing rule.</summary>
    public enum NonMonotonicPolicy
    {
        /// <summary>Drop the message. Safe for state snapshots, where the next one supersedes it.</summary>
        Skip,

        /// <summary>Post it one tick after the previous message. Keeps every message, shifts time by 100 ns.</summary>
        Advance,
    }

    /// <summary>
    /// Guards the posts of an emitter fed by several receivers.
    ///
    /// \psi requires strictly increasing originating times on a given emitter. A per participant
    /// emitter fed by a single stream satisfies this naturally, but a *group level* emitter
    /// written from every participant's receiver does not: the receivers of a component are
    /// delivered in the order the messages arrive, and two sources interleave freely, so
    /// participant 2's message stamped t=10.0 can be delivered after participant 1's stamped
    /// t=10.5. The emitter then throws InvalidOperationException.
    ///
    /// This is the guard the legacy IndividualVad carried inline
    /// (`if (originatingTime != lastPost &amp;&amp; originatingTime > lastPost)`), made reusable.
    /// </summary>
    public class MonotonicPoster
    {
        private readonly Dictionary<object, DateTime> lastPosted = new Dictionary<object, DateTime>();

        public MonotonicPoster(NonMonotonicPolicy policy = NonMonotonicPolicy.Skip)
        {
            this.Policy = policy;
        }

        public NonMonotonicPolicy Policy { get; }

        /// <summary>Number of messages dropped or shifted, useful to detect a badly ordered source.</summary>
        public long CorrectedCount { get; private set; }

        /// <summary>
        /// Posts a message if the time allows it. Returns false when the message was dropped.
        /// </summary>
        public bool Post<T>(Emitter<T> emitter, T value, DateTime originatingTime)
        {
            if (emitter == null)
            {
                return false;
            }

            if (this.lastPosted.TryGetValue(emitter, out DateTime last) && originatingTime <= last)
            {
                this.CorrectedCount++;

                if (this.Policy == NonMonotonicPolicy.Skip)
                {
                    return false;
                }

                originatingTime = last.AddTicks(1);
            }

            this.lastPosted[emitter] = originatingTime;
            emitter.Post(value, originatingTime);
            return true;
        }

        /// <summary>Last time posted on an emitter, or DateTime.MinValue.</summary>
        public DateTime LastPosted<T>(Emitter<T> emitter)
            => this.lastPosted.TryGetValue(emitter, out DateTime last) ? last : DateTime.MinValue;

        public void Reset() => this.lastPosted.Clear();
    }
}
