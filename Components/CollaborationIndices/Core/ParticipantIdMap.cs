using System;
using System.Collections.Generic;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Numbering convention of a source stream.
    /// </summary>
    public enum ParticipantIdBase
    {
        /// <summary>Identifiers start at 0 (PieceStatus.userID, TTData.currentSpeaker, SpeakingTimeIDData.ID).</summary>
        ZeroBased,

        /// <summary>Identifiers start at 1 (NewJVAData.initiator and responder).</summary>
        OneBased,

        /// <summary>Identifiers are explicit and mapped through the dictionary.</summary>
        Explicit,
    }

    /// <summary>
    /// Translates the identifiers of a source stream into the identifiers used by the components.
    ///
    /// This is not decoration: in the existing model the same participant is 0 in
    /// PieceStatus.userID and TTData.currentSpeaker, but 1 in NewJVAData.initiator, which the
    /// legacy code compensated for with a scattered "- 1" (numberJVAInitiator[jvaEvent.initiator - 1]).
    /// Getting it wrong shifts a whole indicator onto the wrong participant without any error.
    /// </summary>
    public class ParticipantIdMap
    {
        public static readonly ParticipantIdMap ZeroBased = new ParticipantIdMap(ParticipantIdBase.ZeroBased);

        public static readonly ParticipantIdMap OneBased = new ParticipantIdMap(ParticipantIdBase.OneBased);

        private readonly Dictionary<int, uint> explicitMap;

        public ParticipantIdMap(ParticipantIdBase idBase)
        {
            this.Base = idBase;
            this.explicitMap = new Dictionary<int, uint>();
        }

        public ParticipantIdMap(Dictionary<int, uint> explicitMapping)
        {
            this.Base = ParticipantIdBase.Explicit;
            this.explicitMap = explicitMapping ?? new Dictionary<int, uint>();
        }

        public ParticipantIdBase Base { get; }

        /// <summary>Source identifier to framework identifier.</summary>
        public uint ToParticipantId(int sourceId)
        {
            switch (this.Base)
            {
                case ParticipantIdBase.OneBased:
                    return (uint)Math.Max(0, sourceId - 1);
                case ParticipantIdBase.Explicit:
                    return this.explicitMap.TryGetValue(sourceId, out uint mapped) ? mapped : (uint)Math.Max(0, sourceId);
                default:
                    return (uint)Math.Max(0, sourceId);
            }
        }

        public uint? ToParticipantId(int? sourceId) => sourceId.HasValue ? this.ToParticipantId(sourceId.Value) : (uint?)null;

        /// <summary>Framework identifier back to the source convention, for the legacy exports.</summary>
        public int ToSourceId(uint participantId)
        {
            switch (this.Base)
            {
                case ParticipantIdBase.OneBased:
                    return (int)participantId + 1;
                case ParticipantIdBase.Explicit:
                    foreach (var entry in this.explicitMap)
                    {
                        if (entry.Value == participantId)
                        {
                            return entry.Key;
                        }
                    }

                    return (int)participantId;
                default:
                    return (int)participantId;
            }
        }
    }
}
