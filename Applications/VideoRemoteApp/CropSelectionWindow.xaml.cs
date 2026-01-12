using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VideoRemoteApp
{
    public partial class CropSelectionWindow : Window
    {
        private Point startPoint;
        private Rectangle selectionRectangle;
        public System.Drawing.Rectangle SelectedRectangle { get; private set; }
        private bool isSelecting;

        public CropSelectionWindow()
        {
            InitializeComponent();
            
            SelectionCanvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            SelectionCanvas.MouseMove += Canvas_MouseMove;
            SelectionCanvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
            SelectionCanvas.KeyDown += Canvas_KeyDown;
            this.KeyDown += Canvas_KeyDown;

            Loaded += (s, e) => SelectionCanvas.Focus();

            SelectedRectangle = System.Drawing.Rectangle.Empty;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isSelecting = true;
            startPoint = e.GetPosition(SelectionCanvas);
            
            selectionRectangle = new Rectangle
            {
                Fill = new SolidColorBrush(Colors.Transparent),
                Stroke = new SolidColorBrush(Colors.Lime),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 5 }
            };
            
            SelectionCanvas.Children.Add(selectionRectangle);
            Canvas.SetLeft(selectionRectangle, startPoint.X);
            Canvas.SetTop(selectionRectangle, startPoint.Y);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isSelecting || selectionRectangle == null)
                return;

            Point currentPoint = e.GetPosition(SelectionCanvas);
            
            double x = Math.Min(startPoint.X, currentPoint.X);
            double y = Math.Min(startPoint.Y, currentPoint.Y);
            double width = Math.Abs(currentPoint.X - startPoint.X);
            double height = Math.Abs(currentPoint.Y - startPoint.Y);
            
            Canvas.SetLeft(selectionRectangle, x);
            Canvas.SetTop(selectionRectangle, y);
            selectionRectangle.Width = width;
            selectionRectangle.Height = height;
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isSelecting || selectionRectangle == null)
                return;

            isSelecting = false;
            
            int x = (int)Canvas.GetLeft(selectionRectangle);
            int y = (int)Canvas.GetTop(selectionRectangle);
            int width = (int)selectionRectangle.Width;
            int height = (int)selectionRectangle.Height;
            
            SelectedRectangle = new System.Drawing.Rectangle(x, y, width, height);
            DialogResult = true;
            Close();
        }

        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}