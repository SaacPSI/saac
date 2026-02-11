// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.Drawing;

namespace VideoRemoteApp
{
    /// <summary>
    /// Configuration settings for the Video Remote Application.
    /// </summary>
    public class VideoRemoteAppConfiguration
    {
        /// <summary>
        /// Gets or sets the capture interval between video frames.
        /// </summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(100.0);

        /// <summary>
        /// Gets or sets the JPEG encoding quality level (0-100).
        /// </summary>
        public int EncodingVideoLevel { get; set; } = 75;

        /// <summary>
        /// Gets or sets the dictionary of named cropping areas with their rectangles.
        /// </summary>
        public Dictionary<string, Rectangle> CroppingAreas { get; set; } = new Dictionary<string, Rectangle>();
    }
}
