using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;
using OllamaClientLibrary.Dto.Models;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaClientLibrary.Abstractions
{
    /// <summary>
    /// Defines the interface for interacting with the Ollama API.
    /// </summary>
    public interface IOllamaClient : IDisposable
    {
        /// <summary>
        /// Gets or sets the chat history.
        /// </summary>
        List<OllamaChatMessage> ChatHistory { get; set; }

        /// <summary>
        /// Gets chat completion asynchronously.
        /// </summary>
        /// <param name="prompt">The prompt to get chat completion for.</param>
        /// <param name="tool">The tool to use for the completion.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>An asynchronous enumerable of chat messages.</returns>
        IAsyncEnumerable<OllamaChatMessage?> GetChatCompletionAsync(string prompt, Tool? tool = null, CancellationToken ct = default);

        /// <summary>
        /// Gets embeddings for the specified input asynchronously.
        /// </summary>
        /// <param name="input">The input text to generate embeddings for.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a jagged array of doubles representing the embeddings.</returns>
        Task<double[][]> GetEmbeddingCompletionAsync(string[] input, CancellationToken ct = default);

        /// <summary>
        /// Gets JSON completion asynchronously and deserialize the response to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response to.</typeparam>
        /// <param name="prompt">The prompt to generate completion for.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The generated completion deserialized to the specified type.</returns>
        Task<T?> GetJsonCompletionAsync<T>(string? prompt, CancellationToken ct = default) where T : class;

        /// <summary>
        /// Gets text completion asynchronously.
        /// </summary>
        /// <param name="prompt">The prompt to generate completion for.</param>
        /// <param name="tool">The tool to use for the completion.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The generated text completion.</returns>
        Task<string?> GetTextCompletionAsync(string? prompt, Tool? tool = null, CancellationToken ct = default);

        /// <summary>
        /// Lists models asynchronously.
        /// </summary>
        /// <param name="pattern">The pattern to filter models by name.</param>
        /// <param name="size">The size to filter models by.</param>
        /// <param name="location">The location to filter models by.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A list of models.</returns>
        Task<IEnumerable<OllamaModel>> ListModelsAsync(string? pattern = null, ModelSize? size = null, ModelLocation location = ModelLocation.Remote, CancellationToken ct = default);

        /// <summary>
        /// Pulls a model asynchronously.
        /// </summary>
        /// <param name="model">The name of the model to pull.</param>
        /// <param name="progress">An optional progress reporter to report the status of the model pull.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PullModelAsync(string model, IProgress<OllamaPullModelProgress>? progress = null, CancellationToken ct = default);

        /// <summary>
        /// Deletes a model asynchronously.
        /// </summary>
        /// <param name="model">The name of the model to delete.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteModelAsync(string model, CancellationToken ct = default);
    }
}