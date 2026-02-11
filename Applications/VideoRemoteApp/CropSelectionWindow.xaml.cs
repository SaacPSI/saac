// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VideoRemoteApp
{
    /// <summary>
    /// Interaction logic for CropSelectionWindow - allows user to select a screen area for cropping.
    /// </summary>
    public partial class CropSelectionWindow : Window
    {
        private Point startPoint;
        private Rectangle selectionRectangle;
        private bool isSelecting;

        /// <summary>
        /// Gets the selected rectangle coordinates.
        /// </summary>
        public System.Drawing.Rectangle SelectedRectangle { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CropSelectionWindow"/> class.
        /// </summary>
        public CropSelectionWindow()
        {
            this.InitializeComponent();

            this.SelectionCanvas.MouseLeftButtonDown += this.Canvas_MouseLeftButtonDown;
            this.SelectionCanvas.MouseMove += this.Canvas_MouseMove;
            this.SelectionCanvas.MouseLeftButtonUp += this.Canvas_MouseLeftButtonUp;
            this.SelectionCanvas.KeyDown += this.Canvas_KeyDown;
            this.KeyDown += this.Canvas_KeyDown;

            this.Loaded += (s, e) => this.SelectionCanvas.Focus();

            this.SelectedRectangle = System.Drawing.Rectangle.Empty;
        }

        /// <summary>
        /// Handles the mouse left button down event to start selection.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.isSelecting = true;
            this.startPoint = e.GetPosition(this.SelectionCanvas);

            this.selectionRectangle = new Rectangle
            {
                Fill = new SolidColorBrush(Colors.Transparent),
                Stroke = new SolidColorBrush(Colors.Lime),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 5 },
            };

            this.SelectionCanvas.Children.Add(this.selectionRectangle);
            Canvas.SetLeft(this.selectionRectangle, this.startPoint.X);
            Canvas.SetTop(this.selectionRectangle, this.startPoint.Y);
        }

        /// <summary>
        /// Handles the mouse move event to update the selection rectangle.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this.isSelecting || this.selectionRectangle == null)
            {
                return;
            }

            Point currentPoint = e.GetPosition(this.SelectionCanvas);

            double x = Math.Min(this.startPoint.X, currentPoint.X);
            double y = Math.Min(this.startPoint.Y, currentPoint.Y);
            double width = Math.Abs(currentPoint.X - this.startPoint.X);
            double height = Math.Abs(currentPoint.Y - this.startPoint.Y);

            Canvas.SetLeft(this.selectionRectangle, x);
            Canvas.SetTop(this.selectionRectangle, y);
            this.selectionRectangle.Width = width;
            this.selectionRectangle.Height = height;
        }

        /// <summary>
        /// Handles the mouse left button up event to finalize the selection.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!this.isSelecting || this.selectionRectangle == null)
            {
                return;
            }

            this.isSelecting = false;

            int x = (int)Canvas.GetLeft(this.selectionRectangle);
            int y = (int)Canvas.GetTop(this.selectionRectangle);
            int width = (int)this.selectionRectangle.Width;
            int height = (int)this.selectionRectangle.Height;

            this.SelectedRectangle = new System.Drawing.Rectangle(x, y, width, height);
            this.DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// Handles the key down event to cancel selection on Escape key.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }
    }
}
