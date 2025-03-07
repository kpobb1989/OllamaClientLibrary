using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OllamaClientLibrary.Extensions;

namespace OllamaClientLibrary.Converters
{
    static class ImageConverter
    {
        public static byte[] ToBytes(Stream stream, ImageFormat format, int targetWidth, int targetHeight)
        {
            stream.Position = 0;
            
            using var image = Image.FromStream(stream);
            image.AdjustOrientation();
            using var resizedImage = image.Resize(targetWidth, targetHeight);
            using var ms = new MemoryStream();
            resizedImage.Save(ms, format);

            return ms.ToArray();
        }
        
        public static byte[] ToBytes(Image image, ImageFormat format)
        {
            using var ms = new MemoryStream();
            image.Save(ms, format);
            return ms.ToArray();
        }
    }
}