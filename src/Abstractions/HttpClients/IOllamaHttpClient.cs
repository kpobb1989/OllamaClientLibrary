using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;
using OllamaClientLibrary.Dto.ChatCompletion;

using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using OllamaClientLibrary.Dto.Models;
using OllamaClientLibrary.Models;

namespace OllamaClientLibrary.Abstractions.HttpClients
{
    internal interface IOllamaHttpClient : IDisposable
    {
        Task<ChatCompletionResponse<T>?> GetCompletionAsync<T>(ChatMessageRequest[] messages, Tool[]? tools = null, CancellationToken ct = default) where T : class;

        IAsyncEnumerable<ChatCompletionResponse<string>> GetChatCompletionAsync(ChatMessageRequest[] messages, Tool[]? tools = null, CancellationToken ct = default);

        Task<double[][]> GetEmbeddingCompletionAsync(string[] input, CancellationToken ct = default);

        Task<IEnumerable<Model>> ListLocalModelsAsync(CancellationToken ct = default);

        Task<IEnumerable<Model>> ListRemoteModelsAsync(CancellationToken ct = default);

        Task PullModelAsync(string modelName, IProgress<OllamaPullModelProgress>? progress, CancellationToken ct);

        Task DeleteModelAsync(string model, CancellationToken ct = default);
    }
}
