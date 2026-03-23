using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace AutoRpaTool
{
    public partial class ClickMarkerWindow : Window
    {
        /// <summary>Offset X (0.0–1.0) so với ảnh. 0.5 = tâm.</summary>
        public double OffsetX { get; private set; } = 0.5;

        /// <summary>Offset Y (0.0–1.0) so với ảnh. 0.5 = tâm.</summary>
        public double OffsetY { get; private set; } = 0.5;

        public ClickMarkerWindow(string imagePath)
        {
            InitializeComponent();

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(imagePath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                PreviewImage.Source = bmp;
            }
            catch (Exception ex)
            {
                StatusText.Text = $"❌ Lỗi tải ảnh: {ex.Message}";
            }
        }

        private void OnImageClick(object sender, MouseButtonEventArgs e)
        {
            // Lấy vị trí click tương đối với Image control
            var pos = e.GetPosition(PreviewImage);
            double imgW = PreviewImage.ActualWidth;
            double imgH = PreviewImage.ActualHeight;

            if (imgW <= 0 || imgH <= 0) return;

            // Tính offset tỉ lệ 0.0–1.0
            OffsetX = Math.Clamp(pos.X / imgW, 0.0, 1.0);
            OffsetY = Math.Clamp(pos.Y / imgH, 0.0, 1.0);

            // Cập nhật visual marker
            UpdateMarkerPosition(pos.X, pos.Y, imgW, imgH);
            StatusText.Text = $"🎯 Điểm click: {OffsetX:P0} x {OffsetY:P0}";
        }

        private void UpdateMarkerPosition(double x, double y, double imgW, double imgH)
        {
            // Tính offset của Image trong Grid (do Stretch=Uniform)
            var imgPos = PreviewImage.TranslatePoint(new System.Windows.Point(0, 0), MarkerCanvas);

            double canvasX = imgPos.X + x;
            double canvasY = imgPos.Y + y;

            // Marker dot (centered on click point)
            Canvas.SetLeft(MarkerDot, canvasX - MarkerDot.Width / 2);
            Canvas.SetTop(MarkerDot, canvasY - MarkerDot.Height / 2);
            MarkerDot.Visibility = Visibility.Visible;

            // Crosshair lines
            LineH.X1 = imgPos.X;
            LineH.Y1 = canvasY;
            LineH.X2 = imgPos.X + imgW;
            LineH.Y2 = canvasY;
            LineH.Visibility = Visibility.Visible;

            LineV.X1 = canvasX;
            LineV.Y1 = imgPos.Y;
            LineV.X2 = canvasX;
            LineV.Y2 = imgPos.Y + imgH;
            LineV.Visibility = Visibility.Visible;
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnSkip(object sender, RoutedEventArgs e)
        {
            // Giữ default 0.5, 0.5
            OffsetX = 0.5;
            OffsetY = 0.5;
            DialogResult = true;
            Close();
        }
    }
}
