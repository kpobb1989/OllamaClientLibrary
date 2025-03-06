using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Tesseract;
using OllamaClientLibrary.Abstractions.Services;

namespace OllamaClientLibrary.Services
{
    class OcrService : IOcrService
    {
        private static readonly string TessDataPath = "./tessdata";
        private static readonly string TessDataUrl = "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata";

        public async Task<string?> GetTextFromImageAsync(Stream stream, string language = "eng")
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            return await GetTextFromImageAsync(memoryStream.ToArray(), language);
        }

        public async Task<string?> GetTextFromImageAsync(byte[] imageBytes, string language = "eng")
        {
            await EnsureTessDataAsync().ConfigureAwait(false);

            using var engine = new TesseractEngine(TessDataPath, language, EngineMode.Default);

            using var img = Pix.LoadFromMemory(imageBytes);

            using var page = engine.Process(img);

            var text = page.GetText();

            return CleanupOcrText(text);
        }

        private async Task EnsureTessDataAsync()
        {
            if (!Directory.Exists(TessDataPath))
            {
                Directory.CreateDirectory(TessDataPath);
            }

            string tessDataFilePath = Path.Combine(TessDataPath, "eng.traineddata");
            if (!File.Exists(tessDataFilePath))
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(TessDataUrl).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await using var fileStream = new FileStream(tessDataFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream).ConfigureAwait(false);
            }
        }

        static string CleanupOcrText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Replace multiple newlines with a single space
            text = text.Replace("\r\n", " ").Replace("\n", " ");

            // Replace multiple spaces with a single space
            text = string.Join(" ", text.Split(new string[' '], StringSplitOptions.RemoveEmptyEntries));

            // Remove any leading/trailing whitespace
            return text.Trim();
        }
    }
}
