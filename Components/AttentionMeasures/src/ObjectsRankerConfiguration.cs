// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Configuration for the ObjectsRanker component.
    /// </summary>
    public class ObjectsRankerConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum score value.
        /// </summary>
        public double MaxScore { get; set; } = 100;

        /// <summary>
        /// Gets or sets the minimum score value.
        /// </summary>
        public double MinScore { get; set; } = 0;

        /// <summary>
        /// Gets or sets the A parameter.
        /// </summary>
        public double A { get; set; } = 2;

        /// <summary>
        /// Gets or sets the B parameter (increase rate when gazed).
        /// </summary>
        public double B { get; set; } = 10;

        /// <summary>
        /// Gets or sets the C parameter.
        /// </summary>
        public double C { get; set; } = 0.9;

        /// <summary>
        /// Gets or sets the D parameter (decrease rate when not gazed).
        /// </summary>
        public double D { get; set; } = -1;
    }
}
