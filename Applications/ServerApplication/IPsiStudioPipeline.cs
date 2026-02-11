// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace Microsoft.Psi.PsiStudio.PipelinePlugin
{
    using System;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Enum providing the replay mode of the pipeline.
    /// </summary>
    public enum PipelineReplaybleMode
    {
        /// <summary>
        /// The pipeline is not replayable.
        /// </summary>
        Not,

        /// <summary>
        /// The pipeline is reading stores form the dataset.
        /// </summary>
        Pipeline,

        /// <summary>
        /// The pipeline is not reading stores form the dataset but the application need to replay a session from a dataset.
        /// </summary>
        PsiStudio,
    }

    /// <summary>
    /// Interface that plugin should implement.
    /// </summary>
    internal interface IPsiStudioPipeline
    {
        /// <summary>
        /// Return the dataset loaded by the plugin.
        /// </summary>
        /// <returns>The dataset of the plugin.</returns>
        public Dataset GetDataset();

        /// <summary>
        /// Run the pipeline's) of the plugin.
        /// </summary>
        /// <param name="timeInterval">Interval to replay incase some stores are read.</param>
        public void RunPipeline(TimeInterval timeInterval);

        /// <summary>
        /// Stop the pipeline's of the plugin.
        /// </summary>
        public void StopPipeline();

        /// <summary>
        /// Dispose the plugin.
        /// </summary>
        public void Dispose();

        /// <summary>
        /// Gets the time of pipeline start (Pipeline.StartTime).
        /// </summary>
        /// <returns>Returns the start time of the pipeline.</returns>
        public DateTime GetStartTime();

        /// <summary>
        /// Gets the mode of the plugin.
        /// </summary>
        /// <returns>Returns the replay mode.</returns>
        public PipelineReplaybleMode GetReplaybleMode();

        // *** Optionnals *** //
        // /// <summary>
        // /// Gets the layout of the plugin.
        // /// </summary>
        // /// <returns>Returns the layout xml in string.</returns>
        // public string GetLayout();

        // /// <summary>
        // /// Gets the annotation of the plugin.
        // /// </summary>
        // /// <returns>Returns the annotation xml in string.</returns>
        // public string GetAnnotation();

        // /// <summary>
        // /// Register an action to trigger when the plugin as loaded the dataset.
        // /// </summary>
        // /// <param name="action">The action to trigger.</param>
        // public void OnDatasetLoaded(Action<Dataset> action);
    }
}
