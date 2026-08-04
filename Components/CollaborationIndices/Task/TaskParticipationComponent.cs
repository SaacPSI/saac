using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Pattern of two consecutive actions on the same object that cancel each other,
    /// e.g. grabbing then releasing a piece at the same place without doing anything with it.
    /// </summary>
    public class InefficientActionPattern
    {
        public string FirstCategory { get; set; } = IndexCategories.Grab;

        public string SecondCategory { get; set; } = IndexCategories.Ungrab;

        /// <summary>If true, the two events must carry the same Label (object id, area...).</summary>
        public bool RequireSameLabel { get; set; } = true;

        /// <summary>Maximum delay between the two actions. Zero means no limit.</summary>
        public TimeSpan MaximumDelay { get; set; } = TimeSpan.Zero;
    }

    public class TaskParticipationConfiguration : EventCountIndexConfiguration
    {
        public TaskParticipationConfiguration()
        {
            this.Categories = new List<string>
            {
                IndexCategories.Grab,
                IndexCategories.Ungrab,
                IndexCategories.Place,
                IndexCategories.Color,
                IndexCategories.Uncolor,
                IndexCategories.GeneratorInteraction,
            };
            this.Levels = IndexLevel.Individual | IndexLevel.Group;
        }

        /// <summary>Patterns detected as inefficient (legacy: Grab then Ungrab on the central table).</summary>
        public List<InefficientActionPattern> InefficientPatterns { get; set; } = new List<InefficientActionPattern>
        {
            new InefficientActionPattern(),
        };

        /// <summary>Each detected inefficient action removes this many units from the score (legacy: 2).</summary>
        public double InefficientActionPenalty { get; set; } = 2.0;

        /// <summary>Categories counted as interfering rather than productive (legacy: unplaced).</summary>
        public List<string> InterferingCategories { get; set; } = new List<string> { IndexCategories.Unplace };

        /// <summary>If true, the score cannot go below zero after the penalty.</summary>
        public bool ClampToZero { get; set; } = true;
    }

    /// <summary>
    /// Task participation: number of productive actions of each participant over the window,
    /// minus a penalty for the actions that cancel each other.
    ///
    /// The legacy version hard coded the Grab/Ungrab pattern on the central table and the
    /// list of task streams. Here both are configuration, and the number of participants
    /// is free.
    ///
    /// Additional outputs on top of EventCountIndexComponent:
    ///  - InefficientActionsOut: number of detected inefficient actions per participant;
    ///  - InterferingParticipationOut: interfering actions per participant.
    /// </summary>
    public class TaskParticipationComponent : EventCountIndexComponent<TaskParticipationConfiguration>
    {
        private Dictionary<uint, double> lastInefficientActions = new Dictionary<uint, double>();

        public TaskParticipationComponent(Pipeline pipeline, TaskParticipationConfiguration configuration, string name = nameof(TaskParticipationComponent))
            : base(pipeline, configuration, name)
        {
            this.InefficientActionsOut = pipeline.CreateEmitter<Dictionary<uint, double>>(this, $"{name}-InefficientActions");
            this.InterferingParticipationOut = pipeline.CreateEmitter<Dictionary<uint, double>>(this, $"{name}-InterferingParticipation");
        }

        public Emitter<Dictionary<uint, double>> InefficientActionsOut { get; }

        public Emitter<Dictionary<uint, double>> InterferingParticipationOut { get; }

        protected override Dictionary<uint, double> ComputeIndividualRaw(DateTime currentTime)
        {
            Dictionary<uint, double> counts = this.CountsByParticipant(this.configuration.Categories, currentTime);
            this.lastInefficientActions = this.CountInefficientActions(currentTime);

            foreach (uint participantId in counts.Keys.ToList())
            {
                double penalty = this.lastInefficientActions[participantId] * this.configuration.InefficientActionPenalty;
                double value = counts[participantId] - penalty;
                counts[participantId] = this.configuration.ClampToZero && value < 0 ? 0 : value;
            }

            return counts;
        }

        protected override void OnComputed(Dictionary<uint, double> individualRaw, DateTime originatingTime)
        {
            this.InefficientActionsOut.Post(this.lastInefficientActions, originatingTime);

            var interfering = new Dictionary<uint, double>();
            DateTime start = this.WindowStart(originatingTime);
            foreach (uint participantId in this.configuration.ParticipantIds)
            {
                double total = this.lastInefficientActions[participantId];
                foreach (string category in this.configuration.InterferingCategories)
                {
                    total += this.events.CountWithin(participantId, category, start, originatingTime);
                }

                interfering[participantId] = total;
            }

            this.InterferingParticipationOut.Post(interfering, originatingTime);
        }

        /// <summary>
        /// Walks the merged action sequence of each participant and counts the configured
        /// cancelling patterns. Replaces the legacy MergeSortedLists + manual comparison.
        /// </summary>
        private Dictionary<uint, double> CountInefficientActions(DateTime currentTime)
        {
            DateTime start = this.WindowStart(currentTime);
            var result = new Dictionary<uint, double>();

            foreach (uint participantId in this.configuration.ParticipantIds)
            {
                double count = 0;

                foreach (InefficientActionPattern pattern in this.configuration.InefficientPatterns)
                {
                    var sequence = new List<InteractionEvent>();
                    sequence.AddRange(this.events.Within(participantId, pattern.FirstCategory, start, currentTime));
                    sequence.AddRange(this.events.Within(participantId, pattern.SecondCategory, start, currentTime));
                    sequence.Sort((a, b) => a.OriginatingTime.CompareTo(b.OriginatingTime));

                    for (int i = 1; i < sequence.Count; i++)
                    {
                        InteractionEvent previous = sequence[i - 1];
                        InteractionEvent current = sequence[i];

                        if (previous.Category != pattern.FirstCategory || current.Category != pattern.SecondCategory)
                        {
                            continue;
                        }

                        if (pattern.RequireSameLabel && previous.Label != current.Label)
                        {
                            continue;
                        }

                        if (pattern.MaximumDelay > TimeSpan.Zero && current.OriginatingTime - previous.OriginatingTime > pattern.MaximumDelay)
                        {
                            continue;
                        }

                        count++;
                    }
                }

                result[participantId] = count;
            }

            return result;
        }
    }
}
