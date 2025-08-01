using OllamaClientLibrary.Dto.ChatCompletion;
using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;
using OllamaClientLibrary.Dto.Models;
using OllamaClientLibrary.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaClientLibrary.Abstractions.HttpClients
{
    internal interface IOllamaHttpClient : IDisposable
    {
        Task<ChatCompletionResponse<T>?> GetCompletionAsync<T>(ChatMessageRequest[] messages, Tool[]? tools = null, CancellationToken ct = default) where T : class;

        IAsyncEnumerable<ChatCompletionResponse<string>> GetChatCompletionAsync(ChatMessageRequest[] messages, Tool[]? tools = null, CancellationToken ct = default);

        Task<double[][]> GetEmbeddingCompletionAsync(string[] input, CancellationToken ct = default);

        Task PullModelAsync(string modelName, IProgress<OllamaPullModelProgress>? progress, CancellationToken ct);

        Task<IEnumerable<Model>> ListLocalModelsAsync(CancellationToken ct = default);
    }
}
