// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace SAAC.Visualizations
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
