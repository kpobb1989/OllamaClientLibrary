using OllamaClientLibrary.Dto.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace OllamaClientLibrary.Abstractions.Services
{
    internal interface IOllamaWebParserService
    {
        Task<IEnumerable<Model>> GetRemoteModelsAsync(Stream stream, CancellationToken ct);
    }
}
