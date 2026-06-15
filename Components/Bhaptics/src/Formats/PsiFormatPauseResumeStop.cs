// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using SAAC.PipelineServices;

    /// <summary>
    /// Provides a \psi binary serialization format for <see cref="SAAC.Bhaptic.Helpers.PauseResumeStop"/>.
    /// Used to transmit haptic playback state change commands (pause, resume, stop) over TCP streams
    /// and store them in a dataset.
    /// The <c>state</c> enum is serialized as an <c>int</c>.
    /// </summary>
    public class PsiFormatPauseResumeStop : IPsiFormat
    {
        /// <summary>
        /// Gets the format for serializing and deserializing PauseResumeStop values.
        /// </summary>
        /// <returns>A format instance for PauseResumeStop serialization.</returns>
        public dynamic GetFormat()
        {
            return SAAC.Bhaptics.PsiFormats.PsiFormatPauseResumeStop.GetFormat();
        }
    }
}
