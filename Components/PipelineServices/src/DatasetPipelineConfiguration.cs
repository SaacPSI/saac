// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    /// <summary>
    /// Configuration settings for a dataset pipeline.
    /// </summary>
    public class DatasetPipelineConfiguration
    {
        /// <summary>
        /// Gets or sets the diagnostics mode for the pipeline.
        /// </summary>
        public DatasetPipeline.DiagnosticsMode Diagnostics = DatasetPipeline.DiagnosticsMode.Off;

        /// <summary>
        /// Gets or sets a value indicating whether debug mode is enabled.
        /// </summary>
        public bool Debug = false;

        /// <summary>
        /// Gets or sets a value indicating whether the pipeline should run automatically.
        /// </summary>
        public bool AutomaticPipelineRun = false;

        /// <summary>
        /// Gets or sets the session naming mode.
        /// </summary>
        public DatasetPipeline.SessionNamingMode SessionMode = DatasetPipeline.SessionNamingMode.Increment;

        /// <summary>
        /// Gets or sets the store mode for pipeline data.
        /// </summary>
        public DatasetPipeline.StoreMode StoreMode = DatasetPipeline.StoreMode.Independant;

        /// <summary>
        /// Gets or sets the file path to the dataset.
        /// </summary>
        public string DatasetPath = string.Empty;

        /// <summary>
        /// Gets or sets the name of the dataset.
        /// </summary>
        public string DatasetName = string.Empty;

        /// <summary>
        /// Gets or sets the name of the session.
        /// </summary>
        public string SessionName = string.Empty;

        /// <summary>
        /// Gets or sets the mapping between stream names and store names.
        /// </summary>
        public Dictionary<string, string> StreamToStore = new Dictionary<string, string>();
    }
}
