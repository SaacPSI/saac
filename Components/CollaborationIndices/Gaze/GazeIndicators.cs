// <copyright file="GazeIndicators.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Options of <see cref="JointVisualAttentionIndicator"/>.
    /// </summary>
    public class JointVisualAttentionOptions : ComponentIndicatorOptions<JointVisualAttentionConfiguration>
    {
    }

    /// <summary>
    /// Joint visual attention: number of episodes where a participant follows the visual
    /// attention of another one (<see cref="JointVisualAttentionComponent"/>). Connect the
    /// episodes to <c>Component.EventsIn</c>, the initiator in ParticipantId and the
    /// responder in TargetId.
    /// </summary>
    public class JointVisualAttentionIndicator : ComponentIndicator<JointVisualAttentionOptions, JointVisualAttentionComponent>
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.JointVisualAttention;

        /// <inheritdoc/>
        public override IEnumerable<string> Validate(IndicatorBuildContext context) => this.RequireParticipants(context, 2);

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            var configuration = new JointVisualAttentionConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                WindowDuration = context.WindowDuration,
                ComputationInterval = TimeSpan.Zero,
                GroupNormalizer = context.NormalizerFor(IndexNames.JointVisualAttention),
                PairNormalizer = context.NormalizerFor(IndexNames.JointVisualAttentionPair),
                ComputeOnDataReception = false,
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new JointVisualAttentionComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("JVA")));
            context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);

            // Only the levels the component is configured to post are declared.
            if (configuration.Levels.HasFlag(IndexLevel.Group))
            {
                context.PublishGroup(this.Name, this.Component.GroupOut);
                context.PublishScoreInput(this.Name, this.Component.GroupOut);
            }

            if (configuration.Levels.HasFlag(IndexLevel.UndirectedPair))
            {
                context.PublishPair(this.Name, this.Component.PairOut);
            }
        }
    }

    /// <summary>
    /// Options of <see cref="GazeOnPeersIndicator"/>.
    /// </summary>
    public class GazeOnPeersOptions : ComponentIndicatorOptions<GazeOnPeersConfiguration>
    {
    }

    /// <summary>
    /// Gaze on peers: how often each participant looks at each peer
    /// (<see cref="GazeOnPeersComponent"/>). Connect the gaze events to
    /// <c>Component.EventsIn</c>, the gazer in ParticipantId and the gazed peer in TargetId.
    /// </summary>
    public class GazeOnPeersIndicator : ComponentIndicator<GazeOnPeersOptions, GazeOnPeersComponent>
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.GazeOnPeers;

        /// <inheritdoc/>
        public override IEnumerable<string> Validate(IndicatorBuildContext context) => this.RequireParticipants(context, 2);

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            var configuration = new GazeOnPeersConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                WindowDuration = context.WindowDuration,
                ComputationInterval = TimeSpan.Zero,
                GroupNormalizer = context.NormalizerFor(IndexNames.GazeOnPeers),
                ComputeOnDataReception = false,
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new GazeOnPeersComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("GazeOnPeers")));
            context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);

            // Only the levels the component is configured to post are declared.
            if (configuration.Levels.HasFlag(IndexLevel.Group))
            {
                context.PublishGroup(this.Name, this.Component.GroupOut);
                context.PublishScoreInput(this.Name, this.Component.GroupOut);
            }

            if (configuration.Levels.HasFlag(IndexLevel.DirectedPair))
            {
                context.PublishDirectedPair(this.Name, this.Component.DirectedPairOut);
            }
        }
    }

    /// <summary>
    /// Options of <see cref="AttentionLevelIndicator"/>.
    /// </summary>
    public class AttentionLevelOptions : ComponentIndicatorOptions<AttentionLevelConfiguration>
    {
        /// <summary>
        /// Gets or sets the increment applied per tick of the attention clock while the
        /// participant is attentive. Set it to the period of that clock.
        /// </summary>
        public TimeSpan Step { get; set; } = TimeSpan.FromMilliseconds(50);

        /// <summary>Gets or sets a value indicating whether the level is published in [0, 1] instead of milliseconds.</summary>
        public bool PublishAsRatio { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the indicator creates its own clock, of
        /// period Step. Leave it false to drive <c>ClockIn</c> from a stream, which is what a
        /// replay needs.
        /// </summary>
        public bool UseInternalClock { get; set; } = false;
    }

    /// <summary>
    /// Attention level: an accumulator that grows while a participant is attentive and
    /// decreases otherwise (<see cref="AttentionLevelComponent"/>). It is the only indicator
    /// that does not follow the clock of the indices: it has its own, faster, to connect to
    /// <see cref="ClockIn"/>. The graph therefore shows its latest value rather than a value
    /// of the tick. Connect the attention states to <c>Component.GetAttentiveInput(participantId)</c>.
    /// </summary>
    public class AttentionLevelIndicator : ComponentIndicator<AttentionLevelOptions, AttentionLevelComponent>
    {
        /// <inheritdoc/>
        public override string DefaultName => IndexNames.AttentionLevel;

        /// <summary>Gets the clock of the accumulator.</summary>
        public Receiver<bool> ClockIn => this.Component.TickIn;

        /// <inheritdoc/>
        public override IEnumerable<string> Validate(IndicatorBuildContext context)
        {
            if (this.Options.Step <= TimeSpan.Zero)
            {
                yield return $"Indicator '{this.Name}': Step must be strictly positive.";
            }
        }

        /// <inheritdoc/>
        public override void Build(IndicatorBuildContext context)
        {
            var configuration = new AttentionLevelConfiguration
            {
                ParticipantIds = context.CopyParticipantIds(),
                Step = this.Options.Step,
                ComputeOnDataReception = false,
                PublishAsRatio = this.Options.PublishAsRatio,
            };

            this.Options.Configure?.Invoke(configuration);

            this.Component = new AttentionLevelComponent(context.Pipeline, configuration, context.ComponentName(this.SuffixOrName("AttentionLevel")));

            if (this.Options.UseInternalClock)
            {
                Generators.Repeat(context.Pipeline, true, this.Options.Step).PipeTo(this.Component.TickIn);
            }

            // No DriveByClock: the level is not a value of the tick, the graph reads the latest one.
            context.PublishIndividual(this.Name, this.Component.Out, IndexUsage.Graph);
        }
    }
}
