// <copyright file="SlidingAverageConfiguration.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Configuration of the whole indicator pipeline by families of indices. Superseded by
    /// <see cref="CollaborationIndicesBuilder"/> and <see cref="CollaborationIndicesConfiguration"/>,
    /// which declare the indicators one by one; kept so that existing pipelines still build.
    /// </summary>
    [Obsolete("Declare the indices with CollaborationIndicesBuilder or CollaborationIndicesConfiguration. See the README of the component for the migration.")]
    public class SlidingAverageConfiguration
    {
        /// <summary>Participants of the session. Any number, contiguous or not.</summary>
        public List<uint> ParticipantIds { get; set; } = new List<uint> { 0, 1, 2 };

        /// <summary>Sliding window of every index (the legacy threshold, in milliseconds).</summary>
        public TimeSpan WindowDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Publication period of the indices.</summary>
        public TimeSpan ComputationInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>Period of the fast clock used by the attention accumulator.</summary>
        public TimeSpan AttentionInterval { get; set; } = TimeSpan.FromMilliseconds(50);

        public IndexCalibration Calibration { get; set; } = IndexCalibration.Threshold20Seconds();

        /// <summary>Areas of the environment tracked by the spatial indices.</summary>
        public List<string> Areas { get; set; } = new List<string>();

        /// <summary>Areas whose group occupancy is aggregated (planning area).</summary>
        public List<string> PlanningAreas { get; set; } = new List<string>();

        /// <summary>Body parts used by the activity level.</summary>
        public List<string> ActivityBodyParts { get; set; } = new List<string> { BodyPartNames.Head, BodyPartNames.LeftHand, BodyPartNames.RightHand };

        /// <summary>Body parts used by the synchrony.</summary>
        public List<string> SynchronyBodyParts { get; set; } = new List<string> { BodyPartNames.Head };

        /// <summary>Destination of the CSV export. Null disables it.</summary>
        public TextWriter IndicesWriter { get; set; }

        /// <summary>
        /// If true, the indices only run inside a phase, i.e. after a message on
        /// Gate.PhaseStartIn. Set it to false when the session has no phase structure or when
        /// the phase source is not wired: otherwise the gate never opens and nothing is
        /// published, even though the clocks are correctly connected.
        /// </summary>
        public bool RequirePhase { get; set; } = true;

        /// <summary>Traces the gate transitions on the console, to diagnose a silent pipeline.</summary>
        public bool LogGateTransitions { get; set; } = false;

        /// <summary>Enables the components that are only relevant for a scripted task.</summary>
        public bool UseTaskIndices { get; set; } = true;

        public bool UseSpatialIndices { get; set; } = true;

        public bool UseVerbalIndices { get; set; } = true;

        public bool UsePhysicalIndices { get; set; } = true;

        public bool UseVisualIndices { get; set; } = true;

        public bool ComputeCollaborationScores { get; set; } = true;

        public bool GenerateGraph { get; set; } = true;

        /// <summary>
        /// If true, the instance creates its own clock generators. Set it to false when several
        /// instances run side by side, and drive ClockIn and AttentionClockIn from a single
        /// shared generator: two generators of the same period produce two slightly different
        /// tick sequences, which would make the instances impossible to compare row by row.
        /// </summary>
        public bool UseInternalClock { get; set; } = false;

        /// <summary>Deep enough copy to give another instance an independent configuration.</summary>
        public SlidingAverageConfiguration Clone() => new SlidingAverageConfiguration
        {
            ParticipantIds = new List<uint>(this.ParticipantIds),
            WindowDuration = this.WindowDuration,
            ComputationInterval = this.ComputationInterval,
            AttentionInterval = this.AttentionInterval,
            Calibration = this.Calibration,
            Areas = new List<string>(this.Areas),
            PlanningAreas = new List<string>(this.PlanningAreas),
            ActivityBodyParts = new List<string>(this.ActivityBodyParts),
            SynchronyBodyParts = new List<string>(this.SynchronyBodyParts),
            IndicesWriter = this.IndicesWriter,
            UseTaskIndices = this.UseTaskIndices,
            UseSpatialIndices = this.UseSpatialIndices,
            UseVerbalIndices = this.UseVerbalIndices,
            UseVisualIndices = this.UseVisualIndices,
            UsePhysicalIndices = this.UsePhysicalIndices,
            UseInternalClock = this.UseInternalClock,
            ComputeCollaborationScores = this.ComputeCollaborationScores,
            GenerateGraph = this.GenerateGraph,
            RequirePhase = this.RequirePhase,
            LogGateTransitions = this.LogGateTransitions,
        };
    }
}
