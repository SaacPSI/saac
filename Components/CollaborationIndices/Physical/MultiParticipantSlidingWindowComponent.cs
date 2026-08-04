using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Base class of the multi participant sliding window indicators.
    /// It owns:
    ///  - one Vector3 receiver per participant and per body part, created from the configuration,
    ///  - one "skeleton" receiver per participant, for sources that emit all the body parts at once,
    ///  - an optional clock receiver (TickIn) to drive the computation at a fixed rate,
    ///  - the sliding buffer, its pruning and the throttling of the computations.
    /// Derived classes only implement Compute().
    /// </summary>
    /// <typeparam name="TConfiguration">Configuration type of the indicator.</typeparam>
    public abstract class MultiParticipantSlidingWindowComponent<TConfiguration>
        where TConfiguration : SlidingWindowConfiguration
    {
        private readonly Dictionary<uint, Dictionary<string, Receiver<Vector3>>> positionReceivers
            = new Dictionary<uint, Dictionary<string, Receiver<Vector3>>>();

        private readonly Dictionary<uint, Receiver<Dictionary<string, Vector3>>> skeletonReceivers
            = new Dictionary<uint, Receiver<Dictionary<string, Vector3>>>();

        private DateTime lastComputationTime = DateTime.MinValue;

        protected readonly Pipeline pipeline;
        protected readonly TConfiguration configuration;
        protected readonly MultiParticipantSlidingBuffer buffer;
        protected readonly string name;

        protected MultiParticipantSlidingWindowComponent(Pipeline pipeline, TConfiguration configuration, string name)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (configuration.ParticipantIds == null || configuration.ParticipantIds.Count < configuration.MinimumParticipantCount)
            {
                throw new ArgumentException($"{name} needs at least {configuration.MinimumParticipantCount} participant(s).", nameof(configuration));
            }

            if (configuration.BodyParts == null || configuration.BodyParts.Count == 0)
            {
                throw new ArgumentException($"{name} needs at least one body part.", nameof(configuration));
            }

            this.pipeline = pipeline;
            this.configuration = configuration;
            this.name = name;
            this.buffer = new MultiParticipantSlidingBuffer(configuration.ParticipantIds, configuration.BodyParts);

            foreach (uint participantId in configuration.ParticipantIds)
            {
                uint participant = participantId;
                var receivers = new Dictionary<string, Receiver<Vector3>>();
                foreach (string bodyPart in configuration.BodyParts)
                {
                    string part = bodyPart;
                    receivers[part] = pipeline.CreateReceiver<Vector3>(
                        this,
                        (position, envelope) => this.OnPosition(participant, part, position, envelope),
                        $"{name}-In-{participant}-{part}");
                }

                this.positionReceivers[participant] = receivers;
                this.skeletonReceivers[participant] = pipeline.CreateReceiver<Dictionary<string, Vector3>>(
                    this,
                    (skeleton, envelope) => this.OnSkeleton(participant, skeleton, envelope),
                    $"{name}-InBody-{participant}");
            }

            this.TickIn = pipeline.CreateReceiver<bool>(this, (_, envelope) => this.TryCompute(envelope.OriginatingTime), $"{name}-Tick");
        }

        /// <summary>
        /// Clock input. Connect a Generators.Repeat() to compute at a fixed rate,
        /// typically with configuration.ComputeOnDataReception set to false.
        /// </summary>
        public Receiver<bool> TickIn { get; }

        public TConfiguration Configuration => this.configuration;

        public IReadOnlyList<uint> ParticipantIds => this.configuration.ParticipantIds;

        /// <summary>
        /// Input of one body part of one participant.
        /// </summary>
        public Receiver<Vector3> GetPositionInput(uint participantId, string bodyPart)
        {
            if (!this.positionReceivers.TryGetValue(participantId, out var receivers))
            {
                throw new ArgumentException($"Participant {participantId} is not declared in the configuration of {this.name}.", nameof(participantId));
            }

            if (!receivers.TryGetValue(bodyPart, out var receiver))
            {
                throw new ArgumentException($"Body part {bodyPart} is not declared in the configuration of {this.name}.", nameof(bodyPart));
            }

            return receiver;
        }

        /// <summary>
        /// Input of all the body parts of one participant at once.
        /// Keys not declared in the configuration are ignored.
        /// </summary>
        public Receiver<Dictionary<string, Vector3>> GetBodyInput(uint participantId)
        {
            if (!this.skeletonReceivers.TryGetValue(participantId, out var receiver))
            {
                throw new ArgumentException($"Participant {participantId} is not declared in the configuration of {this.name}.", nameof(participantId));
            }

            return receiver;
        }

        /// <summary>
        /// Computes and posts the indicator. Called at most once per ComputationInterval.
        /// </summary>
        protected abstract void Compute(DateTime originatingTime);

        /// <summary>
        /// Additional guard evaluated before Compute (enough data, etc.).
        /// </summary>
        protected virtual bool CanCompute(DateTime originatingTime) => true;

        protected void TryCompute(DateTime originatingTime)
        {
            this.buffer.Prune(originatingTime - this.configuration.BufferRetention);

            // Strictly increasing originating times are mandatory for \psi emitters.
            if (originatingTime <= this.lastComputationTime)
            {
                return;
            }

            if (originatingTime - this.lastComputationTime < this.configuration.ComputationInterval)
            {
                return;
            }

            if (!this.CanCompute(originatingTime))
            {
                return;
            }

            this.lastComputationTime = originatingTime;
            this.Compute(originatingTime);
        }

        private void OnPosition(uint participantId, string bodyPart, Vector3 position, Envelope envelope)
        {
            this.buffer.Add(participantId, bodyPart, envelope.OriginatingTime, position);
            if (this.configuration.ComputeOnDataReception)
            {
                this.TryCompute(envelope.OriginatingTime);
            }
        }

        private void OnSkeleton(uint participantId, Dictionary<string, Vector3> body, Envelope envelope)
        {
            if (body == null)
            {
                return;
            }

            foreach (string bodyPart in this.configuration.BodyParts)
            {
                if (body.TryGetValue(bodyPart, out Vector3 position))
                {
                    this.buffer.Add(participantId, bodyPart, envelope.OriginatingTime, position);
                }
            }

            if (this.configuration.ComputeOnDataReception)
            {
                this.TryCompute(envelope.OriginatingTime);
            }
        }

        public override string ToString() => this.name;
    }
}
