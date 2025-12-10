// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace SAAC.Visualizations
{
    /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
    public class PersonGroup
    {
        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double taskingMost { get; set; }

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double talkingMost { get; set; }// [0..1] recommandé

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double watchedMost { get; set; }// [0..1] recommandé

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double leadVA { get; set; }// [0..1] recommandé

        public double dominance_score { get; set; }
        public double ja_score { get; set; }
        public double engagement_score { get; set; }
        public double cpm_score { get; set; }
        public double spatial_score { get; set; }
        public double dimension2_score { get; set; }
        public double collaboration_score { get; set; }
    }
}
