// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PsiFormats
{
    using SAAC.PipelineServices;

    /// <summary>
    /// Provides a \psi binary serialization format for <see cref="SAAC.Bhaptic.Helpers.HapticPlay"/>.
    /// Used to transmit pattern-based haptic play events over TCP streams and store them in a dataset.
    /// </summary>
    public class PsiFormatMotorPlay : IPsiFormat
    {
        /// <summary>
        /// Gets the format for serializing and deserializing MotorPlay values.
        /// </summary>
        /// <returns>A format instance for MotorPlay serialization.</returns>
        public dynamic GetFormat()
        {
            return SAAC.Bhaptics.PsiFormats.PsiFormatMotorPlay.GetFormat();
        }
    }
}
