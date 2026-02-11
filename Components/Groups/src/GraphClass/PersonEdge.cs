// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
    public class PersonEdge
    {
        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public string FromId_1 { get; set; }

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public string ToId_1 { get; set; }

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public string FromId_2 { get; set; }

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public string ToId_2 { get; set; }

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double Proximity { get; set; }// 0..1 (à définir)

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double Synchrony { get; set; }// 0..1

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double GazeOnPeers12 { get; set; }// normalisé recommandé

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double GazeOnPeers21 { get; set; }

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double JVAEvent { get; set; }// 0..1 (taux)

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double JVAIntensity { get; set; }// 0..1 (durée moyenne normalisée)

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double SpeechEquality { get; set; }// 0..1 (0.5 = égalité parfaite)

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public double TaskEquality { get; set; }// 0..1
    }
}
