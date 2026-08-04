using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Microsoft.Psi;
using SerializableClass;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Typed adapters for the stream types of the SerializableClass assembly.
    ///
    /// One adapter per source type; no indicator component ever references a domain type.
    /// The numbering convention of each source is explicit through ParticipantIdMap, because
    /// it is not the same everywhere in the existing model:
    ///
    ///   PieceStatus.userID .............. 0 based
    ///   GeneratorInteraction.userID ..... 0 based
    ///   TTData.currentSpeaker/lastSpeaker 0 based
    ///   SpeakingTimeIDData.ID ........... 0 based
    ///   NewJVAData.initiator/responder .. 1 based
    ///   gaze keys "01", "12" ............ 0 based, one character per participant
    /// </summary>
    public static class SerializableClassAdapters
    {
        // ------------------------------------------------------------------
        // Task actions: Tuple<int, Queue<PieceStatus>>
        // ------------------------------------------------------------------

        /// <summary>
        /// Task actions of one participant. The category is derived from PieceStatus.type, so a
        /// single adapter covers grab, ungrab, placed, unplaced, colored and uncolored.
        ///
        /// The Label carries object and location ("piece12@CentraleTableZone"), which is what
        /// InefficientActionPattern.RequireSameLabel matches on: the legacy rule was "same
        /// objectID and both events in the central table zone".
        /// </summary>
        public static IProducer<IEnumerable<InteractionEvent>> ToTaskEvents(
            this IProducer<Tuple<int, Queue<PieceStatus>>> source,
            ParticipantIdMap idMap = null,
            DeliveryPolicy<Tuple<int, Queue<PieceStatus>>> deliveryPolicy = null)
        {
            ParticipantIdMap map = idMap ?? ParticipantIdMap.ZeroBased;
            return source.Select(
                tuple =>
                {
                    var events = new List<InteractionEvent>();
                    if (tuple?.Item2 == null)
                    {
                        return (IEnumerable<InteractionEvent>)events;
                    }

                    foreach (PieceStatus piece in tuple.Item2)
                    {
                        events.Add(new InteractionEvent(
                            piece.originatingTime,
                            CategoryOf(piece.type),
                            map.ToParticipantId(piece.userID),
                            null,
                            1.0,
                            $"{piece.objectID}@{piece.currentLocation}"));
                    }

                    return (IEnumerable<InteractionEvent>)events;
                },
                deliveryPolicy);
        }

        /// <summary>Maps the State enum onto the categories of the framework.</summary>
        public static string CategoryOf(State state)
        {
            switch (state)
            {
                case State.Grab:
                    return IndexCategories.Grab;
                case State.Ungrab:
                    return IndexCategories.Ungrab;
                case State.Placed:
                    return IndexCategories.Place;
                case State.Unplaced:
                    return IndexCategories.Unplace;
                case State.Colored:
                    return IndexCategories.Color;
                case State.Uncolored:
                    return IndexCategories.Uncolor;
                default:
                    return state.ToString();
            }
        }

        // ------------------------------------------------------------------
        // Generator interactions: Tuple<int, Queue<GeneratorInteraction>>
        // ------------------------------------------------------------------

        public static IProducer<IEnumerable<InteractionEvent>> ToGeneratorEvents(
            this IProducer<Tuple<int, Queue<GeneratorInteraction>>> source,
            ParticipantIdMap idMap = null,
            DeliveryPolicy<Tuple<int, Queue<GeneratorInteraction>>> deliveryPolicy = null)
        {
            ParticipantIdMap map = idMap ?? ParticipantIdMap.ZeroBased;
            return source.Select(
                tuple =>
                {
                    var events = new List<InteractionEvent>();
                    if (tuple?.Item2 == null)
                    {
                        return (IEnumerable<InteractionEvent>)events;
                    }

                    foreach (GeneratorInteraction interaction in tuple.Item2)
                    {
                        events.Add(new InteractionEvent(
                            interaction.originatingTime,
                            IndexCategories.GeneratorInteraction,
                            map.ToParticipantId(interaction.userID),
                            null,
                            1.0,
                            $"generator{interaction.generatorID}:{interaction.interactionType}"));
                    }

                    return (IEnumerable<InteractionEvent>)events;
                },
                deliveryPolicy);
        }

        // ------------------------------------------------------------------
        // Turn taking: Dictionary<int, Queue<TTData>>
        // ------------------------------------------------------------------

        /// <summary>
        /// Turn takings and overlaps. The new speaker becomes ParticipantId and the previous
        /// speaker becomes TargetId, which is what makes the pair level generic: the legacy
        /// version dispatched onto AB / AC / BC with a switch over three hard coded cases.
        /// </summary>
        public static IProducer<IEnumerable<InteractionEvent>> ToTurnTakingEvents(
            this IProducer<Dictionary<int, Queue<TTData>>> source,
            string category,
            ParticipantIdMap idMap = null,
            DeliveryPolicy<Dictionary<int, Queue<TTData>>> deliveryPolicy = null)
        {
            ParticipantIdMap map = idMap ?? ParticipantIdMap.ZeroBased;
            return source.Select(
                dictionary =>
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

                        foreach (TTData turn in entry.Value)
                        {
                            events.Add(new InteractionEvent(
                                turn.originatingTime,
                                category,
                                map.ToParticipantId(turn.currentSpeaker),
                                turn.lastSpeaker >= 0 ? map.ToParticipantId(turn.lastSpeaker) : (uint?)null,
                                1.0,
                                turn.type ?? string.Empty));
                        }
                    }

                    return (IEnumerable<InteractionEvent>)events;
                },
                deliveryPolicy);
        }

        /// <summary>
        /// Silences, published as events whose Intensity is the duration in seconds, so that
        /// TurnTakingComponent.SilenceOut cumulates a duration rather than a count.
        /// </summary>
        public static IProducer<IEnumerable<InteractionEvent>> ToSilenceEvents(
            this IProducer<Dictionary<int, Queue<TTData>>> source,
            ParticipantIdMap idMap = null,
            DeliveryPolicy<Dictionary<int, Queue<TTData>>> deliveryPolicy = null)
        {
            ParticipantIdMap map = idMap ?? ParticipantIdMap.ZeroBased;
            return source.Select(
                dictionary =>
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

                        foreach (TTData silence in entry.Value)
                        {
                            events.Add(new InteractionEvent(
                                silence.originatingTime,
                                IndexCategories.Silence,
                                map.ToParticipantId(entry.Key),
                                null,
                                silence.duration));
                        }
                    }

                    return (IEnumerable<InteractionEvent>)events;
                },
                deliveryPolicy);
        }

        // ------------------------------------------------------------------
        // Joint visual attention: Queue<NewJVAData>
        // ------------------------------------------------------------------

        /// <summary>
        /// JVA episodes. Beware: initiator and responder are 1 based in NewJVAData, hence the
        /// default OneBased map. The event is stored once, under its initiator, so the group
        /// count is the number of episodes and not twice that.
        /// </summary>
        public static IProducer<IEnumerable<InteractionEvent>> ToJvaEvents(
            this IProducer<Queue<NewJVAData>> source,
            ParticipantIdMap idMap = null,
            DeliveryPolicy<Queue<NewJVAData>> deliveryPolicy = null)
        {
            ParticipantIdMap map = idMap ?? ParticipantIdMap.OneBased;
            return source.Select(
                queue =>
                {
                    var events = new List<InteractionEvent>();
                    if (queue == null)
                    {
                        return (IEnumerable<InteractionEvent>)events;
                    }

                    foreach (NewJVAData jva in queue)
                    {
                        events.Add(new InteractionEvent(
                            jva.originatingTime,
                            IndexCategories.JointVisualAttention,
                            map.ToParticipantId(jva.initiator),
                            map.ToParticipantId(jva.responder),
                            1.0,
                            jva.objectID ?? string.Empty));
                    }

                    return (IEnumerable<InteractionEvent>)events;
                },
                deliveryPolicy);
        }

        /// <summary>
        /// Group JVA episodes: one event per pair of the group, so that the pair level and the
        /// subset level both see them. JVAGroupData carries no OT, groupStart is used.
        /// </summary>
        public static IProducer<IEnumerable<InteractionEvent>> ToJvaGroupEvents(
            this IProducer<Queue<JVAGroupData>> source,
            ParticipantIdMap idMap = null,
            DeliveryPolicy<Queue<JVAGroupData>> deliveryPolicy = null)
        {
            ParticipantIdMap map = idMap ?? ParticipantIdMap.OneBased;
            return source.Select(
                queue =>
                {
                    var events = new List<InteractionEvent>();
                    if (queue == null)
                    {
                        return (IEnumerable<InteractionEvent>)events;
                    }

                    foreach (JVAGroupData group in queue)
                    {
                        if (group?.participants == null)
                        {
                            continue;
                        }

                        var ids = group.participants.Select(map.ToParticipantId).Distinct().OrderBy(i => i).ToList();
                        for (int i = 0; i < ids.Count; i++)
                        {
                            for (int j = i + 1; j < ids.Count; j++)
                            {
                                events.Add(new InteractionEvent(
                                    group.groupStart,
                                    IndexCategories.JointVisualAttention,
                                    ids[i],
                                    ids[j],
                                    1.0,
                                    group.objectID ?? string.Empty));
                            }
                        }
                    }

                    return (IEnumerable<InteractionEvent>)events;
                },
                deliveryPolicy);
        }

        // ------------------------------------------------------------------
        // Gaze on peers: Dictionary<string, Queue<TimeData>> keyed "gazer gazed"
        // ------------------------------------------------------------------

        /// <summary>
        /// Directed gaze events, one per gaze episode. The key of the dictionary is read as a
        /// pair of participant indices; keys longer than two characters are parsed as
        /// "gazer_gazed" so that groups of ten or more remain possible.
        /// </summary>
        public static IProducer<IEnumerable<InteractionEvent>> ToGazeEvents(
            this IProducer<Dictionary<string, Queue<TimeData>>> source,
            ParticipantIdMap idMap = null,
            DeliveryPolicy<Dictionary<string, Queue<TimeData>>> deliveryPolicy = null)
        {
            ParticipantIdMap map = idMap ?? ParticipantIdMap.ZeroBased;
            return source.Select(
                dictionary =>
                {
                    var events = new List<InteractionEvent>();
                    if (dictionary == null)
                    {
                        return (IEnumerable<InteractionEvent>)events;
                    }

                    foreach (var entry in dictionary)
                    {
                        if (entry.Value == null || !TryParseGazeKey(entry.Key, map, out uint gazer, out uint gazed))
                        {
                            continue;
                        }

                        foreach (TimeData gaze in entry.Value)
                        {
                            events.Add(new InteractionEvent(gaze.endOriginatingTime, IndexCategories.GazeOnPeer, gazer, gazed));
                        }
                    }

                    return (IEnumerable<InteractionEvent>)events;
                },
                deliveryPolicy);
        }

        /// <summary>
        /// Same source seen as durations rather than counts, when the index of interest is the
        /// time spent looking at a peer instead of the number of glances.
        /// </summary>
        public static IProducer<IEnumerable<InteractionInterval>> ToGazeIntervals(
            this IProducer<Dictionary<string, Queue<TimeData>>> source,
            ParticipantIdMap idMap = null,
            DeliveryPolicy<Dictionary<string, Queue<TimeData>>> deliveryPolicy = null)
        {
            ParticipantIdMap map = idMap ?? ParticipantIdMap.ZeroBased;
            return source.Select(
                dictionary =>
                {
                    var intervals = new List<InteractionInterval>();
                    if (dictionary == null)
                    {
                        return (IEnumerable<InteractionInterval>)intervals;
                    }

                    foreach (var entry in dictionary)
                    {
                        if (entry.Value == null || !TryParseGazeKey(entry.Key, map, out uint gazer, out uint gazed))
                        {
                            continue;
                        }

                        foreach (TimeData gaze in entry.Value)
                        {
                            intervals.Add(new InteractionInterval(
                                gaze.startOriginatingTime,
                                gaze.endOriginatingTime,
                                IndexCategories.GazeOnPeer,
                                gazer,
                                gazed,
                                gazed.ToString()));
                        }
                    }

                    return (IEnumerable<InteractionInterval>)intervals;
                },
                deliveryPolicy);
        }

        private static bool TryParseGazeKey(string key, ParticipantIdMap map, out uint gazer, out uint gazed)
        {
            gazer = 0;
            gazed = 0;

            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (key.IndexOf('_') >= 0)
            {
                string[] parts = key.Split('_');
                if (parts.Length < 2 || !int.TryParse(parts[0], out int from) || !int.TryParse(parts[1], out int to))
                {
                    return false;
                }

                gazer = map.ToParticipantId(from);
                gazed = map.ToParticipantId(to);
                return true;
            }

            if (key.Length != 2 ||
                !int.TryParse(key.Substring(0, 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int a) ||
                !int.TryParse(key.Substring(1, 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int b))
            {
                return false;
            }

            gazer = map.ToParticipantId(a);
            gazed = map.ToParticipantId(b);
            return true;
        }

        // ------------------------------------------------------------------
        // Presence in areas: Dictionary<Location, Queue<TimeData>>
        // ------------------------------------------------------------------

        /// <summary>
        /// Presence intervals of one participant, one queue per area.
        ///
        /// TimeData.text distinguishes a completed presence ("Out": the participant has left,
        /// the interval is [start, end]) from an ongoing one ("In": still inside, the interval
        /// stays open and is clipped at the current time by the component). This replaces the
        /// four branches of UpdateAndPostTimeInAreaV3 and its manual presenceStatus matrix.
        /// </summary>
        public static IProducer<IEnumerable<InteractionInterval>> ToPresenceIntervals(
            this IProducer<Dictionary<Location, Queue<TimeData>>> source,
            uint participantId,
            DeliveryPolicy<Dictionary<Location, Queue<TimeData>>> deliveryPolicy = null)
        {
            return source.Select(
                dictionary =>
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

                        foreach (TimeData presence in entry.Value)
                        {
                            bool isClosed = string.Equals(presence.text, "Out", StringComparison.OrdinalIgnoreCase);
                            intervals.Add(new InteractionInterval(
                                presence.startOriginatingTime,
                                isClosed ? presence.endOriginatingTime : DateTime.MaxValue,
                                IndexCategories.InArea,
                                participantId,
                                null,
                                entry.Key.ToString()));
                        }
                    }

                    return (IEnumerable<InteractionInterval>)intervals;
                },
                deliveryPolicy);
        }

        /// <summary>Area names of the Location enum, for TimeInAreaConfiguration.Areas.</summary>
        public static List<string> AreaNames(params Location[] locations) => locations.Select(l => l.ToString()).ToList();

        // ------------------------------------------------------------------
        // Speaking time: Dictionary<int, Queue<SpeakingTimeIDData>>
        // ------------------------------------------------------------------

        public static IProducer<IEnumerable<InteractionInterval>> ToSpeakingIntervals(
            this IProducer<Dictionary<int, Queue<SpeakingTimeIDData>>> source,
            ParticipantIdMap idMap = null,
            DeliveryPolicy<Dictionary<int, Queue<SpeakingTimeIDData>>> deliveryPolicy = null)
        {
            ParticipantIdMap map = idMap ?? ParticipantIdMap.ZeroBased;
            return source.Select(
                dictionary =>
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

                        foreach (SpeakingTimeIDData speech in entry.Value)
                        {
                            intervals.Add(new InteractionInterval(
                                speech.startOriginatingTime,
                                speech.endOriginatingTime,
                                IndexCategories.Speaking,
                                map.ToParticipantId(speech.ID),
                                null,
                                speech.text ?? string.Empty));
                        }
                    }

                    return (IEnumerable<InteractionInterval>)intervals;
                },
                deliveryPolicy);
        }

        // ------------------------------------------------------------------
        // F-formations: string "END_<type>_<datetime>"
        // ------------------------------------------------------------------

        /// <summary>
        /// F-formation ends of one pair. The legacy streams were one per pair
        /// (formation12In, formation13In, formation23In), so the pair is given here.
        /// </summary>
        public static IProducer<InteractionEvent> ToFormationEvents(
            this IProducer<string> source,
            uint participantA,
            uint participantB,
            DeliveryPolicy<string> deliveryPolicy = null)
        {
            return source.Process<string, InteractionEvent>(
                (message, envelope, emitter) =>
                {
                    if (string.IsNullOrEmpty(message))
                    {
                        return;
                    }

                    string[] parts = message.Split('_');
                    if (parts.Length < 3 || parts[0] != "END")
                    {
                        return;
                    }

                    DateTime time = DateTime.TryParse(parts[2], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed)
                        ? parsed
                        : envelope.OriginatingTime;

                    emitter.Post(
                        new InteractionEvent(time, IndexCategories.FormationEnd, participantA, participantB, 1.0, parts[1]),
                        envelope.OriginatingTime);
                },
                deliveryPolicy);
        }

        // ------------------------------------------------------------------
        // Positions
        // ------------------------------------------------------------------

        /// <summary>Body part positions of one participant, as consumed by the physical components.</summary>
        public static IProducer<Vector3> ToPositions(
            this IProducer<Tuple<int, Vector3>> source,
            int sourceParticipantId,
            ParticipantIdMap idMap = null,
            DeliveryPolicy<Tuple<int, Vector3>> deliveryPolicy = null)
        {
            return source
                .Where(tuple => tuple != null && tuple.Item1 == sourceParticipantId, deliveryPolicy)
                .Select(tuple => tuple.Item2);
        }

        public static IProducer<Vector3> ToPositions(
            this IProducer<BodyPartPosition> source,
            DeliveryPolicy<BodyPartPosition> deliveryPolicy = null)
            => source.Select(position => position.Position, deliveryPolicy);

        /// <summary>
        /// Head, left hand and right hand of one participant from a PositionData stream.
        /// Note: PositionData.ToVectRightHandPos parses lHandPos, so this adapter parses the
        /// right hand itself rather than calling it.
        /// </summary>
        public static (IProducer<Vector3> Head, IProducer<Vector3> LeftHand, IProducer<Vector3> RightHand) ToBodyPositions(
            this IProducer<PositionData> source,
            DeliveryPolicy<PositionData> deliveryPolicy = null)
        {
            IProducer<Vector3> head = source.Select(p => ParseVector(p.headPos), deliveryPolicy);
            IProducer<Vector3> left = source.Select(p => ParseVector(p.lHandPos), deliveryPolicy);
            IProducer<Vector3> right = source.Select(p => ParseVector(p.rHandPos), deliveryPolicy);
            return (head, left, right);
        }

        private static Vector3 ParseVector(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Vector3.Zero;
            }

            string[] parts = value.Split('_');
            if (parts.Length < 3)
            {
                return Vector3.Zero;
            }

            float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
            float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z);
            return new Vector3(x, y, z);
        }

        // ------------------------------------------------------------------
        // Phases and gaze on avatars
        // ------------------------------------------------------------------

        /// <summary>
        /// Start of a phase: in the existing protocol a PuzzleStatus with no piece left marks
        /// the beginning of the next puzzle.
        /// </summary>
        public static IProducer<bool> ToPhaseStart(this IProducer<PuzzleStatus> source, DeliveryPolicy<PuzzleStatus> deliveryPolicy = null)
            => source.Where(status => status != null && status.currentPiecesNumber == 0, deliveryPolicy).Select(_ => true);

        /// <summary>End of a phase: a PuzzleStatus with remaining pieces closes the current puzzle.</summary>
        public static IProducer<bool> ToPhaseEnd(this IProducer<PuzzleStatus> source, DeliveryPolicy<PuzzleStatus> deliveryPolicy = null)
            => source.Where(status => status != null && status.currentPiecesNumber != 0, deliveryPolicy).Select(_ => true);

        /// <summary>Boolean attention state of one participant, for AttentionLevelComponent.</summary>
        public static IProducer<bool> ToAttentionState(
            this IProducer<Tuple<int, bool>> source,
            int sourceParticipantId,
            DeliveryPolicy<Tuple<int, bool>> deliveryPolicy = null)
            => source.Where(tuple => tuple != null && tuple.Item1 == sourceParticipantId, deliveryPolicy).Select(tuple => tuple.Item2);

        /// <summary>Gaze on an avatar seen as a directed gaze event, when the gazed id is known.</summary>
        public static IProducer<InteractionEvent> ToGazeEvent(
            this IProducer<AvatarGazeEvent> source,
            Func<string, uint?> gazedResolver,
            ParticipantIdMap idMap = null,
            DeliveryPolicy<AvatarGazeEvent> deliveryPolicy = null)
        {
            ParticipantIdMap map = idMap ?? ParticipantIdMap.ZeroBased;
            return source
                .Where(gaze => gaze != null && gaze.status, deliveryPolicy)
                .Select(gaze => new InteractionEvent(
                    gaze.originatingTime,
                    IndexCategories.GazeOnPeer,
                    map.ToParticipantId(gaze.userID),
                    gazedResolver?.Invoke(gaze.objectID),
                    1.0,
                    gaze.objectID ?? string.Empty));
        }
    }
}
