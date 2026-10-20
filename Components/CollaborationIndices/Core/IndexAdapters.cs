using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Bridges between the streams already produced by the SAAC pipeline and the two generic
    /// types consumed by the indicator components.
    ///
    /// The rule is: one adapter per source type, never a new component. A new modality is
    /// integrated by writing a selector that returns an InteractionEvent or an
    /// InteractionInterval, and every existing indicator can then consume it.
    /// </summary>
    public static class IndexAdapters
    {
        /// <summary>
        /// Converts a stream of (participantId, queue of domain objects) into a stream of events.
        /// This is the shape of the legacy task streams: Tuple&lt;int, Queue&lt;PieceStatus&gt;&gt;.
        /// </summary>
        public static IProducer<IEnumerable<InteractionEvent>> ToEvents<TSource>(
            this IProducer<Tuple<int, Queue<TSource>>> source,
            string category,
            Func<TSource, DateTime> timeSelector,
            Func<TSource, string> labelSelector = null,
            Func<TSource, bool> filter = null)
        {
            return source.Select(tuple =>
            {
                var events = new List<InteractionEvent>();
                if (tuple?.Item2 == null)
                {
                    return (IEnumerable<InteractionEvent>)events;
                }

                foreach (TSource item in tuple.Item2)
                {
                    if (filter != null && !filter(item))
                    {
                        continue;
                    }

                    events.Add(new InteractionEvent(
                        timeSelector(item),
                        category,
                        (uint)tuple.Item1,
                        null,
                        1.0,
                        labelSelector?.Invoke(item) ?? string.Empty));
                }

                return (IEnumerable<InteractionEvent>)events;
            });
        }

        /// <summary>
        /// Converts a queue of dyadic domain objects (JVA, turn taking) into events carrying
        /// both participants.
        /// </summary>
        public static IProducer<IEnumerable<InteractionEvent>> ToDyadicEvents<TSource>(
            this IProducer<Queue<TSource>> source,
            string category,
            Func<TSource, DateTime> timeSelector,
            Func<TSource, uint> initiatorSelector,
            Func<TSource, uint?> responderSelector)
        {
            return source.Select(queue =>
            {
                var events = new List<InteractionEvent>();
                if (queue == null)
                {
                    return (IEnumerable<InteractionEvent>)events;
                }

                foreach (TSource item in queue)
                {
                    events.Add(new InteractionEvent(
                        timeSelector(item),
                        category,
                        initiatorSelector(item),
                        responderSelector?.Invoke(item)));
                }

                return (IEnumerable<InteractionEvent>)events;
            });
        }

        /// <summary>
        /// Converts a stream of (participantId, queue of domain objects) into events carrying a
        /// second participant, for instance a turn taking where the previous speaker is known.
        /// </summary>
        public static IProducer<IEnumerable<InteractionEvent>> ToDirectedEvents<TSource>(
            this IProducer<Dictionary<int, Queue<TSource>>> source,
            string category,
            Func<TSource, DateTime> timeSelector,
            Func<TSource, uint?> targetSelector)
        {
            return source.Select(dictionary =>
            {
                var events = new List<InteractionEvent>();
                if (dictionary == null)
                {
                    return (IEnumerable<InteractionEvent>)events;
                }

                foreach (var entry in dictionary)
                {
                    if (entry.Value == null)
                    {
                        continue;
                    }

                    foreach (TSource item in entry.Value)
                    {
                        events.Add(new InteractionEvent(timeSelector(item), category, (uint)entry.Key, targetSelector?.Invoke(item)));
                    }
                }

                return (IEnumerable<InteractionEvent>)events;
            });
        }

        /// <summary>
        /// Converts a stream of (participantId, queue of timed states) into intervals:
        /// speaking times, presence in an area, gaze on a peer.
        /// </summary>
        public static IProducer<IEnumerable<InteractionInterval>> ToIntervals<TSource>(
            this IProducer<Dictionary<int, Queue<TSource>>> source,
            string category,
            Func<TSource, DateTime> startSelector,
            Func<TSource, DateTime> endSelector,
            Func<TSource, string> labelSelector = null)
        {
            return source.Select(dictionary =>
            {
                var intervals = new List<InteractionInterval>();
                if (dictionary == null)
                {
                    return (IEnumerable<InteractionInterval>)intervals;
                }

                foreach (var entry in dictionary)
                {
                    if (entry.Value == null)
                    {
                        continue;
                    }

                    foreach (TSource item in entry.Value)
                    {
                        intervals.Add(new InteractionInterval(
                            startSelector(item),
                            endSelector(item),
                            category,
                            (uint)entry.Key,
                            null,
                            labelSelector?.Invoke(item) ?? string.Empty));
                    }
                }

                return (IEnumerable<InteractionInterval>)intervals;
            });
        }

        /// <summary>
        /// Converts a stream keyed by area (the legacy Dictionary&lt;Location, Queue&lt;TimeData&gt;&gt;)
        /// into presence intervals of one participant, the area being carried by the Label.
        /// </summary>
        public static IProducer<IEnumerable<InteractionInterval>> ToAreaIntervals<TKey, TSource>(
            this IProducer<Dictionary<TKey, Queue<TSource>>> source,
            uint participantId,
            Func<TSource, DateTime> startSelector,
            Func<TSource, DateTime> endSelector,
            string category = IndexCategories.InArea)
        {
            return source.Select(dictionary =>
            {
                var intervals = new List<InteractionInterval>();
                if (dictionary == null)
                {
                    return (IEnumerable<InteractionInterval>)intervals;
                }

                foreach (var entry in dictionary)
                {
                    if (entry.Value == null)
                    {
                        continue;
                    }

                    foreach (TSource item in entry.Value)
                    {
                        intervals.Add(new InteractionInterval(
                            startSelector(item),
                            endSelector(item),
                            category,
                            participantId,
                            null,
                            entry.Key.ToString()));
                    }
                }

                return (IEnumerable<InteractionInterval>)intervals;
            });
        }

        /// <summary>
        /// Converts a gaze stream keyed by "gazer + gazed" (the legacy "01", "12" keys) into
        /// directed gaze events.
        /// </summary>
        public static IProducer<IEnumerable<InteractionEvent>> ToDirectedGazeEvents<TSource>(
            this IProducer<Dictionary<string, Queue<TSource>>> source,
            Func<TSource, DateTime> timeSelector,
            string category = IndexCategories.GazeOnPeer)
        {
            return source.Select(dictionary =>
            {
                var events = new List<InteractionEvent>();
                if (dictionary == null)
                {
                    return (IEnumerable<InteractionEvent>)events;
                }

                foreach (var entry in dictionary)
                {
                    if (entry.Value == null || entry.Key.Length < 2)
                    {
                        continue;
                    }

                    if (!uint.TryParse(entry.Key.Substring(0, 1), out uint gazer) ||
                        !uint.TryParse(entry.Key.Substring(1, 1), out uint gazed))
                    {
                        continue;
                    }

                    foreach (TSource item in entry.Value)
                    {
                        events.Add(new InteractionEvent(timeSelector(item), category, gazer, gazed));
                    }
                }

                return (IEnumerable<InteractionEvent>)events;
            });
        }

        /// <summary>
        /// Routes a (participantId, position) stream to the right input of a position based
        /// component, for every declared participant.
        /// </summary>
        public static void ConnectPositions(
            this IProducer<Tuple<int, Vector3>> source,
            MultiParticipantSlidingWindowComponent<PhysicalActivityLevelConfiguration> component,
            string bodyPart,
            IEnumerable<uint> participantIds,
            DeliveryPolicy<Tuple<int, Vector3>> deliveryPolicy = null)
        {
            foreach (uint participantId in participantIds)
            {
                uint id = participantId;
                source
                    .Where(tuple => tuple != null && (uint)tuple.Item1 == id, deliveryPolicy)
                    .Select(tuple => tuple.Item2)
                    .PipeTo(component.GetPositionInput(id, bodyPart));
            }
        }

        /// <summary>
        /// Same for the synchrony component.
        /// </summary>
        public static void ConnectPositions(
            this IProducer<Tuple<int, Vector3>> source,
            MultiParticipantSlidingWindowComponent<PhysicalSynchronyConfiguration> component,
            string bodyPart,
            IEnumerable<uint> participantIds,
            DeliveryPolicy<Tuple<int, Vector3>> deliveryPolicy = null)
        {
            foreach (uint participantId in participantIds)
            {
                uint id = participantId;
                source
                    .Where(tuple => tuple != null && (uint)tuple.Item1 == id, deliveryPolicy)
                    .Select(tuple => tuple.Item2)
                    .PipeTo(component.GetPositionInput(id, bodyPart));
            }
        }

        /// <summary>
        /// Turns a boolean state stream into intervals, closing the previous interval when the
        /// state goes back to false. Useful when a source only publishes a state and not an
        /// interval (for example "is currently speaking").
        /// </summary>
        public static IProducer<InteractionInterval> ToIntervals(
            this IProducer<bool> source,
            uint participantId,
            string category)
        {
            DateTime openedAt = DateTime.MinValue;
            bool isOpen = false;

            return source.Process<bool, InteractionInterval>((state, envelope, emitter) =>
            {
                if (state && !isOpen)
                {
                    openedAt = envelope.OriginatingTime;
                    isOpen = true;
                }
                else if (!state && isOpen)
                {
                    isOpen = false;
                    emitter.Post(new InteractionInterval(openedAt, envelope.OriginatingTime, category, participantId), envelope.OriginatingTime);
                }
            });
        }
    }
}
