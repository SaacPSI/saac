// <copyright file="LofScore.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

namespace SAAC.Groups
{
    public class LofScore
    {
        public DateTime FrameTime { get; set; }
        public double MuX { get; set; }
        public double MuY { get; set; }

        public double Vx { get; set; }
        public double Vy { get; set; }

        public double Ux { get; set; }
        public double Uy { get; set; }

        public double Mass { get; set; }
        public double Density { get; set; }
        public double AspectRatio { get; set; }

        public double KfX { get; set; }
        public double KfY { get; set; }
        public double KfVx { get; set; }
        public double KfVy { get; set; }
        public double KfVel { get; set; }

        public double Score { get; set; }
    }
}
