// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace SAAC.Visualizations
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
