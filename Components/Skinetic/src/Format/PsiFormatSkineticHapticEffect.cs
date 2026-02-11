// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    /// <summary>
    /// Provides Psi format for Skinetic haptic effect data.
    /// </summary>
    public class PsiFormatSkineticHapticEffect : IPsiFormat
    {
        /// <summary>
        /// Gets the format configuration for Skinetic haptic effect data.
        /// </summary>
        /// <returns>The format configuration.</returns>
        public dynamic GetFormat()
        {
            return PsiFormats.PsiFormatSkineticHapticEffect.GetFormat();
        }
    }
}
