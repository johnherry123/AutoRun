using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace AutoRpaTool.Services
{

    public class ImageCaptureService
    {
        private readonly string _imageFolder;

        public ImageCaptureService()
        {
           
            _imageFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
            Directory.CreateDirectory(_imageFolder);
        }
        public string CaptureFullScreen()
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            return CaptureRegion(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
        public string CaptureRegion(int x, int y, int width, int height)
        {
            using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

            string fileName = $"capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            string filePath = Path.Combine(_imageFolder, fileName);
            bmp.Save(filePath, ImageFormat.Png);
            return filePath;
        }
        public string ImageFolder => _imageFolder;
    }
}
