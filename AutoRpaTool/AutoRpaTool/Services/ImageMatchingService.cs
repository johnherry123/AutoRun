using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AutoRpaTool.Services
{
  
    public class ImageMatchingService
    {
        private readonly ImageCaptureService _captureService;

        public ImageMatchingService(ImageCaptureService captureService)
        {
            _captureService = captureService;
        }


        public System.Drawing.Point? FindTemplate(string templatePath, double threshold = 0.85)
        {
            if (!System.IO.File.Exists(templatePath))
                return null;

 
            string screenPath = _captureService.CaptureFullScreen();

            using var source = Cv2.ImRead(screenPath, ImreadModes.Color);
            using var template = Cv2.ImRead(templatePath, ImreadModes.Color);

            if (source.Empty() || template.Empty()) return null;
            if (template.Rows > source.Rows || template.Cols > source.Cols) return null;

            using var result = new Mat();
            Cv2.MatchTemplate(source, template, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal >= threshold)
            {
          
                int centerX = maxLoc.X + template.Cols / 2;
                int centerY = maxLoc.Y + template.Rows / 2;
                return new System.Drawing.Point(centerX, centerY);
            }

            return null;
        }

        /// <summary>
        /// Tìm ảnh mẫu và trả về vị trí click theo offset (0.0–1.0).
        /// offsetX=0.5, offsetY=0.5 = tâm ảnh (behavior cũ).
        /// </summary>
        public System.Drawing.Point? FindTemplateWithOffset(string templatePath, double threshold, double offsetX, double offsetY)
        {
            if (!System.IO.File.Exists(templatePath))
                return null;

            string screenPath = _captureService.CaptureFullScreen();

            using var source = Cv2.ImRead(screenPath, ImreadModes.Color);
            using var template = Cv2.ImRead(templatePath, ImreadModes.Color);

            if (source.Empty() || template.Empty()) return null;
            if (template.Rows > source.Rows || template.Cols > source.Cols) return null;

            using var result = new Mat();
            Cv2.MatchTemplate(source, template, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal >= threshold)
            {
                int clickX = maxLoc.X + (int)(template.Cols * offsetX);
                int clickY = maxLoc.Y + (int)(template.Rows * offsetY);
                return new System.Drawing.Point(clickX, clickY);
            }

            return null;
        }


        public bool IsTemplateVisible(string templatePath, double threshold = 0.85)
            => FindTemplate(templatePath, threshold) != null;
    }
}
