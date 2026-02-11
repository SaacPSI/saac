// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC
{
    namespace PipelineServices
    {
        /// <summary>
        /// Provides a wrapper for Command serialization format.
        /// </summary>
        public class PsiFormatCommand : IPsiFormat
        {
            /// <summary>
            /// Gets the format for serializing and deserializing Command tuples.
            /// </summary>
            /// <returns>A format instance for Command serialization.</returns>
            public dynamic GetFormat()
            {
                return PsiFormats.PsiFormatCommand.GetFormat();
            }
        }
    }
}
