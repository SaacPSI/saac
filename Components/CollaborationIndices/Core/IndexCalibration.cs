// <copyright file="IndexCalibration.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Calibration of an index: the reference value (typically the P95 measured on a corpus)
    /// that should map to ReferenceScore once normalized.
    /// Replaces the alpha1..alpha12bis fields and the DefineAlpha switch of the legacy class.
    /// </summary>
    public class IndexCalibration
    {
        public Dictionary<string, double> ReferenceValues { get; set; } = new Dictionary<string, double>();

        public double ReferenceScore { get; set; } = 0.95;

        public IIndexNormalizer NormalizerFor(string indexName)
        {
            if (this.ReferenceValues != null && this.ReferenceValues.TryGetValue(indexName, out double reference) && reference > 0)
            {
                return ExponentialSaturationNormalizer.FromReference(reference, this.ReferenceScore);
            }

            return new IdentityNormalizer();
        }

        /// <summary>
        /// Calibration measured on the 27 session corpus, for a 30 s window.
        /// The keys are the index names used by SlidingAverageComputation.
        /// </summary>
        public static IndexCalibration Threshold20Seconds() => new IndexCalibration
        {
            ReferenceValues = new Dictionary<string, double>
            {
                { IndexNames.JointVisualAttention, 8 },
                { IndexNames.JointVisualAttentionPair, 5 },
                { IndexNames.GazeOnPeers, 7 },
                { IndexNames.TaskParticipation, 18 },
                { IndexNames.Formation, 4 },
                { IndexNames.Movement, 0.041 },
                { IndexNames.VerbalParticipation, 20 },
                { IndexNames.TurnTakingWithOverlap, 2 },
                { IndexNames.TurnTakingWithoutOverlap, 4 },
                { IndexNames.TurnTakingWithoutOverlapPair, 3 },
            },
        };

        public static IndexCalibration Threshold30Seconds() => new IndexCalibration
        {
            ReferenceValues = new Dictionary<string, double>
            {
                { IndexNames.JointVisualAttention, 11 },
                { IndexNames.JointVisualAttentionPair, 8 },
                { IndexNames.GazeOnPeers, 10 },
                { IndexNames.TaskParticipation, 27 },
                { IndexNames.Formation, 5 },
                { IndexNames.Movement, 0.041 },
                { IndexNames.VerbalParticipation, 30 },
                { IndexNames.TurnTakingWithOverlap, 2 },
                { IndexNames.TurnTakingWithoutOverlap, 5 },
                { IndexNames.TurnTakingWithoutOverlapPair, 4 },
            },
        };

        public static IndexCalibration Threshold45Seconds() => new IndexCalibration
        {
            ReferenceValues = new Dictionary<string, double>
            {
                { IndexNames.JointVisualAttention, 18 },
                { IndexNames.JointVisualAttentionPair, 11 },
                { IndexNames.GazeOnPeers, 14 },
                { IndexNames.TaskParticipation, 32 },
                { IndexNames.Formation, 8 },
                { IndexNames.Movement, 0.037 },
                { IndexNames.VerbalParticipation, 45 },
                { IndexNames.TurnTakingWithOverlap, 3 },
                { IndexNames.TurnTakingWithoutOverlap, 5 },
                { IndexNames.TurnTakingWithoutOverlapPair, 3 },
            },
        };

        /// <summary>
        /// Calibration matching a window duration. The calibration and the window must always
        /// be chosen together: a P95 measured on 20 s means nothing on a 45 s window, and using
        /// the wrong one silently compresses or stretches every normalized score.
        /// Windows without a measured calibration fall back to the closest one.
        /// </summary>
        public static IndexCalibration ForWindow(TimeSpan window)
        {
            int seconds = (int)Math.Round(window.TotalSeconds);
            switch (seconds)
            {
                case 20:
                    return Threshold20Seconds();
                case 30:
                    return Threshold30Seconds();
                case 45:
                    return Threshold45Seconds();
                default:
                    return seconds < 25 ? Threshold20Seconds() : (seconds < 35 ? Threshold30Seconds() : Threshold45Seconds());
            }
        }
    }
}
