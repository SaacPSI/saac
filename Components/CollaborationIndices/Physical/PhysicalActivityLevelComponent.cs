using System;
using System.Collections.Generic;
using System.IdentityModel.Protocols.WSTrust;
using System.Numerics;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using SAAC.PipelineServices;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Individual physical activity level, computed for N participants on a sliding window.
    /// The index is the weighted mean of the movement of the configured body parts over the
    /// last WindowDuration.
    ///
    /// Real time cost: one computation is O(P * B * log n), independent of the window
    /// duration and of the sensor frame rate, because the travelled distance is cumulated
    /// when the samples arrive (see PositionTrack).
    ///
    /// Outputs:
    ///  - Out: all the participants at once, for downstream group indicators;
    ///  - GetActivityLevelEmitter(id): one stream per participant, for storage and display;
    ///  - GroupActivityLevelOut: group level aggregation;
    ///  - GetWindowedActivityLevelsEmitter(window): one stream per additional window.
    /// </summary>
    public class PhysicalActivityLevelComponent : MultiParticipantSlidingWindowComponent<PhysicalActivityLevelConfiguration>,
                                                  IProducer<Dictionary<uint, double>>
    {
        private Dictionary<uint, Emitter<double>> participantsActivityLevelOut = new Dictionary<uint, Emitter<double>>();
        private Dictionary<TimeSpan, Emitter<Dictionary<uint, double>>> windowEmitters = new Dictionary<TimeSpan, Emitter<Dictionary<uint, double>>>();

        public PhysicalActivityLevelComponent(Pipeline pipeline, DatasetPipeline server, PhysicalActivityLevelConfiguration configuration, string name = nameof(PhysicalActivityLevelComponent))
            : base(pipeline, configuration, name)
        {
            this.SessionName = server.GetSession("RawDataPipelineProcess.000");

            this.Out = pipeline.CreateEmitter<Dictionary<uint, double>>(this, $"{name}-ActivityLevels");
            this.GroupActivityLevelOut = pipeline.CreateEmitter<double>(this, $"{name}-GroupActivityLevel");
            server.CreateConnectorAndStore($"{name}-GroupActivityLevel", "LiveVisualization", this.SessionName, pipeline, this.GroupActivityLevelOut.Type, this.GroupActivityLevelOut, true);

            foreach (uint participantId in configuration.ParticipantIds)
            {
                this.participantsActivityLevelOut[participantId] = pipeline.CreateEmitter<double>(this, $"{name}-ActivityLevel-{participantId}");
                server.CreateConnectorAndStore($"{name}-ActivityLevel-{participantId}", "LiveVisualization", this.SessionName, pipeline, this.participantsActivityLevelOut[participantId].Type, this.participantsActivityLevelOut[participantId], true);
            }

            if (configuration.AdditionalWindows != null)
            {
                foreach (TimeSpan window in configuration.AdditionalWindows)
                {
                    if (!this.windowEmitters.ContainsKey(window))
                    {
                        this.windowEmitters[window] = pipeline.CreateEmitter<Dictionary<uint, double>>(this, $"{name}-ActivityLevels-{(int)window.TotalMilliseconds}ms");
                    }
                }
            }
        }

        public Session SessionName;

        /// <summary>
        /// Activity level of every participant, on the main window.
        /// </summary>
        public Emitter<Dictionary<uint, double>> Out { get; }

        /// <summary>
        /// Aggregated activity level of the whole group.
        /// </summary>
        public Emitter<double> GroupActivityLevelOut { get; }

        /// <summary>
        /// Dedicated stream of one participant.
        /// </summary>
        public Emitter<double> GetActivityLevelEmitter(uint participantId)
        {
            if (!this.participantsActivityLevelOut.TryGetValue(participantId, out var emitter))
            {
                throw new ArgumentException($"Participant {participantId} is not declared in the configuration of {this.name}.", nameof(participantId));
            }

            return emitter;
        }

        /// <summary>
        /// Stream of one of the additional windows declared in the configuration.
        /// </summary>
        public Emitter<Dictionary<uint, double>> GetWindowedActivityLevelsEmitter(TimeSpan window)
        {
            if (!this.windowEmitters.TryGetValue(window, out var emitter))
            {
                throw new ArgumentException($"Window {window} is not declared in AdditionalWindows of {this.name}.", nameof(window));
            }

            return emitter;
        }

        /// <summary>
        /// Activity level of one participant over the last <paramref name="window"/>.
        /// Public so that it can be unit tested or reused without going through the pipeline.
        /// </summary>
        public double ComputeActivityLevel(uint participantId, TimeSpan window, DateTime currentTime)
        {
            DateTime start = currentTime - window;
            double weightedSum = 0;
            double totalWeight = 0;

            foreach (string bodyPart in this.configuration.BodyParts)
            {
                double weight = this.configuration.GetWeight(bodyPart);
                var track = this.buffer.GetTrack(participantId, bodyPart);

                double distance = track.DistanceOverWindow(start, out double elapsedSeconds, out int stepCount);
                double movement = 0;

                if (stepCount + 1 >= this.configuration.MinimumSampleCount)
                {
                    movement = this.configuration.Unit == MovementUnit.DisplacementPerSecond
                        ? (elapsedSeconds > double.Epsilon ? distance / elapsedSeconds : 0)
                        : distance / stepCount;
                }

                weightedSum += weight * movement;
                totalWeight += weight;
            }

            if (this.configuration.NormalizeWeights && totalWeight > double.Epsilon)
            {
                return weightedSum / totalWeight;
            }

            return weightedSum;
        }

        protected override bool CanCompute(DateTime originatingTime)
        {
            if (this.configuration.WarmUp == WarmUpBehavior.PublishPartial)
            {
                return true;
            }

            // Wait until every participant has enough data on the whole window.
            DateTime start = originatingTime - this.configuration.WindowDuration;
            foreach (uint participantId in this.configuration.ParticipantIds)
            {
                foreach (string bodyPart in this.configuration.BodyParts)
                {
                    var track = this.buffer.GetTrack(participantId, bodyPart);
                    track.DistanceOverWindow(start, out _, out int stepCount);
                    if (stepCount + 1 < this.configuration.MinimumSampleCount)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        protected override void Compute(DateTime originatingTime)
        {
            var levels = this.ComputeAll(this.configuration.WindowDuration, originatingTime);

            this.Out.Post(levels, originatingTime);
            foreach (var entry in this.participantsActivityLevelOut)
            {
                entry.Value.Post(levels[entry.Key], originatingTime);
            }

            this.GroupActivityLevelOut.Post(this.configuration.GroupAggregator.Aggregate(levels.Values), originatingTime);

            foreach (var entry in this.windowEmitters)
            {
                entry.Value.Post(this.ComputeAll(entry.Key, originatingTime), originatingTime);
            }
        }

        private Dictionary<uint, double> ComputeAll(TimeSpan window, DateTime originatingTime)
        {
            var levels = new Dictionary<uint, double>();
            foreach (uint participantId in this.configuration.ParticipantIds)
            {
                levels[participantId] = this.ComputeActivityLevel(participantId, window, originatingTime);
            }

            return levels;
        }
    }
}
