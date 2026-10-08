// <copyright file="Indicators.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Catalogue of the indicators of the library. Each entry is what the builder takes to
    /// declare an indicator, and what the built instance takes to give it back:
    ///
    /// <code>
    /// var indices = new CollaborationIndicesBuilder(pipeline)
    ///     .AddIndicator(Indicators.Movement)
    ///     .AddIndicator(Indicators.Synchrony, o => o.SubsetSize = 3)
    ///     .Build();
    ///
    /// positions.PipeTo(indices.Get(Indicators.Movement).Component.GetPositionInput(0, BodyPartNames.Head));
    /// </code>
    ///
    /// The name of an entry is also the "Type" of the indicator in a configuration file, the
    /// default name of its outputs, and the key of its reference value in the calibration.
    /// </summary>
    public static class Indicators
    {
        /// <summary>Physical activity level of each participant and of the group.</summary>
        public static readonly IndicatorType<PhysicalActivityIndicator, PhysicalActivityOptions> Movement
            = IndicatorRegistry.Register<PhysicalActivityIndicator, PhysicalActivityOptions>(IndexNames.Movement);

        /// <summary>Synchrony of the movements of each pair and of the group.</summary>
        public static readonly IndicatorType<PhysicalSynchronyIndicator, PhysicalSynchronyOptions> Synchrony
            = IndicatorRegistry.Register<PhysicalSynchronyIndicator, PhysicalSynchronyOptions>(IndexNames.Synchrony);

        /// <summary>Time spent speaking by each participant and by the group.</summary>
        public static readonly IndicatorType<VerbalParticipationIndicator, VerbalParticipationOptions> VerbalParticipation
            = IndicatorRegistry.Register<VerbalParticipationIndicator, VerbalParticipationOptions>(IndexNames.VerbalParticipation);

        /// <summary>Equality of the speaking times. Needs <see cref="VerbalParticipation"/>.</summary>
        public static readonly IndicatorType<EqualityIndicator, EqualityOptions> SpeechEquality
            = IndicatorRegistry.Register<EqualityIndicator, EqualityOptions>(IndexNames.SpeechEquality, () => new EqualityIndicator(IndexNames.SpeechEquality, IndexNames.VerbalParticipation));

        /// <summary>Identity of the participant who speaks the most. Needs <see cref="VerbalParticipation"/>.</summary>
        public static readonly IndicatorType<DominanceIndicator, DominanceOptions> TalkingMost
            = IndicatorRegistry.Register<DominanceIndicator, DominanceOptions>(IndexNames.TalkingMost, () => new DominanceIndicator(IndexNames.TalkingMost, IndexNames.VerbalParticipation));

        /// <summary>Turn takings with and without overlap, and overlaps.</summary>
        public static readonly IndicatorType<TurnTakingIndicator, TurnTakingOptions> TurnTaking
            = IndicatorRegistry.Register<TurnTakingIndicator, TurnTakingOptions>(IndexNames.TurnTaking);

        /// <summary>Cumulated time during which no participant speaks. Same speech intervals as <see cref="VerbalParticipation"/>.</summary>
        public static readonly IndicatorType<SilenceIndicator, SimultaneousSpeechOptions> Silence
            = IndicatorRegistry.Register<SilenceIndicator, SimultaneousSpeechOptions>(IndexNames.Silence);

        /// <summary>Cumulated time during which at least two participants speak at the same time. Same speech intervals as <see cref="VerbalParticipation"/>.</summary>
        public static readonly IndicatorType<CrossTalkIndicator, SimultaneousSpeechOptions> CrossTalk
            = IndicatorRegistry.Register<CrossTalkIndicator, SimultaneousSpeechOptions>(IndexNames.CrossTalk);

        /// <summary>Episodes of joint visual attention.</summary>
        public static readonly IndicatorType<JointVisualAttentionIndicator, JointVisualAttentionOptions> JointVisualAttention
            = IndicatorRegistry.Register<JointVisualAttentionIndicator, JointVisualAttentionOptions>(IndexNames.JointVisualAttention);

        /// <summary>Gazes of each participant on each peer.</summary>
        public static readonly IndicatorType<GazeOnPeersIndicator, GazeOnPeersOptions> GazeOnPeers
            = IndicatorRegistry.Register<GazeOnPeersIndicator, GazeOnPeersOptions>(IndexNames.GazeOnPeers);

        /// <summary>Times two participants look at each other at the same time.</summary>
        public static readonly IndicatorType<MutualGazeIndicator, MutualGazeOptions> MutualGaze
            = IndicatorRegistry.Register<MutualGazeIndicator, MutualGazeOptions>(IndexNames.MutualGaze);

        /// <summary>Attention accumulator of each participant, on its own fast clock.</summary>
        public static readonly IndicatorType<AttentionLevelIndicator, AttentionLevelOptions> AttentionLevel
            = IndicatorRegistry.Register<AttentionLevelIndicator, AttentionLevelOptions>(IndexNames.AttentionLevel);

        /// <summary>Productive task actions of each participant and of the group.</summary>
        public static readonly IndicatorType<TaskParticipationIndicator, TaskParticipationOptions> TaskParticipation
            = IndicatorRegistry.Register<TaskParticipationIndicator, TaskParticipationOptions>(IndexNames.TaskParticipation);

        /// <summary>Equality of the task participations. Needs <see cref="TaskParticipation"/>.</summary>
        public static readonly IndicatorType<EqualityIndicator, EqualityOptions> TaskEquality
            = IndicatorRegistry.Register<EqualityIndicator, EqualityOptions>(IndexNames.TaskEquality, () => new EqualityIndicator(IndexNames.TaskEquality, IndexNames.TaskParticipation));

        /// <summary>Identity of the participant who acts the most on the task. Needs <see cref="TaskParticipation"/>.</summary>
        public static readonly IndicatorType<DominanceIndicator, DominanceOptions> TaskingMost
            = IndicatorRegistry.Register<DominanceIndicator, DominanceOptions>(IndexNames.TaskingMost, () => new DominanceIndicator(IndexNames.TaskingMost, IndexNames.TaskParticipation));

        /// <summary>Time spent by each participant in each area.</summary>
        public static readonly IndicatorType<TimeInAreaIndicator, TimeInAreaOptions> TimeInArea
            = IndicatorRegistry.Register<TimeInAreaIndicator, TimeInAreaOptions>(IndexNames.TimeInArea);

        /// <summary>Shared spatial formations (F-formations).</summary>
        public static readonly IndicatorType<FFormationIndicator, FFormationOptions> Formation
            = IndicatorRegistry.Register<FFormationIndicator, FFormationOptions>(IndexNames.Formation);

        /// <summary>Interpersonal distance of each pair.</summary>
        public static readonly IndicatorType<ProximityIndicator, ProximityOptions> Proximity
            = IndicatorRegistry.Register<ProximityIndicator, ProximityOptions>(IndexNames.Proximity);

        /// <summary>Equality of any distribution: set <c>Source</c> and <c>Name</c> in the options.</summary>
        public static readonly IndicatorType<EqualityIndicator, EqualityOptions> Equality
            = IndicatorRegistry.Register<EqualityIndicator, EqualityOptions>("Equality");

        /// <summary>Leader of any distribution: set <c>Source</c> and <c>Name</c> in the options.</summary>
        public static readonly IndicatorType<DominanceIndicator, DominanceOptions> Dominance
            = IndicatorRegistry.Register<DominanceIndicator, DominanceOptions>("Dominance");
    }
}
