// <copyright file="LofRenderState.cs" company="SAAC">
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
    /*public class LofRenderState
    {
        public DateTime FrameTime { get; set; }

        public List<LofPersonRenderState> People { get; set; } = new();

        public LofLocusRenderState? Locus { get; set; }

        public double GlobalScore { get; set; }
        public double DistanceBetweenPeople { get; set; }
    }*/

    public class LofRenderState
    {
        public float Score;                // global LOF score
        public Vector3 Centroid;           // focus center
        public Vector3 MajorAxis;          // spread direction
        public Vector3 MinorAxis;
        public float Mass;                 // area/strength
        public float Density;
        public List<Vector3> Contour;      // optional for viz

        public List<PersonSample> Persons;
    }
}
