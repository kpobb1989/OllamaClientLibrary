using System;
using System.IO;
using System.Linq;

namespace OllamaClientLibrary.Models
{
    public class OllamaFile
    {
        private readonly string[] _supportedImages = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        private readonly string[] _supportedDocuments = { ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv", ".json", ".xml" };

        /// <summary>
        /// The name of the file to be processed.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The file stream of the image or document.
        /// </summary>
        public Stream FileStream { get; set; }
        
        public OllamaFile(string filePath)
        {
            FileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            FileName = Path.GetFileName(filePath);
        }
        
        public string GetExtension()
            => Path.GetExtension(FileName).ToLowerInvariant();

        public bool IsImage()
            => _supportedImages.Contains(Path.GetExtension(FileName).ToLowerInvariant());

        public bool IsPdf()
            => Path.GetExtension(FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        
        public bool IsDocument()
            => _supportedDocuments.Contains(Path.GetExtension(FileName).ToLowerInvariant());
    }
}
