using System.IO;
using System.Threading.Tasks;

namespace OllamaClientLibrary.Abstractions.Services
{
    public interface IDocumentService
    {
        string? GetTextFromWord(Stream stream, string extension);

        string? GetTextFromExcel(Stream stream, string extension);

        Task<string?> GetTextAsync(Stream stream);
    }
}
