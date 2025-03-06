using System.IO;
using System.Threading.Tasks;

namespace OllamaClientLibrary.Abstractions.Services
{
    public interface IOcrService
    {
        Task<string?> GetTextFromImageAsync(Stream stream, string language = "eng");

        Task<string?> GetTextFromImageAsync(byte[] imageBytes, string language = "eng");
    }
}
