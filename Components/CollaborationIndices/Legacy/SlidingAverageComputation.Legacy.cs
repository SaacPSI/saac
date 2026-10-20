// <copyright file="SlidingAverageComputation.Legacy.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using Microsoft.Psi;
using SAAC.PipelineServices;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// What existed before the builder: the constructor taking a configuration by families of
    /// indices, and one property per component. Everything here is a shortcut over the
    /// builder and over <c>Get</c>; nothing new should be added to this file.
    /// </summary>
    public partial class SlidingAverageComputation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SlidingAverageComputation"/> class from
        /// the Use... flags of a <see cref="SlidingAverageConfiguration"/>.
        /// </summary>
        /// <param name="pipeline">Pipeline or subpipeline hosting the components.</param>
        /// <param name="server">Dataset pipeline storing the streams. Null stores nothing.</param>
        /// <param name="configuration">Configuration by families of indices.</param>
        /// <param name="name">Name of the instance, prefix of every component and stream name.</param>
        [Obsolete("Declare the indices with CollaborationIndicesBuilder: new CollaborationIndicesBuilder(pipeline).AddIndicator(Indicators.Movement)...Build(). See the README of the component for the migration.")]
        public SlidingAverageComputation(Pipeline pipeline, DatasetPipeline server, SlidingAverageConfiguration configuration, string name = nameof(SlidingAverageComputation))
            : this(LegacyIndices.ToBuilder(pipeline, server, configuration, name).CreateBlueprint())
        {
        }

        /// <summary>Gets the clock of the attention accumulator, or null when the instance has none.</summary>
        public Receiver<bool>? AttentionClockIn => this.AttentionLevel?.TickIn;

        /// <summary>Gets the component of <see cref="Indicators.Movement"/>, or null when the instance does not compute it.</summary>
        public PhysicalActivityLevelComponent? ActivityLevel => this.Find<PhysicalActivityIndicator>(IndexNames.Movement)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.Synchrony"/>, or null when the instance does not compute it.</summary>
        public PhysicalSynchronyComponent? Synchrony => this.Find<PhysicalSynchronyIndicator>(IndexNames.Synchrony)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.VerbalParticipation"/>, or null when the instance does not compute it.</summary>
        public VerbalParticipationComponent? VerbalParticipation => this.Find<VerbalParticipationIndicator>(IndexNames.VerbalParticipation)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.SpeechEquality"/>, or null when the instance does not compute it.</summary>
        public EqualityIndexComponent? SpeechEquality => this.Find<EqualityIndicator>(IndexNames.SpeechEquality)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.TurnTaking"/>, or null when the instance does not compute it.</summary>
        public TurnTakingComponent? TurnTaking => this.Find<TurnTakingIndicator>(IndexNames.TurnTaking)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.JointVisualAttention"/>, or null when the instance does not compute it.</summary>
        public JointVisualAttentionComponent? JointVisualAttention => this.Find<JointVisualAttentionIndicator>(IndexNames.JointVisualAttention)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.GazeOnPeers"/>, or null when the instance does not compute it.</summary>
        public GazeOnPeersComponent? GazeOnPeers => this.Find<GazeOnPeersIndicator>(IndexNames.GazeOnPeers)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.AttentionLevel"/>, or null when the instance does not compute it.</summary>
        public AttentionLevelComponent? AttentionLevel => this.Find<AttentionLevelIndicator>(IndexNames.AttentionLevel)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.TaskParticipation"/>, or null when the instance does not compute it.</summary>
        public TaskParticipationComponent? TaskParticipation => this.Find<TaskParticipationIndicator>(IndexNames.TaskParticipation)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.TaskEquality"/>, or null when the instance does not compute it.</summary>
        public EqualityIndexComponent? TaskEquality => this.Find<EqualityIndicator>(IndexNames.TaskEquality)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.TimeInArea"/>, or null when the instance does not compute it.</summary>
        public TimeInAreaComponent? TimeInArea => this.Find<TimeInAreaIndicator>(IndexNames.TimeInArea)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.Formation"/>, or null when the instance does not compute it.</summary>
        public FFormationComponent? FFormation => this.Find<FFormationIndicator>(IndexNames.Formation)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.Proximity"/>, or null when the instance does not compute it.</summary>
        public ProximityComponent? Proximity => this.Find<ProximityIndicator>(IndexNames.Proximity)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.TalkingMost"/>, or null when the instance does not compute it.</summary>
        public DominanceIdentityComponent? TalkingMost => this.Find<DominanceIndicator>(IndexNames.TalkingMost)?.Component;

        /// <summary>Gets the component of <see cref="Indicators.TaskingMost"/>, or null when the instance does not compute it.</summary>
        public DominanceIdentityComponent? TaskingMost => this.Find<DominanceIndicator>(IndexNames.TaskingMost)?.Component;
    }
}
