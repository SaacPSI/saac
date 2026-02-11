// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    /// <summary>
    /// Interface for PSI format providers that can serialize and deserialize data.
    /// </summary>
    public interface IPsiFormat
    {
        /// <summary>
        /// Gets the format instance for serialization and deserialization.
        /// </summary>
        /// <returns>A dynamic format object.</returns>
        abstract dynamic GetFormat();
    }
}
