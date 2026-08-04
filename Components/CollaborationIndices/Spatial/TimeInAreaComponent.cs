using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class TimeInAreaConfiguration : IndexComponentConfiguration
    {
        /// <summary>Areas tracked by the component. Any string is accepted.</summary>
        public List<string> Areas { get; set; } = new List<string>();

        /// <summary>Category of the presence intervals.</summary>
        public string PresenceCategory { get; set; } = IndexCategories.InArea;

        /// <summary>If true, the value is the share of the window (0 to 1) instead of seconds.</summary>
        public bool AsRatioOfWindow { get; set; } = false;

        /// <summary>Areas whose group time is aggregated in GroupAreaTimeOut (e.g. the planning area).</summary>
        public List<string> GroupAggregatedAreas { get; set; } = new List<string>();
    }

    /// <summary>
    /// Time spent by each participant in each area of the environment, over the sliding window.
    ///
    /// The presence is expressed as intervals, so the partial overlap with the window edges is
    /// handled once by InteractionInterval.DurationWithin. The legacy version rebuilt this from
    /// In/Out events with four branches per case and a manual presence flag; an interval that is
    /// still open (participant currently inside the area) is simply clipped at the current time.
    ///
    /// Outputs:
    ///  - Out: per participant, per area;
    ///  - GetEmitter(participantId, area): dedicated stream;
    ///  - AreaTotalsOut: group total per area;
    ///  - GroupAreaTimeOut: normalized group time over the aggregated areas.
    /// </summary>
    public class TimeInAreaComponent : MultiParticipantIntervalComponent<TimeInAreaConfiguration>,
                                       IProducer<Dictionary<uint, Dictionary<string, double>>>
    {
        private readonly Dictionary<uint, KeyedEmitters<string>> areaEmitters = new Dictionary<uint, KeyedEmitters<string>>();

        public TimeInAreaComponent(Pipeline pipeline, TimeInAreaConfiguration configuration, string name = nameof(TimeInAreaComponent))
            : base(pipeline, configuration, name)
        {
            this.Out = pipeline.CreateEmitter<Dictionary<uint, Dictionary<string, double>>>(this, $"{name}-TimeInAreas");
            this.AreaTotalsOut = pipeline.CreateEmitter<Dictionary<string, double>>(this, $"{name}-AreaTotals");
            this.GroupAreaTimeOut = pipeline.CreateEmitter<double>(this, $"{name}-GroupAreaTime");

            foreach (uint participantId in configuration.ParticipantIds)
            {
                this.areaEmitters[participantId] = new KeyedEmitters<string>(pipeline, this, configuration.Areas, $"{name}-{participantId}");
            }
        }

        public Emitter<Dictionary<uint, Dictionary<string, double>>> Out { get; }

        public Emitter<Dictionary<string, double>> AreaTotalsOut { get; }

        public Emitter<double> GroupAreaTimeOut { get; }

        public Emitter<double> GetEmitter(uint participantId, string area)
        {
            if (!this.areaEmitters.TryGetValue(participantId, out var emitters))
            {
                throw new ArgumentException($"Participant {participantId} is not declared in the configuration of {this.name}.", nameof(participantId));
            }

            return emitters[area];
        }

        protected override void Compute(DateTime originatingTime)
        {
            DateTime start = this.WindowStart(originatingTime);
            double windowSeconds = this.WindowSeconds;

            var perParticipant = new Dictionary<uint, Dictionary<string, double>>();
            var areaTotals = new Dictionary<string, double>();
            foreach (string area in this.configuration.Areas)
            {
                areaTotals[area] = 0;
            }

            foreach (uint participantId in this.configuration.ParticipantIds)
            {
                var perArea = new Dictionary<string, double>();
                foreach (string area in this.configuration.Areas)
                {
                    double seconds = this.DurationInArea(participantId, area, start, originatingTime);

                    // The time inside the window can never exceed the window itself.
                    if (seconds > windowSeconds)
                    {
                        seconds = windowSeconds;
                    }

                    double value = this.configuration.AsRatioOfWindow && windowSeconds > double.Epsilon ? seconds / windowSeconds : seconds;
                    perArea[area] = this.Normalize(value);
                    areaTotals[area] += seconds;
                }

                perParticipant[participantId] = perArea;
                this.areaEmitters[participantId].PostAll(perArea, originatingTime);
            }

            this.Out.Post(perParticipant, originatingTime);
            this.AreaTotalsOut.Post(areaTotals, originatingTime);

            if (this.configuration.GroupAggregatedAreas != null && this.configuration.GroupAggregatedAreas.Count > 0)
            {
                double total = this.configuration.GroupAggregatedAreas.Sum(area => areaTotals.TryGetValue(area, out double value) ? value : 0);
                double maximum = windowSeconds * this.configuration.ParticipantIds.Count;
                this.GroupAreaTimeOut.Post(maximum > double.Epsilon ? total / maximum : 0, originatingTime);
            }
        }

        /// <summary>
        /// Presence duration in one area. The area is carried either by the interval Label
        /// or by a dedicated category, which lets an upstream component choose its encoding.
        /// </summary>
        private double DurationInArea(uint participantId, string area, DateTime windowStart, DateTime windowEnd)
        {
            double total = 0;

            foreach (var interval in this.intervals.Get(participantId, this.configuration.PresenceCategory))
            {
                if (interval.Label == area)
                {
                    total += interval.DurationWithin(windowStart, windowEnd);
                }
            }

            total += this.intervals.DurationWithin(participantId, $"{this.configuration.PresenceCategory}:{area}", windowStart, windowEnd);

            return total;
        }
    }
}
