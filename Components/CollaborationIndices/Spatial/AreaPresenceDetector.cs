using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class AreaPresenceDetectorConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>Separator of the fields of an area event ("User1;In;In;CentraleTableArea").</summary>
        public char Separator { get; set; } = ';';

        /// <summary>Position of the "In" / "Out" field.</summary>
        public int StateFieldIndex { get; set; } = 1;

        /// <summary>Position of the name of the area.</summary>
        public int AreaFieldIndex { get; set; } = 3;

        public string EnterKeyword { get; set; } = "In";

        public string LeaveKeyword { get; set; } = "Out";

        /// <summary>
        /// Name given to each area in the published intervals. An area that is not listed
        /// keeps the name it has in the events. The default gives the areas of the puzzle
        /// task the names of the Location enumeration, which is what the task streams use.
        /// </summary>
        public Dictionary<string, string> AreaNames { get; set; } = new Dictionary<string, string>
        {
            { "CentraleTableArea", "CentraleTableZone" },
            { "Generator1Area", "Generator1" },
            { "Generator2Area", "Generator2" },
            { "IterationArea", "IterationTable" },
            { "ValidationButton", "Button" },
        };

        /// <summary>Area published while a participant is in none.</summary>
        public string OutsideName { get; set; } = "Outside";
    }

    /// <summary>
    /// Presence of each participant in the areas of the scene, from the enter and leave
    /// events of the headsets.
    ///
    /// An "In" event opens an interval, published at once with an open end so that the time
    /// in area counts the participant while still inside; the matching "Out" event publishes
    /// the same interval closed (the interval stores update it in place). The intervals go
    /// to the TimeInArea indicator.
    ///
    /// Differences with the legacy AreaMovements:
    ///  - an "Out" event without "In" (the participant was already inside when the recording
    ///    began) is ignored, where the legacy counted the presence from the start of the
    ///    task or of the current puzzle;
    ///  - the current area is the most recently entered area the participant is still in,
    ///    where the legacy published "Outside" on any leave, even from a nested area;
    ///  - any area name is accepted, where the legacy knew five.
    /// </summary>
    public class AreaPresenceDetector
    {
        private readonly AreaPresenceDetectorConfiguration configuration;
        private readonly ReceiverMap<uint, string> areaReceivers;
        private readonly EmitterMap<uint, InteractionInterval> presenceEmitters;
        private readonly EmitterMap<uint, string> currentAreaEmitters;
        private readonly MonotonicPoster poster = new MonotonicPoster(NonMonotonicPolicy.Advance);

        // For each participant, the areas currently occupied and when each was entered.
        private readonly Dictionary<uint, Dictionary<string, DateTime>> occupied = new Dictionary<uint, Dictionary<string, DateTime>>();

        public AreaPresenceDetector(Pipeline pipeline, AreaPresenceDetectorConfiguration configuration, string name = nameof(AreaPresenceDetector))
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.areaReceivers = new ReceiverMap<uint, string>(pipeline, this, configuration.ParticipantIds, this.ReceiveAreaEvent, $"{name}-Area");
            this.presenceEmitters = new EmitterMap<uint, InteractionInterval>(pipeline, this, configuration.ParticipantIds, $"{name}-Presence");
            this.currentAreaEmitters = new EmitterMap<uint, string>(pipeline, this, configuration.ParticipantIds, $"{name}-CurrentArea");
            this.Out = pipeline.CreateEmitter<InteractionInterval>(this, $"{name}-Presence");

            foreach (uint participantId in configuration.ParticipantIds)
            {
                this.occupied[participantId] = new Dictionary<string, DateTime>();
            }
        }

        /// <summary>Presence intervals of every participant.</summary>
        public Emitter<InteractionInterval> Out { get; }

        /// <summary>Area events of one participant.</summary>
        public Receiver<string> GetAreaInput(uint participantId) => this.areaReceivers[participantId];

        /// <summary>Presence intervals of one participant, category InArea, the area in the label.</summary>
        public Emitter<InteractionInterval> GetPresenceEmitter(uint participantId) => this.presenceEmitters[participantId];

        /// <summary>Area one participant is in, published at each change.</summary>
        public Emitter<string> GetCurrentAreaEmitter(uint participantId) => this.currentAreaEmitters[participantId];

        private void ReceiveAreaEvent(uint participantId, string areaEvent, Envelope envelope)
        {
            if (string.IsNullOrEmpty(areaEvent))
            {
                return;
            }

            string[] fields = areaEvent.Split(this.configuration.Separator);
            if (fields.Length <= Math.Max(this.configuration.StateFieldIndex, this.configuration.AreaFieldIndex))
            {
                return;
            }

            string state = fields[this.configuration.StateFieldIndex];
            string area = fields[this.configuration.AreaFieldIndex];
            if (this.configuration.AreaNames != null && this.configuration.AreaNames.TryGetValue(area, out string renamed))
            {
                area = renamed;
            }

            Dictionary<string, DateTime> areas = this.occupied[participantId];
            DateTime time = envelope.OriginatingTime;
            if (state == this.configuration.EnterKeyword)
            {
                if (areas.ContainsKey(area))
                {
                    // Entered twice without leaving: the first entry stands.
                    return;
                }

                areas[area] = time;
                this.Publish(participantId, new InteractionInterval(time, DateTime.MaxValue, IndexCategories.InArea, participantId, null, area), time);
            }
            else if (state == this.configuration.LeaveKeyword)
            {
                if (!areas.TryGetValue(area, out DateTime entered))
                {
                    return;
                }

                areas.Remove(area);
                this.Publish(participantId, new InteractionInterval(entered, time, IndexCategories.InArea, participantId, null, area), time);
            }
            else
            {
                return;
            }

            string current = areas.Count == 0
                ? this.configuration.OutsideName
                : areas.OrderByDescending(entry => entry.Value).First().Key;
            this.poster.Post(this.currentAreaEmitters[participantId], current, time);
        }

        private void Publish(uint participantId, InteractionInterval interval, DateTime time)
        {
            this.poster.Post(this.presenceEmitters[participantId], interval, time);
            this.poster.Post(this.Out, interval.Clone(), time);
        }
    }
}
