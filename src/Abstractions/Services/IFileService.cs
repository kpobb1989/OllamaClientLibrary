using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OllamaClientLibrary.Abstractions.Services
{
    public interface IFileService
    {
        Task<string?> GetTextAsync(string fileName, Stream stream);

        Task<List<byte[]>> PdfToImagesAsync(string fileName, Stream stream);
    }
}
