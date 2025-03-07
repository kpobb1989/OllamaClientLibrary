using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace OllamaClientLibrary.Converters
{
    static class PdfConverter
    {
        public static async Task<List<byte[]>> ToImagesAsync(Stream stream, string fileName)
        {
            if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("This method only works with PDF files");
            }

            // Reset stream position if possible
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            else
            {
                // If we can't seek the original stream, we must copy it
                var seekableStream = new MemoryStream();
                await stream.CopyToAsync(seekableStream).ConfigureAwait(false);
                seekableStream.Position = 0;
                stream = seekableStream;
            }

            var images = new List<byte[]>();

            using var document = PdfDocument.Open(stream);

            // First try to extract embedded images from each page
            foreach (var page in document.GetPages())
            {
                foreach (var pdfImage in page.GetImages())
                {
                    // Try to convert to bitmap
                    using var ms = new MemoryStream(pdfImage.RawBytes.ToArray());

                    // Try to detect the format and convert to bitmap
                    using var image = System.Drawing.Image.FromStream(ms);

                    image.RotateFlip(System.Drawing.RotateFlipType.Rotate270FlipNone);

                    var bytes = ImageConverter.ToBytes(image, image.RawFormat);

                    images.Add(bytes);
                }
            }

            return images;
        }
    }
}