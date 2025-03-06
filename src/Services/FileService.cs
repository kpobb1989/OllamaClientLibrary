using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.XSSF.UserModel;
using NPOI.XWPF.UserModel;

using OllamaClientLibrary.Abstractions.Services;

using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UglyToad.PdfPig;

namespace OllamaClientLibrary.Services
{
    internal class FileService : IFileService
    {
        private readonly IOcrService _ocrService;

        public FileService(IOcrService ocrService)
        {
            _ocrService = ocrService;
        }

        public async Task<string?> GetTextAsync(string fileName, Stream stream)
        {
            var extension = Path.GetExtension(fileName);

            switch (extension)
            {
                case ".doc":
                case ".docx":
                    return GetTextFromWord(stream, extension);
                case ".xls":
                case ".xlsx":
                    return GetTextFromExcel(stream, extension);
                case ".pdf":
                    return await GetTextFromPdfAsync(stream).ConfigureAwait(false);
                case ".txt":
                case ".json":
                case ".xml":
                case ".csv":
                    return await GetTextAsync(stream).ConfigureAwait(false);
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                case ".webp":
                    return await _ocrService.GetTextFromImageAsync(stream).ConfigureAwait(false);
                default:
                    throw new NotSupportedException($"File type {extension} is not supported");
            }
        }

        public async Task<List<byte[]>> PdfToImagesAsync(string fileName, Stream stream)
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

                    var bytes = ConvertImageToBytes(image, image.RawFormat);

                    images.Add(bytes);
                }
            }

            return images;
        }

        private byte[] ConvertImageToBytes(System.Drawing.Image image, ImageFormat format)
        {
            using var ms = new MemoryStream();

            image.Save(ms, format);

            return ms.ToArray();
        }

        private string? GetTextFromWord(Stream stream, string extension)
        {
            if (extension == ".doc")
            {
                var fs = new POIFSFileSystem(stream);

                var extractor = new NPOI.HSSF.Extractor.EventBasedExcelExtractor(fs);
                return extractor.Text;
            }
            else if (extension == ".docx")
            {
                using var doc = new XWPFDocument(stream);
                var stringBuilder = new StringBuilder();
                foreach (var paragraph in doc.Paragraphs)
                {
                    stringBuilder.AppendLine(paragraph.ParagraphText);
                }
                return stringBuilder.ToString();
            }
            else
            {
                throw new NotSupportedException($"File type {extension} is not supported");
            }
        }

        private string? GetTextFromExcel(Stream stream, string extension)
        {
            if (extension == ".xls")
            {
                using var workbook = new HSSFWorkbook(stream);
                return ExtractTextFromWorkbook(workbook);
            }
            else if (extension == ".xlsx")
            {
                using var workbook = new XSSFWorkbook(stream);
                return ExtractTextFromWorkbook(workbook);
            }
            else
            {
                throw new NotSupportedException($"File type {extension} is not supported");
            }
        }

        private string? ExtractTextFromWorkbook(NPOI.SS.UserModel.IWorkbook workbook)
        {
            var sheet = workbook.GetSheetAt(0);
            if (sheet == null)
                return null;
            var stringBuilder = new StringBuilder();
            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null)
                    continue;
                for (var j = 0; j < row.LastCellNum; j++)
                {
                    var cell = row.GetCell(j);
                    if (cell != null)
                        stringBuilder.Append(cell).Append("\t");
                }
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }

        private static async Task<string?> GetTextAsync(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private async Task<string?> GetTextFromPdfAsync(Stream stream)
        {
            var builder = new StringBuilder();

            using (var document = PdfDocument.Open(stream))
            {
                foreach (var page in document.GetPages())
                {
                    if (IsImageBasedPage(page))
                    {
                        foreach (var image in page.GetImages())
                        {
                            var text = await _ocrService.GetTextFromImageAsync(image.RawBytes.ToArray());

                            builder.Append(text);
                        }
                    }
                    else
                    {
                        builder.Append(page.Text);
                    }
                }
            }

            return builder.ToString();
        }

        private bool IsImageBasedPage(UglyToad.PdfPig.Content.Page page)
        {
            // Calculate text-to-area ratio
            double pageArea = page.Width * page.Height;
            double textCoverage = page.Letters.Count / pageArea;

            return page.GetImages().Any() && textCoverage < 0.0001;
        }
    }
}