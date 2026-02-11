// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    /// <summary>
    /// PSI format implementation for a list of simplified body data.
    /// </summary>
    public class PsiFormatListOfSimplifiedBody : IPsiFormat
    {
        /// <summary>
        /// Gets the format definition for a list of simplified bodies.
        /// </summary>
        /// <returns>The format definition object.</returns>
        public dynamic GetFormat()
        {
            return PsiFormats.PsiFormatListOfSimplifiedBody.GetFormat();
        }
    }
}
