// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    /// <summary>
    /// Command types.
    /// </summary>
    public enum Command
    {
        /// <summary>
        /// Initialize command.
        /// </summary>
        Initialize,

        /// <summary>
        /// Run command.
        /// </summary>
        Run,

        /// <summary>
        /// Stop command.
        /// </summary>
        Stop,

        /// <summary>
        /// Reset command.
        /// </summary>
        Reset,

        /// <summary>
        /// Close command.
        /// </summary>
        Close,

        /// <summary>
        /// Status command.
        /// </summary>
        Status,
    }
}
