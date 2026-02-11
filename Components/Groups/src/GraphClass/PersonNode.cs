// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
    public class PersonNode
    {
        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double MovementScore { get; set; }// [0..1] recommandé

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double SpeakingTime { get; set; }// [0..1] recommandé

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double TaskParticipation { get; set; }// [0..1] recommandé
    }
}
