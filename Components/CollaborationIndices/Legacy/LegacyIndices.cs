// <copyright file="LegacyIndices.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.Psi;
using SAAC.PipelineServices;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Translation of a <see cref="SlidingAverageConfiguration"/> into builder calls.
    ///
    /// This is the selection of indicators that used to be hard coded in the constructor of
    /// SlidingAverageComputation, kept in one place for the pipelines that still use the
    /// Use... flags. It reproduces what that constructor did when it was replaced:
    ///  - turn taking is not instantiated;
    ///  - joint visual attention, gaze on peers and speech equality are computed and exported
    ///    but do not enter the collaboration score.
    /// A pipeline that wants any of those writes its own builder calls instead.
    /// </summary>
    [Obsolete("Only used by the constructors kept for compatibility.")]
    internal static class LegacyIndices
    {
        public static CollaborationIndicesBuilder ToBuilder(Pipeline pipeline, DatasetPipeline? server, SlidingAverageConfiguration configuration, string name)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            List<uint> participants = configuration.ParticipantIds ?? new List<uint>();

            CollaborationIndicesBuilder builder = new CollaborationIndicesBuilder(pipeline)
                .WithName(name)
                .WithParticipants(participants)
                .WithWindow(configuration.WindowDuration)
                .WithComputationInterval(configuration.ComputationInterval)
                .WithPhaseGate(configuration.RequirePhase, configuration.LogGateTransitions);

            if (configuration.Calibration != null)
            {
                builder.WithCalibration(configuration.Calibration);
            }

            if (server != null)
            {
                builder.WithStore(server);
            }

            if (configuration.UseInternalClock)
            {
                builder.WithInternalClock();
            }

            // The attention accumulator has always been created, whatever the flags.
            builder.AddIndicator(Indicators.AttentionLevel, options =>
            {
                options.Step = configuration.AttentionInterval;
                options.UseInternalClock = configuration.UseInternalClock;
            });

            if (configuration.UsePhysicalIndices)
            {
                builder.AddIndicator(Indicators.Movement, options =>
                {
                    options.BodyParts = new List<string>(configuration.ActivityBodyParts);
                    options.AdditionalWindows = new List<TimeSpan> { TimeSpan.FromSeconds(5) };
                });
                builder.AddIndicator(Indicators.Synchrony, options => options.BodyParts = new List<string>(configuration.SynchronyBodyParts));
            }

            if (configuration.UseVerbalIndices)
            {
                builder.AddIndicator(Indicators.VerbalParticipation);
                builder.AddIndicator(Indicators.SpeechEquality, options => options.IncludeInScore = false);
                builder.AddIndicator(Indicators.TalkingMost);
            }

            if (configuration.UseVisualIndices)
            {
                builder.AddIndicator(Indicators.JointVisualAttention, options => options.IncludeInScore = false);
                builder.AddIndicator(Indicators.GazeOnPeers, options => options.IncludeInScore = false);
            }

            if (configuration.UseTaskIndices)
            {
                builder.AddIndicator(Indicators.TaskParticipation);
                builder.AddIndicator(Indicators.TaskEquality);
                builder.AddIndicator(Indicators.TaskingMost);
            }

            if (configuration.UseSpatialIndices)
            {
                builder.AddIndicator(Indicators.TimeInArea, options =>
                {
                    options.Areas = new List<string>(configuration.Areas ?? new List<string>());
                    options.PlanningAreas = new List<string>(configuration.PlanningAreas ?? new List<string>());
                });
                builder.AddIndicator(Indicators.Formation);
                builder.AddIndicator(Indicators.Proximity);
            }

            if (configuration.ComputeCollaborationScores)
            {
                builder.WithCollaborationScore(SlidingAverageComputation.DefaultDimensions());
            }

            if (configuration.GenerateGraph)
            {
                builder.WithInteractionGraph();
            }

            if (configuration.IndicesWriter != null)
            {
                builder.WithCsvExport(configuration.IndicesWriter, ExportColumns(participants));
            }

            return builder;
        }

        /// <summary>
        /// Columns the export has always had, whether or not the corresponding index is
        /// computed: an absent one stays "NA". Existing readers of these files rely on the order.
        /// </summary>
        private static List<string> ExportColumns(List<uint> participants)
        {
            var columns = new List<string>();
            foreach (uint participantId in participants)
            {
                columns.Add($"{IndexNames.Movement}_{participantId}");
                columns.Add($"{IndexNames.VerbalParticipation}_{participantId}");
                columns.Add($"{IndexNames.TaskParticipation}_{participantId}");
            }

            foreach (ParticipantPair pair in Combinatorics.Pairs(participants))
            {
                columns.Add($"{IndexNames.Synchrony}_{pair}");
            }

            columns.AddRange(new[]
            {
                IndexNames.SpeechEquality,
                IndexNames.TaskEquality,
                IndexNames.JointVisualAttention,
                IndexNames.GazeOnPeers,
                IndexNames.CollaborationScore,
            });

            return columns;
        }
    }
}
