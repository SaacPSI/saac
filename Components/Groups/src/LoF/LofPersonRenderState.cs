// <copyright file="LofPersonRenderState.cs" company="SAAC">
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
    public class LofPersonRenderState
    {
        public string Uuid { get; set; } = string.Empty;
        public string? DisplayName { get; set; }

        public Vector2 Position { get; set; }
        public Vector2 Forward { get; set; }

        public double SpeakingTime { get; set; }
        public double Volume { get; set; }

        public bool IsSpeaking => SpeakingTime > 0.0;
        public double NodeRadius { get; set; }
    }

    public class PersonSample
    {
        public int Id;
        public Vector3 Position;   // (X, Y, Z) → use Y=0 if planar
        public Vector3 Direction;  // normalized
        public float Volume;       // speaking weight
    }

    public class LofFeatures
    {
        public Vector3 Centroid;
        public float Mass;
        public float Density;
        public float Spread;
    }
}
