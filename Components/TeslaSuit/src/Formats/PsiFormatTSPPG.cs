// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using SAAC.PipelineServices;

namespace SAAC.TeslaSuit
{
    /// <summary>
    /// Provides Psi format for TeslaSuit PPG data.
    /// </summary>
    public class PsiFormatTsPPG : IPsiFormat
    {
        /// <summary>
        /// Gets the format configuration for TeslaSuit PPG data.
        /// </summary>
        /// <returns>The format configuration.</returns>
        public dynamic GetFormat()
        {
            return PsiFormats.PsiFormatTsPPG.GetFormat();
        }
    }
}
