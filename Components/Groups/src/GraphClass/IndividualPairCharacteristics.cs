// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using System.Collections.Generic;

    /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
    public class IndividualPairCharacteristics
    {
        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public List<PersonNode> PersonNodes { get; set; }

        /// <summary>Gets or sets épaisseur min. (px) des arêtes.</summary>
        public List<PersonEdge> PersonEdges { get; set; }

        public PersonGroup PersonGroup { get; set; }
    }
}
