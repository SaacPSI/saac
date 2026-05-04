// <copyright file="LofScoreXYVisualizationObject.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Psi.Visualization.Helpers;
using Microsoft.Psi.Visualization.VisualizationObjects;
using SAAC.Groups;

namespace SAAC.Visualizations
{
    [VisualizationObject("LOF Score 2D")]
    public class LofScoreXYVisualizationObject : XYValueVisualizationObject<LofRenderState>
    {
        private bool showLabels = true;
        private double metersPerPixel = 0.008;
        private double nodeRadius = 14.0;
        private double arrowLength = 24.0;
        private double scoreGain = 1.0;
        private double massGain = 1.0;

        [DataMember]
        public bool ShowLabels
        {
            get => this.showLabels;
            set => this.Set(nameof(this.ShowLabels), ref this.showLabels, value);
        }

        [DataMember]
        public double MetersPerPixel
        {
            get => this.metersPerPixel;
            set => this.Set(nameof(this.MetersPerPixel), ref this.metersPerPixel, value);
        }

        [DataMember]
        public double NodeRadius
        {
            get => this.nodeRadius;
            set => this.Set(nameof(this.NodeRadius), ref this.nodeRadius, value);
        }

        [DataMember]
        public double ArrowLength
        {
            get => this.arrowLength;
            set => this.Set(nameof(this.ArrowLength), ref this.arrowLength, value);
        }

        [DataMember]
        public double ScoreGain
        {
            get => this.scoreGain;
            set => this.Set(nameof(this.ScoreGain), ref this.scoreGain, value);
        }

        [DataMember]
        public double MassGain
        {
            get => this.massGain;
            set => this.Set(nameof(this.MassGain), ref this.massGain, value);
        }

        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate
            => XamlHelper.CreateTemplate(this.GetType(), typeof(LofScoreXYVisualizationObjectViews));
    }
}
