// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
    public enum EdgeWidthMode
    {
        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        GazeOrJVA,

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        Synchrony,

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        Proximity,

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        SpeechEquality,
    }
}
