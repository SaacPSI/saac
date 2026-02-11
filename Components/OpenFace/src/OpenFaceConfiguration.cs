// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.OpenFace
{
    /// <summary>
    /// Configuration settings for the OpenFace facial analysis component.
    /// </summary>
    public class OpenFaceConfiguration
    {
        /// <summary>
        /// Gets or sets the directory path containing the OpenFace models.
        /// </summary>
        public string ModelDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether pose detection is enabled.
        /// </summary>
        public bool Pose { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether eye tracking is enabled.
        /// </summary>
        public bool Eyes { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether facial action unit detection is enabled.
        /// </summary>
        public bool Face { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenFaceConfiguration"/> class.
        /// </summary>
        /// <param name="modelDirectory">The directory path containing the OpenFace models.</param>
        public OpenFaceConfiguration(string modelDirectory)
        {
            this.ModelDirectory = modelDirectory;
        }
    }
}
