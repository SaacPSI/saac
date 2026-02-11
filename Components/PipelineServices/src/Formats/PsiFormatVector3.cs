// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    /// <summary>
    /// Provides a wrapper for Vector3 serialization format.
    /// </summary>
    public class PsiFormatVector3 : IPsiFormat
    {
        /// <summary>
        /// Gets the format for serializing and deserializing Vector3 objects.
        /// </summary>
        /// <returns>A format instance for Vector3 serialization.</returns>
        public dynamic GetFormat()
        {
            return PsiFormats.PsiFormatVector3.GetFormat();
        }
    }
}
