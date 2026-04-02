using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ZeroMix.Recorder
{
    public partial class AreaSelectorWindow : Window
    {
        private System.Windows.Point _startPoint;
        public Rect SelectedRect { get; private set; }
        private bool _isDragging = false;
        private bool _isMoved = false;

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _isMoved = false;
                _startPoint = e.GetPosition(SelectionCanvas);
                SelectionBox.Width = 0;
                SelectionBox.Height = 0;
                SelectionBox.Visibility = Visibility.Visible;
                Canvas.SetLeft(SelectionBox, _startPoint.X);
                Canvas.SetTop(SelectionBox, _startPoint.Y);
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                _isMoved = true;
                var currentPoint = e.GetPosition(SelectionCanvas);
                // ...

                double x = Math.Min(_startPoint.X, currentPoint.X);
                double y = Math.Min(_startPoint.Y, currentPoint.Y);
                double width = Math.Max(_startPoint.X, currentPoint.X) - x;
                double height = Math.Max(_startPoint.Y, currentPoint.Y) - y;

                Canvas.SetLeft(SelectionBox, x);
                Canvas.SetTop(SelectionBox, y);
                SelectionBox.Width = width;
                SelectionBox.Height = height;
            }
        }

        public bool IsCancelled { get; private set; } = true;

        public AreaSelectorWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            
            if (_isMoved && SelectionBox.Width > 10 && SelectionBox.Height > 10)
            {
                SelectedRect = new Rect(
                    Canvas.GetLeft(SelectionBox),
                    Canvas.GetTop(SelectionBox),
                    SelectionBox.Width,
                    SelectionBox.Height
                );
                
                IsCancelled = false;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                IsCancelled = true;
                this.DialogResult = false;
                this.Close();
            }
        }
    }
}
