// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for PlayersDataVisualizationObjectView.xaml.
    /// </summary>
    public partial class PlayersDataVisualizationObjectView : VisualizationObjectView
    {
        private Point canvasPosition = new Point();
        private Point initialMousePos;
        private double currentZoom = 1;
        private bool isMouseCaptured = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayersDataVisualizationObjectView"/> class.
        /// </summary>
        public PlayersDataVisualizationObjectView()
        {
            this.InitializeComponent();
        }


        private void onCanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;

            Point mousePos = e.GetPosition(mainCanvas);
            double offsetX = canvasPosition.X - mousePos.X;
            double offsetY = canvasPosition.Y - mousePos.Y;

            canvasPosition.X = canvasPosition.X * zoomFactor;
            canvasPosition.Y = canvasPosition.Y * zoomFactor;

            mainCanvas.LayoutTransform = new ScaleTransform(mainCanvas.LayoutTransform.Value.M11 * zoomFactor, mainCanvas.LayoutTransform.Value.M22 * zoomFactor);
            mainCanvas.RenderTransform = new TranslateTransform(canvasPosition.X, canvasPosition.Y);

            e.Handled = true;
        }


        private void onCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.CaptureMouse();
            initialMousePos = e.GetPosition(this);
            isMouseCaptured = true;
        }

        private void onCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseCaptured)
            {
                Point currentPosition = e.GetPosition(this);
                double offsetX = currentPosition.X - initialMousePos.X;
                double offsetY = currentPosition.Y - initialMousePos.Y;

                mainCanvas.RenderTransform = new TranslateTransform(canvasPosition.X + offsetX, canvasPosition.Y + offsetY);
            }
        }

        private void onCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.ReleaseMouseCapture();
            isMouseCaptured = false;

            Point currentPosition = e.GetPosition(this);
            double offsetX = currentPosition.X - initialMousePos.X;
            double offsetY = currentPosition.Y - initialMousePos.Y;

            canvasPosition.X += offsetX;
            canvasPosition.Y += offsetY;

            mainCanvas.RenderTransform = new TranslateTransform(canvasPosition.X, canvasPosition.Y);
        }
    }
}