// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Visualizations
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using System.Windows.Threading;
    using SAAC.Groups;

    /// <summary>
    /// WPF view (UserControl) for the 2D sociogram visualization.
    /// Keeps original architecture, adds on/off per-layer toggles from VO.
    /// </summary>

    public partial class LofScoreXYVisualizationObjectViews : UserControl
    {
        public LofScoreXYVisualizationObjectViews()
        {
            InitializeComponent();
        }

        public LofScoreXYVisualizationObject VisualizationObject
            => this.DataContext as LofScoreXYVisualizationObject;

        public LofRenderState CurrentData
            => this.VisualizationObject?.CurrentData;

        // Derived properties for ellipse
        public double EllipseWidth =>
            CurrentData == null ? 0 : CurrentData.MajorAxis.Length() * 2;

        public double EllipseHeight =>
            CurrentData == null ? 0 : CurrentData.MinorAxis.Length() * 2;

        public double EllipseWidthHalf => EllipseWidth / 2;
        public double EllipseHeightHalf => EllipseHeight / 2;

        public double EllipseAngle =>
            CurrentData == null ? 0 :
            Math.Atan2(CurrentData.MajorAxis.Y, CurrentData.MajorAxis.X) * 180 / Math.PI;
    }
}
