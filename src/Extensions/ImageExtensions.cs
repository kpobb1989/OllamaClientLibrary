using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace OllamaClientLibrary.Extensions
{
    public static class ImageExtensions
    {
        public static void AdjustOrientation(this Image image)
        {
            if (image.PropertyIdList.Contains(0x0112))
            {
                var orientation = (int?)image.GetPropertyItem(0x0112)?.Value?[0];
                switch (orientation)
                {
                    case 1:
                        // No rotation required.
                        break;
                    case 2:
                        image.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        break;
                    case 3:
                        image.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case 4:
                        image.RotateFlip(RotateFlipType.Rotate180FlipX);
                        break;
                    case 5:
                        image.RotateFlip(RotateFlipType.Rotate90FlipX);
                        break;
                    case 6:
                        image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case 7:
                        image.RotateFlip(RotateFlipType.Rotate270FlipX);
                        break;
                    case 8:
                        image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                }
            }
        }

        public static Image Resize(this Image image, int targetWidth, int targetHeight)
        {
            // Calculate dimensions while preserving aspect ratio
            int newWidth, newHeight;

            // Calculate aspect ratio
            float aspectRatio = (float)image.Width / image.Height;

            if (aspectRatio > ((float)targetWidth / targetHeight)) // Image is wider
            {
                newWidth = targetWidth;
                newHeight = (int)(targetWidth / aspectRatio);
            }
            else // Image is taller
            {
                newHeight = targetHeight;
                newWidth = (int)(targetHeight * aspectRatio);
            }

            // Create a new bitmap with the calculated size
            var resizedImage = new Bitmap(newWidth, newHeight);

            // Set up the resize quality
            using var graphics = Graphics.FromImage(resizedImage);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;

            // Draw the original image to the new size
            graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return resizedImage;
        }
    }
}
