// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    using Microsoft.Psi;

    /// <summary>
    /// Configuration settings for a replay pipeline.
    /// </summary>
    public class ReplayPipelineConfiguration : DatasetPipelineConfiguration
    {
        /// <summary>
        /// Gets or sets the type of replay operation.
        /// </summary>
        public ReplayPipeline.ReplayType ReplayType = ReplayPipeline.ReplayType.RealTime;

        /// <summary>
        /// Gets or sets the time interval for replay.
        /// </summary>
        public TimeInterval ReplayInterval = TimeInterval.Infinite;

        /// <summary>
        /// Gets or sets a value indicating whether to backup the dataset.
        /// </summary>
        public bool DatasetBackup = true;

        /// <summary>
        /// Gets or sets the progress reporter for replay operations.
        /// </summary>
        public System.IProgress<double>? ProgressReport = null;
    }
}
