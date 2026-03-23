using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AutoRpaTool
{
    public partial class RegionSelectorWindow : Window
    {
        private System.Windows.Point _start;
        private bool _isDragging;

        public Rect SelectedRect { get; private set; }

        public RegionSelectorWindow()
        {
            InitializeComponent();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _start = e.GetPosition(MainCanvas);
            _isDragging = true;
            SelectionRect.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRect, _start.X);
            Canvas.SetTop(SelectionRect, _start.Y);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
        }

        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDragging) return;
            var current = e.GetPosition(MainCanvas);
            double x = Math.Min(_start.X, current.X);
            double y = Math.Min(_start.Y, current.Y);
            double w = Math.Abs(current.X - _start.X);
            double h = Math.Abs(current.Y - _start.Y);
            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width = w;
            SelectionRect.Height = h;
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            SelectedRect = new Rect(
                Canvas.GetLeft(SelectionRect),
                Canvas.GetTop(SelectionRect),
                SelectionRect.Width,
                SelectionRect.Height);

            if (SelectedRect.Width > 5 && SelectedRect.Height > 5)
                DialogResult = true;
            else
                DialogResult = false;

            Close();
        }

        private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { DialogResult = false; Close(); }
        }
    }
}
