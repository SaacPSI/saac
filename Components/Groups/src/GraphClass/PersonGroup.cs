// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
    public class PersonGroup
    {
        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double TaskingMost { get; set; }

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double TalkingMost { get; set; }// [0..1] recommandé

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double WatchedMost { get; set; }// [0..1] recommandé

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double LeadVA { get; set; }// [0..1] recommandé

        public double Dominance_score { get; set; }

        public double Ja_score { get; set; }

        public double Engagement_score { get; set; }

        public double Cpm_score { get; set; }

        public double Spatial_score { get; set; }

        public double Dimension2_score { get; set; }

        public double Collaboration_score { get; set; }
    }
}
