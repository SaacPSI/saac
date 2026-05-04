// <copyright file="LofLocusRenderState.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SAAC.Groups
{
    public class LofLocusRenderState
    {
        public Vector2 Centroid { get; set; }
        public Vector2 SpanVector1 { get; set; }
        public Vector2 SpanVector2 { get; set; }

        public double Mass { get; set; }
        public double Density { get; set; }
        public double AspectRatio { get; set; }

        public Vector2? KfCentroid { get; set; }
        public Vector2? KfVelocity { get; set; }
        public double? KfSpeed { get; set; }
    }
}
